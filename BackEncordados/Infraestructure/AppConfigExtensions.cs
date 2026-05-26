namespace BackEncordados.Infraestructure;

/// <summary>
/// Métodos de extensión para <c>IServiceCollection</c> que registran la configuración
/// global de la aplicación y los clientes HTTP necesarios.
/// </summary>
/// <remarks>
/// <para>Deben llamarse durante la configuración de servicios en <c>Program.cs</c>
/// antes de <c>builder.Build()</c>:</para>
/// <code>
/// builder.Services.AddAppConfig(builder.Configuration);
/// builder.Services.AddWhatsAppHttpClient(builder.Configuration);
/// </code>
/// </remarks>
public static class AppConfigExtensions
{
    /// <summary>
    /// Lee la sección <c>"App"</c> de la configuración, construye un <see cref="AppOptions"/>
    /// y lo asigna a <see cref="AppConfig.Current"/> para acceso global estático.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Obtiene la sección <c>"App"</c> del <paramref name="configuration"/>.</description></item>
    ///   <item><description>Construye un <see cref="AppOptions"/> leyendo cada propiedad desde
    ///   la configuración con parseo manual (<c>int.Parse</c>, <c>bool.Parse</c>).</description></item>
    ///   <item><description>Si una clave no existe en la configuración, se usa el valor predeterminado
    ///   definido en la propiedad del POCO (ej: <c>FrontendUrl</c> → <c>"http://localhost:3000"</c>).</description></item>
    ///   <item><description>Asigna la instancia a <c>AppConfig.Current</c> para que esté disponible
    ///   globalmente sin necesidad de inyectar <c>IOptions&lt;AppOptions&gt;</c>.</description></item>
    ///   <item><description>Retorna <paramref name="services"/> para permitir encadenamiento fluido.</description></item>
    /// </list>
    /// <para><b>Mapeo detallado de propiedades:</b></para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Propiedad de AppOptions</term>
    ///     <description>Clave en appsettings.json</description>
    ///     <description>Parseo</description>
    ///     <description>Valor predeterminado</description>
    ///   </listheader>
    ///   <item>
    ///     <term>FrontendUrl</term>
    ///     <description>App:FrontendUrl</description>
    ///     <description>string directo</description>
    ///     <description>"http://localhost:3000"</description>
    ///   </item>
    ///   <item>
    ///     <term>ServerUrl</term>
    ///     <description>App:ServerUrl</description>
    ///     <description>string directo</description>
    ///     <description>""</description>
    ///   </item>
    ///   <item>
    ///     <term>PasswordResetExpiryMinutes</term>
    ///     <description>App:PasswordResetExpiryMinutes</description>
    ///     <description>int.Parse</description>
    ///     <description>60</description>
    ///   </item>
    ///   <item>
    ///     <term>EmailOtpExpiryMinutes</term>
    ///     <description>App:EmailOtpExpiryMinutes</description>
    ///     <description>int.Parse</description>
    ///     <description>15</description>
    ///   </item>
    ///   <item>
    ///     <term>WhatsAppEnabled</term>
    ///     <description>App:WhatsAppEnabled</description>
    ///     <description>bool.Parse</description>
    ///     <description>false</description>
    ///   </item>
    ///   <item>
    ///     <term>WhatsAppPhoneNumberId</term>
    ///     <description>App:WhatsAppPhoneNumberId</description>
    ///     <description>string directo</description>
    ///     <description>""</description>
    ///   </item>
    ///   <item>
    ///     <term>WhatsAppAccessToken</term>
    ///     <description>App:WhatsAppAccessToken</description>
    ///     <description>string directo</description>
    ///     <description>""</description>
    ///   </item>
    ///   <item>
    ///     <term>WhatsAppApiVersion</term>
    ///     <description>App:WhatsAppApiVersion</description>
    ///     <description>string directo</description>
    ///     <description>"v25.0"</description>
    ///   </item>
    /// </list>
    /// <para><b>Nota técnica:</b> Se usa parseo manual (<c>int.Parse</c>, <c>bool.Parse</c>)
    /// en lugar de <c>configuration.Bind()</c> para tener control explícito sobre los
    /// valores predeterminados y el manejo de errores de parseo. Si una clave existe
    /// pero tiene un valor inválido, se lanzará una excepción de formato.</para>
    /// </remarks>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <param name="configuration">Configuración de la aplicación (IConfiguration root).</param>
    /// <returns>La misma colección de servicios para encadenamiento fluido.</returns>
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

    /// <summary>
    /// Registra un <c>HttpClient</c> nombrado <c>"WhatsApp"</c> con autenticación
    /// Bearer para consumir la WhatsApp Cloud API de Meta.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Lee <c>WhatsAppAccessToken</c> de la sección <c>"App"</c> de la configuración.</description></item>
    ///   <item><summary>Registra un <c>HttpClient</c> con nombre <c>"WhatsApp"</c> mediante
    ///   <c>services.AddHttpClient("WhatsApp")</c>.</description></item>
    ///   <item><description>Configura el header <c>Authorization: Bearer {token}</c> globalmente
    ///   en todas las solicitudes que use este cliente.</description></item>
    ///   <item><description>Retorna <paramref name="services"/> para encadenamiento fluido.</description></item>
    /// </list>
    /// <para><b>Uso en servicios:</b> El <c>HttpClient</c> nombrado es consumido por
    /// <see cref="Common.Service.WhatsApp.WhatsAppService"/> mediante inyección de
    /// <c>IHttpClientFactory</c>:</para>
    /// <code>
    /// var client = _httpClientFactory.CreateClient("WhatsApp");
    /// // client ya tiene Authorization: Bearer {token}
    /// </code>
    /// <para><b>Nota de seguridad:</b> El token de acceso de WhatsApp es una credencial
    /// sensible. En desarrollo puede almacenarse en <c>User Secrets</c>; en producción
    /// debe obtenerse de <c>Azure Key Vault</c>, variables de entorno o un proveedor
    /// de secretos similar.</para>
    /// </remarks>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <param name="configuration">Configuración de la aplicación (IConfiguration root).</param>
    /// <returns>La misma colección de servicios para encadenamiento fluido.</returns>
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

