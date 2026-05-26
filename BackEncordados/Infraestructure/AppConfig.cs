namespace BackEncordados.Infraestructure;

/// <summary>
/// Punto de acceso estático a la configuración global de la aplicación,
/// poblada desde <c>appsettings.json</c> durante el arranque.
/// </summary>
/// <remarks>
/// <para>Proporciona acceso singleton a <see cref="AppOptions"/> mediante la
/// propiedad <see cref="Current"/>, inicializada por el método de extensión
/// <c>AddAppConfig()</c> en la configuración de servicios de <c>Program.cs</c>.</para>
/// <para>Este patrón evita tener que inyectar <c>IOptions&lt;AppOptions&gt;</c>
/// en cada clase que necesite configuración, facilitando el acceso desde
/// servicios, middlewares y clases estáticas como <see cref="Common.Service.Cloudinary.CloudinaryConstants"/>
/// o <see cref="Common.Service.WhatsApp.WhatsAppService"/>.</para>
/// <para><b>Inicialización típica en Program.cs:</b></para>
/// <code>
/// builder.Services.AddAppConfig(builder.Configuration);
/// // AppConfig.Current ya está disponible globalmente
/// </code>
/// </remarks>
public static class AppConfig
{
    /// <summary>
    /// Instancia única de la configuración global, asignada durante el arranque
    /// de la aplicación por <c>AddAppConfig()</c>.
    /// </summary>
    public static AppOptions Current { get; set; } = new();
}

/// <summary>
/// Opciones de configuración de la aplicación leídas desde la sección <c>"App"</c>
/// del archivo <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// <para>POCO con valores predeterminados que se sobreescriben desde la configuración
/// durante el arranque. Incluye ajustes de URLs del frontend, expiración de tokens
/// temporales y credenciales de la API de WhatsApp Cloud.</para>
/// <para><b>Mapeo con appsettings.json:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Propiedad</term>
///     <description>Clave en JSON</description>
///     <description>Default</description>
///     <description>Uso</description>
///   </listheader>
///   <item>
///     <term><see cref="FrontendUrl"/></term>
///     <description>App:FrontendUrl</description>
///     <description>http://localhost:3000</description>
///     <description>URL base del frontend para enlaces en correos (reset password, confirmación).</description>
///   </item>
///   <item>
///     <term><see cref="ServerUrl"/></term>
///     <description>App:ServerUrl</description>
///     <description>""</description>
///     <description>URL base del servidor para enlaces de retorno.</description>
///   </item>
///   <item>
///     <term><see cref="PasswordResetExpiryMinutes"/></term>
///     <description>App:PasswordResetExpiryMinutes</description>
///     <description>60</description>
///     <description>Minutos de validez del token de reset de contraseña.</description>
///   </item>
///   <item>
///     <term><see cref="EmailOtpExpiryMinutes"/></term>
///     <description>App:EmailOtpExpiryMinutes</description>
///     <description>15</description>
///     <description>Minutos de validez del OTP enviado por email.</description>
///   </item>
///   <item>
///     <term><see cref="WhatsAppEnabled"/></term>
///     <description>App:WhatsAppEnabled</description>
///     <description>false</description>
///     <description>Habilita/deshabilita el envío de mensajes por WhatsApp Cloud API.</description>
///   </item>
///   <item>
///     <term><see cref="WhatsAppPhoneNumberId"/></term>
///     <description>App:WhatsAppPhoneNumberId</description>
///     <description>""</description>
///     <description>ID del número de teléfono registrado en Meta Business.</description>
///   </item>
///   <item>
///     <term><see cref="WhatsAppAccessToken"/></term>
///     <description>App:WhatsAppAccessToken</description>
///     <description>""</description>
///     <description>Token de acceso Bearer para autenticación contra Graph API de Meta.</description>
///   </item>
///   <item>
///     <term><see cref="WhatsAppApiVersion"/></term>
///     <description>App:WhatsAppApiVersion</description>
///     <description>v25.0</description>
///     <description>Versión de la Graph API de Meta (ej: v25.0, v26.0).</description>
///   </item>
/// </list>
/// <para><b>Nota de seguridad:</b> Las propiedades <see cref="WhatsAppAccessToken"/>
/// contienen credenciales sensibles. En producción deben almacenarse en
/// <c>User Secrets</c>, <c>Azure Key Vault</c> o variables de entorno,
/// no en el archivo <c>appsettings.json</c> directamente.</para>
/// </remarks>
public class AppOptions
{
    /// <summary>URL base del frontend para enlaces en correos electrónicos (reset password, etc.).</summary>
    public string FrontendUrl { get; set; } = "http://localhost:3000";

    /// <summary>URL base del servidor backend para enlaces de retorno.</summary>
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>Tiempo de expiración del token de restablecimiento de contraseña en minutos (default: 60).</summary>
    public int PasswordResetExpiryMinutes { get; set; } = 60;

    /// <summary>Tiempo de expiración del OTP enviado por email en minutos (default: 15).</summary>
    public int EmailOtpExpiryMinutes { get; set; } = 15;

    /// <summary>Habilita o deshabilita el envío de notificaciones por WhatsApp Cloud API.</summary>
    public bool WhatsAppEnabled { get; set; } = false;

    /// <summary>ID del número de teléfono registrado en Meta Business para WhatsApp Business API.</summary>
    public string WhatsAppPhoneNumberId { get; set; } = string.Empty;

    /// <summary>Token de acceso Bearer para autenticación contra la Graph API de Meta.</summary>
    public string WhatsAppAccessToken { get; set; } = string.Empty;

    /// <summary>Versión de la Graph API de Meta (ej: "v25.0", "v26.0").</summary>
    public string WhatsAppApiVersion { get; set; } = "v25.0";
}

