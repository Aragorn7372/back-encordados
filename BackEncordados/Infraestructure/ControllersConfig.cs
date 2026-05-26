using System.Text.Json;
using System.Text.Json.Serialization;
using BackEncordados.Infraestructure.Constraints;
using Microsoft.AspNetCore.Routing;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Configuración de controladores MVC, rutas, serialización JSON/XML y constraints
/// personalizadas para la aplicación.
/// </summary>
/// <remarks>
/// <para>Proporciona un método de extensión sobre <c>IServiceCollection</c> que
/// registra y configura los controladores MVC con las siguientes características:</para>
/// <list type="bullet">
///   <item><description>Constraint de ruta personalizada <c>{ulid}</c> para validar parámetros ULID.</description></item>
///   <item><description>Negociación de contenido respetando el header <c>Accept</c>.</description></item>
///   <item><description>Rechazo de formatos no aceptables (HTTP 406).</description></item>
///   <item><description>Serialización JSON con camelCase, indentado, case-insensitive y conversión de enums a string.</description></item>
///   <item><description>Soporte para formateadores XML (XmlSerializer y DataContractSerializer).</description></item>
/// </list>
/// <para>Usar <c>services.AddMvcControllers()</c> en <c>Program.cs</c>.</para>
/// </remarks>
public static class ControllersConfig
{
    /// <summary>
    /// Configura y registra los controladores MVC con opciones de formato,
    /// negociación de contenido y constraints de ruta personalizadas.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Configura <c>RouteOptions</c> agregando la constraint <c>"ulid"</c>
    ///   mapeada a <see cref="UlidRouteConstraint"/>, permitiendo usar
    ///   <c>{tournamentId:ulid}</c> en las rutas de los controladores.</description></item>
    ///   <item><description>Configura <c>MvcOptions</c>:</description></item>
    /// </list>
    /// <para><b>Opciones de MVC:</b></para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Opción</term>
    ///     <description>Valor</description>
    ///     <description>Efecto</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>RespectBrowserAcceptHeader</c></term>
    ///     <description>true</description>
    ///     <description>Respeta el header <c>Accept</c> del cliente para determinar
    ///     el formato de respuesta (JSON, XML, etc.).</description>
    ///   </item>
    ///   <item>
    ///     <term><c>ReturnHttpNotAcceptable</c></term>
    ///     <description>true</description>
    ///     <description>Retorna HTTP 406 si el formato solicitado no está soportado,
    ///     en lugar de devolver el formato por defecto.</description>
    ///   </item>
    /// </list>
    /// <para><b>Opciones de serialización JSON:</b></para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Opción</term>
    ///     <description>Valor</description>
    ///     <description>Efecto</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>PropertyNamingPolicy</c></term>
    ///     <description>CamelCase</description>
    ///     <description>Las propiedades se serializan en camelCase (<c>errorId</c>, <c>errorType</c>).</description>
    ///   </item>
    ///   <item>
    ///     <term><c>WriteIndented</c></term>
    ///     <description>true</description>
    ///     <description>JSON indentado para legibilidad en desarrollo.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>PropertyNameCaseInsensitive</c></term>
    ///     <description>true</description>
    ///     <description>La deserialización no distingue mayúsculas/minúsculas en nombres de propiedad.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>JsonStringEnumConverter</c></term>
    ///     <description>Agregado</description>
    ///     <description>Los enums se serializan/deserializan como strings en lugar de números enteros.</description>
    ///   </item>
    /// </list>
    /// <para><b>Formateadores adicionales:</b></para>
    /// <list type="bullet">
    ///   <item><description><c>AddXmlSerializerFormatters()</c> — Soporte para XML mediante <c>System.Xml.Serialization.XmlSerializer</c>.</description></item>
    ///   <item><description><c>AddXmlDataContractSerializerFormatters()</c> — Soporte para XML mediante <c>DataContractSerializer</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <returns>Instancia de <c>IMvcBuilder</c> para configuraciones adicionales.</returns>
    public static IMvcBuilder AddMvcControllers(this IServiceCollection services)
    {
        services.Configure<RouteOptions>(options =>
        {
            options.ConstraintMap.Add("ulid", typeof(UlidRouteConstraint));
        });

        Log.Information("Configurando controladores MVC...");
        return services.AddControllers(options =>
        {
            options.RespectBrowserAcceptHeader = true;
            options.ReturnHttpNotAcceptable = true;
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = true;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        })
        .AddXmlSerializerFormatters()
        .AddXmlDataContractSerializerFormatters();
    }
}