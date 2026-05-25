using BackEncordados.Common.SignalR;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Configuración de SignalR para comunicación en tiempo real con los clientes,
/// incluyendo el hub de torneos y sus opciones de conexión.
/// </summary>
/// <remarks>
/// <para>Proporciona dos métodos de extensión para configurar SignalR:
/// <c>AddRealtimeSignalR</c> para el contenedor DI y <c>MapSignalRHubs</c>
/// para el pipeline de la aplicación.</para>
/// <para><b>Hub registrado:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Hub</term>
///     <description>Ruta</description>
///     <description>Autorización</description>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><see cref="SignalHub"/></term>
///     <description><c>/hub/Torneos</c></description>
///     <description><c>[Authorize(Policy = "RequireSupervisorRole")]</c></description>
///     <description>Notificaciones en tiempo real de torneos, pedidos y cambios de estado.
///     Agrupa clientes por torneo y por rol (Admin global).</description>
///   </item>
/// </list>
/// <para>Usar en <c>Program.cs</c>:</para>
/// <code>
/// builder.Services.AddRealtimeSignalR();
/// // ...
/// app.MapSignalRHubs(); // después de UseAuthorization()
/// app.Run();
/// </code>
/// </remarks>
public static class SignalRExtensions
{
    /// <summary>
    /// Configura y registra los servicios de SignalR con opciones personalizadas
    /// para el <see cref="SignalHub"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Opciones del hub configuradas:</b></para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Opción</term>
    ///     <description>Valor</description>
    ///     <description>Propósito</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>EnableDetailedErrors</c></term>
    ///     <description>true</description>
    ///     <description>Envía mensajes de error detallados al cliente
    ///     (útil en desarrollo; considerar deshabilitar en producción).</description>
    ///   </item>
    ///   <item>
    ///     <term><c>MaximumReceiveMessageSize</c></term>
    ///     <description>4096 bytes (4 KB)</description>
    ///     <description>Tamaño máximo de mensajes entrantes desde el cliente.
    ///     Suficiente para comandos simples de notificación.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>KeepAliveInterval</c></term>
    ///     <description>15 segundos</description>
    ///     <description>Intervalo de heartbeat del servidor al cliente para
    ///     detectar desconexiones tempranas.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <returns>La misma colección de servicios para encadenamiento fluido.</returns>
    public static IServiceCollection AddRealtimeSignalR(this IServiceCollection services) {
        services.AddSignalR()
            .AddHubOptions<SignalHub>(options => {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 1024 * 4;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });

        return services;
    }

    /// <summary>
    /// Mapea el endpoint del hub de SignalR en el pipeline de la aplicación.
    /// </summary>
    /// <remarks>
    /// <para>Registra <see cref="SignalHub"/> en la ruta <c>/hub/Torneos</c>.</para>
    /// <para><b>Ubicación en el pipeline:</b> Debe llamarse después de
    /// <c>app.UseAuthentication()</c> y <c>app.UseAuthorization()</c> para que
    /// el atributo <c>[Authorize]</c> del hub funcione correctamente.</para>
    /// </remarks>
    /// <param name="app">Constructor de la aplicación (IApplicationBuilder).</param>
    /// <returns>El mismo constructor para encadenamiento fluido.</returns>
    public static IApplicationBuilder MapSignalRHubs(this IApplicationBuilder app)
    {
        var webApp = (WebApplication)app;
        
        webApp.MapHub<SignalHub>("/hub/Torneos");

        return app;
    }
}