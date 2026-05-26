using BackEncordados.Common.Service.WhatsApp;
using BackEncordados.Excel.Archive;
using BackEncordados.Excel.Service;
using BackEncordados.Export.Archive;
using BackEncordados.Export.Service;
using BackEncordados.Materials.Service.Cuerdas;
using BackEncordados.Materials.Service.Materials;
using BackEncordados.Purchased.Service;
using BackEncordados.Talleres.Service;
using BackEncordados.Usuarios.Service.Auth;
using BackEncordados.Usuarios.Service.CrudService;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Configuración y registro de todos los servicios de lógica de negocio
/// de la aplicación en el contenedor de inyección de dependencias.
/// </summary>
/// <remarks>
/// <para>Proporciona un método de extensión sobre <c>IServiceCollection</c> que
/// registra 13 servicios como <c>Scoped</c> mediante encadenamiento fluido.</para>
/// <para><b>Servicios registrados:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Interfaz</term>
///     <description>Implementación</description>
///     <description>Módulo</description>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>IJwtService</c></term>
///     <description>JwtService</description>
///     <description>Auth</description>
///     <description>Generación y validación de tokens JWT.</description>
///   </item>
///   <item>
///     <term><c>IJwtTokenExtractor</c></term>
///     <description>JwtTokenExtractor</description>
///     <description>Auth</description>
///     <description>Extracción de claims (userId, role) desde tokens JWT.</description>
///   </item>
///   <item>
///     <term><c>IAuthService</c></term>
///     <description>AuthService</description>
///     <description>Auth</description>
///     <description>Login, registro, refresh token y recuperación de contraseña.</description>
///   </item>
///   <item>
///     <term><c>IUserService</c></term>
///     <description>UserService</description>
///     <description>Usuarios</description>
///     <description>CRUD de usuarios y contactos.</description>
///   </item>
///   <item>
///     <term><c>ITournamentService</c></term>
///     <description>TournamentService</description>
///     <description>Talleres</description>
///     <description>CRUD de torneos con asignación de trabajadores.</description>
///   </item>
///   <item>
///     <term><c>IPurchasedService</c></term>
///     <description>PurchasedService</description>
///     <description>Pedidos</description>
///     <description>CRUD de pedidos y líneas de pedido.</description>
///   </item>
///   <item>
///     <term><c>ICuerdasService</c></term>
///     <description>CuerdasService</description>
///     <description>Materials</description>
///     <description>CRUD de cuerdas (calibre, formato, tipo).</description>
///   </item>
///   <item>
///     <term><c>IMaterialsService</c></term>
///     <description>MaterialsService</description>
///     <description>Materials</description>
///     <description>CRUD de materiales de encordado.</description>
///   </item>
///   <item>
///     <term><c>IExportArchiveManager</c></term>
///     <description>ExportArchiveManager</description>
///     <description>Export</description>
///     <description>Creación y extracción de archivos ZIP con JSON de BD.</description>
///   </item>
///   <item>
///     <term><c>IExportService</c></term>
///     <description>ExportService</description>
///     <description>Export</description>
///     <description>Orquestación de exportación/importación completa de BD.</description>
///   </item>
///   <item>
///     <term><c>IExcelArchiveManager</c></term>
///     <description>ExcelArchiveManager</description>
///     <description>Excel</description>
///     <description>Creación y lectura de archivos Excel mediante ClosedXML.</description>
///   </item>
///   <item>
///     <term><c>IExcelService</c></term>
///     <description>ExcelService</description>
///     <description>Excel</description>
///     <description>Orquestación de exportación/importación de datos Excel.</description>
///   </item>
///   <item>
///     <term><c>IWhatsAppService</c></term>
///     <description>WhatsAppService</description>
///     <description>WhatsApp</description>
///     <description>Envío de notificaciones por WhatsApp Cloud API (fail-safe).</description>
///   </item>
/// </list>
/// <para>Todos los servicios se registran como <c>Scoped</c> (una instancia por
/// solicitud HTTP). Usar <c>services.AddServices()</c> en <c>Program.cs</c>.</para>
/// </remarks>
public static class ServicesConfig
{
    /// <summary>
    /// Registra todos los servicios de lógica de negocio como <c>Scoped</c>
    /// en el contenedor de DI mediante encadenamiento fluido.
    /// </summary>
    /// <remarks>
    /// <para>Registra 13 servicios desde 6 módulos del sistema:
    /// Auth, Usuarios, Talleres, Purchased, Materials, Export, Excel y WhatsApp.</para>
    /// <para><b>Flujo:</b> Cada servicio se agrega a la colección con <c>AddScoped</c>
    /// y se retorna <c>services</c> para continuar el encadenamiento.</para>
    /// </remarks>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <returns>La misma colección de servicios con todos los servicios registrados.</returns>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        Log.Information("Registrando servicios...");
        return services
            .AddScoped<IJwtService, JwtService>()
            .AddScoped<IJwtTokenExtractor, JwtTokenExtractor>()
            .AddScoped<IAuthService, AuthService>()
            .AddScoped<IUserService, UserService>()
            .AddScoped<ITournamentService, TournamentService>()
            .AddScoped<IPurchasedService, PurchasedService>()
            .AddScoped<ICuerdasService, CuerdasService>()
            .AddScoped<IMaterialsService, MaterialsService>()
            .AddScoped<IExportArchiveManager, ExportArchiveManager>()
            .AddScoped<IExportService, ExportService>()
            .AddScoped<IExcelArchiveManager, ExcelArchiveManager>()
            .AddScoped<IExcelService, ExcelService>()
            .AddScoped<IWhatsAppService, WhatsAppService>();
    }
}