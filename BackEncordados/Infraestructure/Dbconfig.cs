using BackEncordados.Common.Database.Config;
using BackEncordados.Common.Database.Helpers;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using Npgsql;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Configuración de bases de datos.
/// </summary>
/// <remarks>
/// Configura los DbContext e Identity para usuarios.
///
/// <para><b>Dependencias:</b></para>
/// <list type="bullet">
///     <item>IConfiguration: Configuración de la app</item>
/// </list>
/// 
    /// <para><b>Características:</b></para>
/// <list type="bullet">
///     <item>Usuarios: InMemory (dev) o PostgreSQL (prod)</item>
///     <item>Pedidos: InMemory (dev) o MongoDB (prod)</item>
///     <item>Talleres: InMemory (dev) o MongoDB (prod)</item>
///     <item>Materials: InMemory (dev) o PostgreSQL (prod)</item>
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
    ///     <item>Producción: UseNpgsql con PostgreSQL</item>
    ///     <item>Configura Identity para usuarios</item>
    /// </list>
    /// </remarks>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la app.</param>
    /// <returns>IServiceCollection.</returns>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
//BBDD de Usuarios
        Log.Information("MONGODB_URI_TALLERES: {value}", configuration["MONGODB_URI_TALLERES"]);
        Log.Information("MONGODB_URI_PEDIDOS: {value}", configuration["MONGODB_URI_PEDIDOS"]);
          services.AddDbContext<UserDbContext>(options =>
          {
              var isDevelopment = configuration.GetValue<bool?>("Development") ?? true;
              
              options.AddInterceptors(new TimestampInterceptor());
              options.AddInterceptors(new VersionInterceptor());
             
             if(isDevelopment) options.UseInMemoryDatabase("UserDatabase");
             else
             {
                 Log.Information("Modo producción activado - Conectando a PostgreSQL (Usuarios)");
                 var connectionString = configuration["DATABASE_URL_USER"] 
                                        ?? configuration.GetConnectionString("DefaultConnection") 
                                        ?? throw new InvalidOperationException("No se encontró DATABASE_URL_USER o DefaultConnection");
                 options.UseNpgsql(connectionString);
                 options.EnableSensitiveDataLogging(); 
                 options.EnableDetailedErrors(); 
             }
         });
         services.AddDbContext<MaterialsDbContext>(options =>
         {
             var isDevelopment = configuration.GetValue<bool?>("Development") ?? true;
             
             options.AddInterceptors(new TimestampInterceptor());
             
             if(isDevelopment) options.UseInMemoryDatabase("MaterialsDatabase");
             else
             {
                 Log.Information("Modo producción activado - Conectando a PostgreSQL (Materials)");
                 var connectionString = configuration["DATABASE_URL_MATERIALS"] 
                                        ?? configuration.GetConnectionString("DefaultConnection") 
                                        ?? throw new InvalidOperationException("No se encontró DATABASE_URL_MATERIALS o DefaultConnection");
                 options.UseNpgsql(connectionString);
                 options.EnableSensitiveDataLogging(); 
                 options.EnableDetailedErrors(); 
             }
         });
         //BBDD de Pedidos - MongoDB Atlas
         services.AddDbContext<PedidosDbContext>(options =>
         {
             var isDevelopment = configuration.GetValue<bool?>("Development") ?? true;
             
             options.AddInterceptors(new TimestampInterceptor());
             
             if(isDevelopment) 
             {
                 options.UseInMemoryDatabase("PedidosDatabase");
             }
             else
             {
                    Log.Information("Modo producción activado - Conectando a MongoDB (Pedidos)");
                   var mongoConnectionStringPedidos = configuration["MONGODB_URI_PEDIDOS"] 
                                               ?? configuration["MONGODB_URI"]
                                               ?? throw new InvalidOperationException("No se encontró MONGODB_URI_PEDIDOS o MONGODB_URI");
                   options.UseMongoDB(mongoConnectionStringPedidos, "pedidos_db");
                   options.EnableSensitiveDataLogging(); 
                   options.EnableDetailedErrors();
             }
         });
         
         //BBDD de Talleres - MongoDB Atlas
         services.AddDbContext<TalleresDbContext>(options =>
         {
             var isDevelopment = configuration.GetValue<bool?>("Development") ?? true;
             
             options.AddInterceptors(new TimestampInterceptor());
             
             if(isDevelopment) 
             {
                 options.UseInMemoryDatabase("PartidosDatabase");
             }
             else
             {
                    Log.Information("Modo producción activado - Conectando a MongoDB (Talleres)");
                   var mongoConnectionStringTalleres = configuration["MONGODB_URI_TALLERES"] 
                                               ?? configuration["MONGODB_URI"]
                                               ?? throw new InvalidOperationException("No se encontró MONGODB_URI_TALLERES o MONGODB_URI");
                   options.UseMongoDB(mongoConnectionStringTalleres, "talleres_db");
                   options.EnableSensitiveDataLogging(); 
                   options.EnableDetailedErrors();
             }
         });

        
        return services;
    }
}