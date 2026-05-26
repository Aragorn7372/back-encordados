using System.Text.Json;
using System.Text.Json.Serialization;
using BackEncordados.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Middleware;

/// <summary>
/// Middleware global de manejo de excepciones que captura errores no controlados
/// del pipeline de ASP.NET Core y genera respuestas HTTP JSON consistentes.
/// </summary>
/// <remarks>
/// <para>Se ejecuta como middleware terminal para errores: captura cualquier
/// excepción no manejada por los controladores u otros middlewares, genera
/// un ID de seguimiento único, registra el error con logging estructurado,
/// y retorna una respuesta JSON normalizada al cliente.</para>
/// <para><b>Estructura de la respuesta JSON:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Campo</term>
///     <description>Tipo</description>
///     <description>Descripción</description>
///   </listheader>
///   <item>
///     <term><c>errorId</c></term>
///     <description>string</description>
///     <description>Identificador único de 8 caracteres hexadecimales para trazabilidad.</description>
///   </item>
///   <item>
///     <term><c>message</c></term>
///     <description>string</description>
///     <description>Mensaje descriptivo del error adaptado al tipo de excepción.</description>
///   </item>
///   <item>
///     <term><c>errorType</c></term>
///     <description>string</description>
///     <description>Categoría del error (ej: "UnauthorizedError", "ValidationError", "InternalError").</description>
///   </item>
///   <item>
///     <term><c>timestamp</c></term>
///     <description>string (ISO 8601)</description>
///     <description>Marca de tiempo UTC en formato <c>yyyy-MM-ddTHH:mm:ss.fffffffZ</c>.</description>
///   </item>
///   <item>
///     <term><c>path</c></term>
///     <description>string</description>
///     <description>Ruta de la solicitud que originó el error (ej: <c>/api/excel/export/...</c>).</description>
///   </item>
///   <item>
///     <term><c>method</c></term>
///     <description>string</description>
///     <description>Verbo HTTP de la solicitud (GET, POST, PUT, DELETE, etc.).</description>
///   </item>
///   <item>
///     <term><c>errors</c></term>
///     <description>Dictionary (nullable)</description>
///     <description>Detalles adicionales de errores de validación (actualmente no implementado,
///     reservado para expansión futura). Se omite si es null.</description>
///   </item>
/// </list>
/// <para><b>Mapeo de excepciones a códigos HTTP:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Excepción</term>
///     <description>HTTP Status</description>
///     <description>errorType</description>
///     <description>Message</description>
///   </listheader>
///   <item>
///     <term><c>UnauthorizedAccessException</c></term>
///     <description>401</description>
///     <description>UnauthorizedError</description>
///     <description>"No autorizado"</description>
///   </item>
///   <item>
///     <term><c>ArgumentException</c></term>
///     <description>400</description>
///     <description>ValidationError</description>
///     <description>ex.Message</description>
///   </item>
///   <item>
///     <term><c>CloudinaryUploadException</c></term>
///     <description>422</description>
///     <description>CloudinaryUploadError</description>
///     <description>ex.Message</description>
///   </item>
///   <item>
///     <term><c>CloudinaryDeleteException</c></term>
///     <description>422</description>
///     <description>CloudinaryDeleteError</description>
///     <description>ex.Message</description>
///   </item>
///   <item>
///     <term><c>CloudinaryConfigurationException</c></term>
///     <description>500</description>
///     <description>CloudinaryConfigurationError</description>
///     <description>ex.Message</description>
///   </item>
///   <item>
///     <term><c>CloudinaryInvalidParameterException</c></term>
///     <description>400</description>
///     <description>CloudinaryInvalidParameterError</description>
///     <description>ex.Message</description>
///   </item>
///   <item>
///     <term><c>DbUpdateException</c></term>
///     <description>400</description>
///     <description>DataIntegrityError</description>
///     <description>"Los datos enviados contienen errores de integridad..."</description>
///   </item>
///   <item>
///     <term><c>TimeoutException</c></term>
///     <description>408</description>
///     <description>InternalError</description>
///     <description>"Tiempo de espera agotado"</description>
///   </item>
///   <item>
///     <term><c>Exception</c> (general)</term>
///     <description>500</description>
///     <description>InternalError</description>
///     <description>"Ha ocurrido un error interno"</description>
///   </item>
/// </list>
/// <para><b>Configuración de serialización JSON:</b></para>
/// <list type="bullet">
///   <item><description><c>PropertyNamingPolicy = JsonNamingPolicy.CamelCase</c>: nombres de propiedad en camelCase.</description></item>
///   <item><description><c>DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull</c>: omite propiedades nulas
///   (como <c>errors</c> cuando no hay detalles de validación).</description></item>
/// </list>
/// <para>Para registrar este middleware en el pipeline de la aplicación, usar
/// <c>app.UseGlobalExceptionHandler()</c> en <c>Program.cs</c> antes de otros middlewares
/// que puedan generar errores.</para>
/// </remarks>
/// <param name="next">Siguiente middleware en el pipeline de ASP.NET Core.</param>
/// <param name="logger">Logger para registro estructurado de errores con ErrorId y Message.</param>
public class GlobalExceptionHandler(
    RequestDelegate next,
    ILogger<GlobalExceptionHandler> logger
)
{
    /// <summary>
    /// Punto de entrada del middleware. Ejecuta el pipeline y captura excepciones no manejadas.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Ejecuta el siguiente middleware en el pipeline de ASP.NET Core
    ///   mediante <c>await next(context)</c>.</description></item>
    ///   <item><description>Si la ejecución es exitosa, el método retorna sin cambios en la respuesta.</description></item>
    ///   <item><description>Si ocurre cualquier excepción, genera un identificador único de error
    ///   de 8 caracteres hexadecimales a partir de <c>Guid.NewGuid().ToString()[..8]</c>.</description></item>
    ///   <item><description>Registra el error con <c>logger.LogError</c> incluyendo el ErrorId y el mensaje
    ///   como propiedades estructuradas para facilitar la búsqueda en sistemas de logging.</description></item>
    ///   <item><description>Delega en <c>HandleExceptionAsync</c> para construir y enviar la respuesta JSON
    ///   con el código HTTP y mensaje apropiados según el tipo de excepción.</description></item>
    ///   <item><description>No relanza la excepción: el middleware actúa como terminal para errores,
    ///   evitando que lleguen al cliente como respuestas HTML por defecto de ASP.NET Core.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="context">Contexto HTTP de la solicitud actual.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString()[..8];
            logger.LogError(ex, "Excepción no manejada. ErrorId: {ErrorId}, Message: {Message}",
                errorId, ex.Message);
            await HandleExceptionAsync(context, ex, errorId);
        }
    }

    /// <summary>
    /// Construye y envía una respuesta JSON estructurada según el tipo de excepción capturada.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Establece el Content-Type de la respuesta como <c>application/json</c>.</description></item>
    ///   <item><description>Usa un switch expression para mapear el tipo de excepción a una tupla
    ///   de <c>(statusCode, message, errors, errorType)</c>:</description></item>
    /// </list>
    /// <para><b>Mapeo detallado:</b></para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Excepción</term>
    ///     <description>Código</description>
    ///     <description>Mensaje</description>
    ///     <description>Tipo de error</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>UnauthorizedAccessException</c></term>
    ///     <description>401</description>
    ///     <description>"No autorizado"</description>
    ///     <description>UnauthorizedError</description>
    ///   </item>
    ///   <item>
    ///     <term><c>ArgumentException</c></term>
    ///     <description>400</description>
    ///     <description><c>argument.Message</c> (mensaje original)</description>
    ///     <description>ValidationError</description>
    ///   </item>
    ///   <item>
    ///     <term><c>CloudinaryUploadException</c></term>
    ///     <description>422</description>
    ///     <description><c>uploadEx.Message</c></description>
    ///     <description>CloudinaryUploadError</description>
    ///   </item>
    ///   <item>
    ///     <term><c>CloudinaryDeleteException</c></term>
    ///     <description>422</description>
    ///     <description><c>deleteEx.Message</c></description>
    ///     <description>CloudinaryDeleteError</description>
    ///   </item>
    ///   <item>
    ///     <term><c>CloudinaryConfigurationException</c></term>
    ///     <description>500</description>
    ///     <description><c>configEx.Message</c></description>
    ///     <description>CloudinaryConfigurationError</description>
    ///   </item>
    ///   <item>
    ///     <term><c>CloudinaryInvalidParameterException</c></term>
    ///     <description>400</description>
    ///     <description><c>paramEx.Message</c></description>
    ///     <description>CloudinaryInvalidParameterError</description>
    ///   </item>
    ///   <item>
    ///     <term><c>DbUpdateException</c></term>
    ///     <description>400</description>
    ///     <description>"Los datos enviados contienen errores de integridad (duplicados o estructura inválida)"</description>
    ///     <description>DataIntegrityError</description>
    ///   </item>
    ///   <item>
    ///     <term><c>TimeoutException</c></term>
    ///     <description>408</description>
    ///     <description>"Tiempo de espera agotado"</description>
    ///     <description>InternalError</description>
    ///   </item>
    ///   <item>
    ///     <term><c>Exception</c> (cualquier otra)</term>
    ///     <description>500</description>
    ///     <description>"Ha ocurrido un error interno"</description>
    ///     <description>InternalError</description>
    ///   </item>
    /// </list>
    /// <para>Luego construye un objeto anónimo con 7 propiedades serializado con
    /// <c>System.Text.Json</c> usando camelCase y omisión de propiedades nulas,
    /// y lo escribe en el cuerpo de la respuesta HTTP.</para>
    /// </remarks>
    /// <param name="context">Contexto HTTP de la solicitud actual.</param>
    /// <param name="exception">Excepción capturada para mapear a código y mensaje HTTP.</param>
    /// <param name="errorId">Identificador único de 8 caracteres para trazabilidad del error.</param>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string errorId)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors, errorType) = exception switch
        {
            UnauthorizedAccessException => (
                401,
                "No autorizado",
                (Dictionary<string, string[]>?)null,
                "UnauthorizedError"
            ),

            ArgumentException argument => (
                400,
                argument.Message,
                (Dictionary<string, string[]>?)null,
                "ValidationError"
            ),

            CloudinaryUploadException uploadEx => (
                422,
                uploadEx.Message,
                (Dictionary<string, string[]>?)null,
                "CloudinaryUploadError"
            ),

            CloudinaryDeleteException deleteEx => (
                422,
                deleteEx.Message,
                (Dictionary<string, string[]>?)null,
                "CloudinaryDeleteError"
            ),

            CloudinaryConfigurationException configEx => (
                500,
                configEx.Message,
                (Dictionary<string, string[]>?)null,
                "CloudinaryConfigurationError"
            ),

            CloudinaryInvalidParameterException paramEx => (
                400,
                paramEx.Message,
                (Dictionary<string, string[]>?)null,
                "CloudinaryInvalidParameterError"
            ),

            DbUpdateException => (
                400,
                "Los datos enviados contienen errores de integridad (duplicados o estructura inválida)",
                (Dictionary<string, string[]>?)null,
                "DataIntegrityError"
            ),

            TimeoutException => (
                408,
                "Tiempo de espera agotado",
                (Dictionary<string, string[]>?)null,
                "InternalError"
            ),

            _ => (
                500,
                "Ha ocurrido un error interno",
                (Dictionary<string, string[]>?)null,
                "InternalError"
            )
        };

        context.Response.StatusCode = statusCode;

        var response = new
        {
            errorId,
            message,
            errorType,
            timestamp = DateTime.UtcNow.ToString("o"),
            path = context.Request.Path,
            method = context.Request.Method,
            errors
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

/// <summary>
/// Extensiones para registrar el middleware <see cref="GlobalExceptionHandler"/> en el pipeline
/// de ASP.NET Core.
/// </summary>
/// <remarks>
/// <para>Proporciona un método de extensión sobre <c>IApplicationBuilder</c> para registrar
/// el middleware de manejo global de excepciones de forma fluida:</para>
/// <code>
/// // En Program.cs:
/// var app = builder.Build();
/// app.UseGlobalExceptionHandler(); // Debe ir antes de otros middlewares
/// app.UseAuthentication();
/// app.UseAuthorization();
/// app.MapControllers();
/// app.Run();
/// </code>
/// <para>Se recomienda registrar este middleware lo antes posible en el pipeline
/// (justo después de <c>app.Build()</c>) para que capture excepciones de todos
/// los middlewares posteriores, incluyendo autenticación, autorización y controladores.</para>
/// </remarks>
public static class GlobalExceptionHandlerExtensions
{
    /// <summary>
    /// Registra el middleware <see cref="GlobalExceptionHandler"/> en el pipeline de la aplicación.
    /// </summary>
    /// <remarks>
    /// <para>Llama a <c>app.UseMiddleware&lt;GlobalExceptionHandler&gt;()</c> para insertar
    /// el middleware en el pipeline. Debe colocarse antes de otros middlewares para
    /// garantizar que capture todas las excepciones.</para>
    /// </remarks>
    /// <param name="app">Constructor de la aplicación (<c>IApplicationBuilder</c>).</param>
    /// <returns>El mismo constructor para encadenar más configuraciones.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}