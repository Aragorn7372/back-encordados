using BackEncordados.Common.Database.Config;
using BackEncordados.Common.Database.Helpers;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using Npgsql;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Configuración de los cuatro DbContexts de la aplicación con soporte
/// para bases de datos en memoria (desarrollo) y producción (PostgreSQL / MongoDB).
/// </summary>
/// <remarks>
/// <para>Registra los cuatro DbContexts usados por el sistema, cada uno con
/// un proveedor de base de datos diferente dependiendo del modo de ejecución:</para>
/// <list type="table">
///   <listheader>
///     <term>DbContext</term>
///     <description>Desarrollo (dev)</description>
///     <description>Producción (prod)</description>
///     <description>Interceptores</description>
///   </listheader>
///   <item>
///     <term><c>UserDbContext</c></term>
///     <description>InMemory (UserDatabase)</description>
///     <description>PostgreSQL (Npgsql) — <c>DATABASE_URL_USER</c> → <c>DefaultConnection</c></description>
///     <description>TimestampInterceptor, VersionInterceptor</description>
///   </item>
///   <item>
///     <term><c>MaterialsDbContext</c></term>
///     <description>InMemory (MaterialsDatabase)</description>
///     <description>PostgreSQL (Npgsql) — <c>DATABASE_URL_MATERIALS</c> → <c>DefaultConnection</c></description>
///     <description>TimestampInterceptor</description>
///   </item>
///   <item>
///     <term><c>PedidosDbContext</c></term>
///     <description>InMemory (PedidosDatabase)</description>
///     <description>MongoDB — <c>MONGODB_URI_PEDIDOS</c> → <c>MONGODB_URI</c> (db: pedidos_db)</description>
///     <description>TimestampInterceptor</description>
///   </item>
///   <item>
///     <term><c>TalleresDbContext</c></term>
///     <description>InMemory (PartidosDatabase)</description>
///     <description>MongoDB — <c>MONGODB_URI_TALLERES</c> → <c>MONGODB_URI</c> (db: talleres_db)</description>
///     <description>TimestampInterceptor</description>
///   </item>
/// </list>
/// <para><b>Configuración del modo Development:</b></para>
/// <para>Se controla mediante la clave <c>"Development"</c> en appsettings.json.
/// Por defecto es <c>true</c>. En producción debe setearse a <c>false</c>.</para>
/// <para><b>Interceptores comunes:</b></para>
/// <list type="bullet">
///   <item><description><c>TimestampInterceptor</c> — Asigna automáticamente <c>CreatedAt</c> y <c>UpdatedAt</c>
///   en entidades que implementan <see cref="ITimestamped"/>.</description></item>
///   <item><description><c>VersionInterceptor</c> — Solo en UserDbContext. Autoincrementa la propiedad
///   <c>Version</c> del usuario para control de concurrencia optimista.</description></item>
/// </list>
/// <para><b>Resolución de connection strings:</b></para>
/// <para>Cada DbContext tiene su propia variable de entorno con fallback.
/// Las variables se resuelven en este orden:</para>
/// <list type="bullet">
///   <item><description>UserDbContext: <c>DATABASE_URL_USER</c> → <c>DefaultConnection</c></description></item>
///   <item><description>MaterialsDbContext: <c>DATABASE_URL_MATERIALS</c> → <c>DefaultConnection</c></description></item>
///   <item><description>PedidosDbContext: <c>MONGODB_URI_PEDIDOS</c> → <c>MONGODB_URI</c></description></item>
///   <item><description>TalleresDbContext: <c>MONGODB_URI_TALLERES</c> → <c>MONGODB_URI</c></description></item>
/// </list>
/// <para>Usar <c>services.AddDatabase(configuration)</c> en <c>Program.cs</c>.</para>
/// </remarks>
public static class DbConfig
{
    /// <summary>
    /// Configura y registra los cuatro DbContexts de la aplicación con el proveedor
    /// de base de datos correspondiente según el modo de ejecución.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Registra <c>UserDbContext</c> (PostgreSQL en prod, InMemory en dev)
    ///   con <c>TimestampInterceptor</c> y <c>VersionInterceptor</c>.</description></item>
    ///   <item><description>Registra <c>MaterialsDbContext</c> (PostgreSQL en prod, InMemory en dev)
    ///   con <c>TimestampInterceptor</c>.</description></item>
    ///   <item><description>Registra <c>PedidosDbContext</c> (MongoDB en prod, InMemory en dev)
    ///   con <c>TimestampInterceptor</c>. En prod usa <c>UseMongoDB</c> con base <c>"pedidos_db"</c>.</description></item>
    ///   <item><description>Registra <c>TalleresDbContext</c> (MongoDB en prod, InMemory en dev)
    ///   con <c>TimestampInterceptor</c>. En prod usa <c>UseMongoDB</c> con base <c>"talleres_db"</c>.</description></item>
    /// </list>
    /// <para>En producción, se habilitan <c>EnableSensitiveDataLogging()</c> y
    /// <c>EnableDetailedErrors()</c> para facilitar la depuración.</para>
    /// </remarks>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <param name="configuration">Configuración de la aplicación (appsettings.json + env vars).</param>
    /// <returns>La misma colección de servicios para encadenamiento fluido.</returns>
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
                  connectionString += ";Pooling=false;Maximum Pool Size=5;Command Timeout=60";
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
                  connectionString += ";Pooling=false;Maximum Pool Size=5;Command Timeout=60";
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