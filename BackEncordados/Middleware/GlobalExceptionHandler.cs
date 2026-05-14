using System.Text.Json;
using System.Text.Json.Serialization;
using BackEncordados.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Middleware;

/// <summary>
/// Manejador global de excepciones.
/// </summary>
/// <remarks>
/// Captura excepciones no controladas y errores del dominio (Result Pattern).
/// Genera respuestas HTTP consistentes y trazables.
/// 
/// <para><b>Dependencias:</b></para>
/// <list type="bullet">
///     <item>RequestDelegate: Pipeline de la solicitud</item>
///     <item>ILogger: Logging de errores</item>
/// </list>
/// 
/// <para><b>Características:</b></para>
/// <list type="bullet">
///     <item>Genera ID único de error (8 caracteres)</item>
///     <item>Mapea tipos de excepción a códigos HTTP</item>
///     <item>Respuesta JSON estructurada</item>
/// </list>
/// 
/// <para><b>Mapeo de excepciones:</b></para>
/// <list type="table">
///     <item>
///         <term>CartNotFoundException</term>
///         <description>404 NotFoundError</description>
///     </item>
///     <item>
///         <term>UnauthorizedAccessException</term>
///         <description>401 UnauthorizedError</description>
///     </item>
///     <item>
///         <term>ArgumentException</term>
///         <description>400 ValidationError</description>
///     </item>
///     <item>
///         <term>CloudinaryUploadException</term>
///         <description>422 CloudinaryUploadError</description>
///     </item>
///     <item>
///         <term>CloudinaryDeleteException</term>
///         <description>422 CloudinaryDeleteError</description>
///     </item>
///     <item>
///         <term>CloudinaryConfigurationException</term>
///         <description>500 CloudinaryConfigurationError</description>
///     </item>
///     <item>
///         <term>CloudinaryInvalidParameterException</term>
///         <description>400 CloudinaryInvalidParameterError</description>
///     </item>
///     <item>
///         <term>DbUpdateException</term>
///         <description>409 ConflictError</description>
///     </item>
///     <item>
///         <term>TimeoutException</term>
///         <description>408 InternalError</description>
///     </item>
///     <item>
///         <term>Exception (general)</term>
///         <description>500 InternalError</description>
///     </item>
/// </list>
/// </remarks>
public class GlobalExceptionHandler(
    RequestDelegate next,
    ILogger<GlobalExceptionHandler> logger
)
{
/// <summary>
/// Punto de entrada del middleware.
/// </summary>
/// <remarks>
/// <list type="number">
///     <item>Ejecuta el siguiente middleware en el pipeline</item>
///     <item>Captura cualquier excepción no manejada</item>
///     <item>Genera ID de error único</item>
///     <item>Llama a HandleExceptionAsync para responder al cliente</item>
/// </list>
/// </remarks>
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
    /// Maneja las excepciones capturadas y genera respuesta HTTP.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    ///     <item>Configura el tipo de contenido como JSON</item>
    ///     <item>Mapea el tipo de excepción a código de estado HTTP</item>
    ///     <item>Construye respuesta con errorId, message, errorType, timestamp, path, method</item>
    ///     <item>Serializa y envía la respuesta</item>
    /// </list>
    /// </remarks>
    /// <param name="context">Contexto HTTP.</param>
    /// <param name="exception">Excepción capturada.</param>
    /// <param name="errorId">ID de seguimiento del error.</param>
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
                409,
                "Error al actualizar la base de datos",
                (Dictionary<string, string[]>?)null,
                "ConflictError"
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
/// Extensiones para registro del middleware.
/// </summary>
public static class GlobalExceptionHandlerExtensions
{
    /// <summary>Registra el middleware de excepciones.</summary>
    /// <param name="app">Constructor de la aplicación.</param>
    /// <returns>IApplicationBuilder.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}