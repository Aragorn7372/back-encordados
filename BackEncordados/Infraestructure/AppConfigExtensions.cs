namespace BackEncordados.Infraestructure;

public static class AppConfigExtensions
{
    public static IServiceCollection AddAppConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var appSection = configuration.GetSection("App");
        
        var appOptions = new AppOptions
        {
            FrontendUrl = appSection["FrontendUrl"] ?? "http://localhost:3000",
            ServerUrl = appSection["ServerUrl"] ?? "",
            PasswordResetExpiryMinutes = int.Parse(appSection["PasswordResetExpiryMinutes"] ?? "60"),
            EmailOtpExpiryMinutes = int.Parse(appSection["EmailOtpExpiryMinutes"] ?? "15"),
            WhatsAppEnabled = bool.Parse(appSection["WhatsAppEnabled"] ?? "false"),
            WhatsAppPhoneNumberId = appSection["WhatsAppPhoneNumberId"] ?? "",
            WhatsAppAccessToken = appSection["WhatsAppAccessToken"] ?? "",
            WhatsAppApiVersion = appSection["WhatsAppApiVersion"] ?? "v25.0"
        };

        AppConfig.Current = appOptions;

        return services;
    }

    public static IServiceCollection AddWhatsAppHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var appSection = configuration.GetSection("App");
        var accessToken = appSection["WhatsAppAccessToken"] ?? "";

        services.AddHttpClient("WhatsApp")
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            });

        return services;
    }
}

