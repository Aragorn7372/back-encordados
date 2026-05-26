using System.Threading.Channels;
using BackEncordados.Common.Service.Email;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Extensiones de configuración de servicios de email.
/// </summary>
public static class EmailConfig
{
    /// <summary>
    /// Configura el servicio de email.
    /// Desarrollo: MemoryEmailService (no envía realmente).
    /// Producción: MailKitEmailService (envía emails reales).
    /// </summary>
    public static IServiceCollection AddEmail(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());

        if (environment.IsDevelopment())
        {
            Log.Information("Configurando servicio de email en memoria (desarrollo local)...");
            services.TryAddScoped<IEmailService, MemoryEmailService>();
        }
        else
        {
            Log.Information("Configurando servicio de email con MailKit (producción)...");
            services.TryAddScoped<IEmailService, MailKitEmailService>();
            services.AddHostedService<EmailBackgroundService>();
        }

        return services;
    }
}