using BackEncordados.Common.Database.Config;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BackEncordados.Infraestructure;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        Log.Information("Inicializando base de datos...");

        using var scope = app.Services.CreateScope();
        var user = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var partidos = scope.ServiceProvider.GetRequiredService<TalleresDbContext>();
        var pedidos = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
        var materials= scope.ServiceProvider.GetRequiredService<MaterialsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var isDevelopment = app.Configuration.GetValue<bool>("Development");

        if (isDevelopment)
        {
            await materials.Database.EnsureCreatedAsync();
            await user.Database.EnsureCreatedAsync();
        }
        else
        {
            await materials.Database.MigrateAsync();
            await user.Database.MigrateAsync();
        }

        await partidos.Database.EnsureCreatedAsync();
        await pedidos.Database.EnsureCreatedAsync();
        
        logger.LogInformation("Base de datos verificada (tablas creadas si no existían)");
    }
}
