using BackEncordados.Common.Database.Config;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Extension methods para inicialización de base de datos.
/// </summary>
public static class DatabaseInitializationExtensions
{
    /// <summary>
    /// Inicializa la base de datos PostgreSQL y MongoDB.
    /// Desarrollo: Elimina y recrea la BD, siembra datos.
    /// Producción: Solo crea tablas si no existen.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        Log.Information("Inicializando base de datos...");

        using var scope = app.Services.CreateScope();
        var user = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var partidos = scope.ServiceProvider.GetRequiredService<PartidosDbContext>();
        var pedidos = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        user.Database.EnsureCreatedAsync();
        partidos.Database.EnsureCreatedAsync();
        pedidos.Database.EnsureCreatedAsync();
        
        logger.LogInformation("Base de datos verificada (tablas creadas si no existían)");
        
    }
}