namespace BackEncordados.Infraestructure;

/// <summary>
/// Clase estática para acceder a configuraciones del appsettings
/// </summary>
public static class AppConfig
{
    public static AppOptions Current { get; set; } = new();
}

public class AppOptions
{
    public string FrontendUrl { get; set; } = "http://localhost:3000";
    public string ServerUrl { get; set; } = string.Empty;
    public int PasswordResetExpiryMinutes { get; set; } = 60;
    public int EmailOtpExpiryMinutes { get; set; } = 15;
    public bool WhatsAppEnabled { get; set; } = false;
    public string WhatsAppPhoneNumberId { get; set; } = string.Empty;
    public string WhatsAppAccessToken { get; set; } = string.Empty;
    public string WhatsAppApiVersion { get; set; } = "v25.0";
}

