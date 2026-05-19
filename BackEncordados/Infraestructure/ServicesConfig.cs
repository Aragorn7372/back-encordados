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
/// Extensiones de configuración de servicios de negocio.
/// </summary>
public static class ServicesConfig
{
    /// <summary>
    /// Registra todos los servicios de negocio en el contenedor de dependencias.
    /// </summary>
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