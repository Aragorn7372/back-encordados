using BackEncordados.Common.Database.Config;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Configuración de bases de datos.
/// </summary>
/// <remarks>
/// Configura los DbContext y Identity para usuarios.
///
/// <para><b>Dependencias:</b></para>
/// <list type="bullet">
///     <item>IConfiguration: Configuración de la app</item>
/// </list>
/// 
/// <para><b>Características:</b></para>
/// <list type="bullet">
///     <item>Productos: InMemory (dev) o PostgreSQL (prod)</li>
///     <item>Usuarios: Entity Framework Core con Identity</item>
/// </list>
/// 
/// <para><b>Configuración de contraseña:</b></para>
/// <list type="bullet">
///     <item>RequireDigit: true</item>
///     <item>RequiredLength: 6</item>
///     <item>RequireNonAlphanumeric: true</item>
///     <item>RequireUppercase: true</item>
/// </list>
/// </remarks>
public static class DbConfig
{
    /// <summary>
    /// Configura los servicios de base de datos.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    ///     <item>Desarrollo: UseInMemoryDatabase</item>
    ///     <item>Producción: UseNpgsql (PostgreSQL)</item>
    ///     <item>Configura Identity para usuarios</item>
    /// </list>
    /// </remarks>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la app.</param>
    /// <returns>IServiceCollection.</returns>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        //BBDD de Productos y categorías
        services.AddDbContext<UserDbContext>(options =>
        {
            var isDevelopment = configuration.GetValue<bool?>("Development") ?? true;
            
            if(isDevelopment) options.UseInMemoryDatabase("UserDatabase");
            else
            {
                Log.Information("modo produccion activado conectando a base de datos");
                var connectionString = configuration["DATABASE_URL_USER"] 
                                       ?? configuration.GetConnectionString("DefaultConnection") 
                                       ?? throw new InvalidOperationException("No se encontrado el DATABASE_URL");
                options.UseNpgsql(connectionString);
                options.EnableSensitiveDataLogging(); 
                options.EnableDetailedErrors(); 
            }
        });
        services.AddDbContext<PedidosDbContext>(options =>
        {
            var isDevelopment = configuration.GetValue<bool?>("Development") ?? true;
            
            if(isDevelopment) options.UseInMemoryDatabase("PedidosDatabase");
            else
            {
                Log.Information("modo produccion activado conectando a base de datos");
                var connectionString = configuration["DATABASE_URL_PEDIDOS"] 
                                       ?? configuration.GetConnectionString("DefaultConnection") 
                                       ?? throw new InvalidOperationException("No se encontrado el DATABASE_URL");
                options.UseNpgsql(connectionString);
                options.EnableSensitiveDataLogging(); 
                options.EnableDetailedErrors(); 
            }
        });
        services.AddDbContext<PartidosDbContext>(options =>
        {
            var isDevelopment = configuration.GetValue<bool?>("Development") ?? true;
            
            if(isDevelopment) options.UseInMemoryDatabase("PartidosDatabase");
            else
            {
                Log.Information("modo produccion activado conectando a base de datos");
                var connectionString = configuration["DATABASE_URL_PARTIDOS"] 
                                       ?? configuration.GetConnectionString("DefaultConnection") 
                                       ?? throw new InvalidOperationException("No se encontrado el DATABASE_URL");
                options.UseNpgsql(connectionString);
                options.EnableSensitiveDataLogging(); 
                options.EnableDetailedErrors(); 
            }
        });

        
        return services;
        
        
    }
}