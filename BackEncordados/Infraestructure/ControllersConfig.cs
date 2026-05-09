using System.Text.Json;
using System.Text.Json.Serialization;
using BackEncordados.Infraestructure.Constraints;
using Microsoft.AspNetCore.Routing;
using Serilog;

namespace BackEncordados.Infraestructure;

public static class ControllersConfig
{
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