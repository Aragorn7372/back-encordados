using System.Text.Json;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Extensiones de configuración de controladores MVC y validación FluentValidation.
/// </summary>
public static class ControllersConfig
{
    /// <summary>
    /// Configura los controladores MVC con negociación de contenido.
    /// </summary>
    public static IMvcBuilder AddMvcControllers(this IServiceCollection services)
    {
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
            })
            .AddXmlSerializerFormatters()
            .AddXmlDataContractSerializerFormatters();
    }
}