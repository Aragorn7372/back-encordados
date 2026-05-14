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
            EmailOtpExpiryMinutes = int.Parse(appSection["EmailOtpExpiryMinutes"] ?? "15")
        };

        AppConfig.Current = appOptions;

        return services;
    }
}

