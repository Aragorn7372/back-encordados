using BackEncordados.Common.Database.Config;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Métodos de extensión para inicialización de las bases de datos al arrancar
/// la aplicación.
/// </summary>
/// <remarks>
/// <para>Proporciona un método de extensión sobre <c>WebApplication</c> que
/// garantiza que las tablas de los cuatro DbContexts existan al iniciar
/// la aplicación, creándolas si no existen.</para>
/// <para>Usar <c>await app.InitializeDatabaseAsync()</c> en <c>Program.cs</c>
/// después de <c>builder.Build()</c> y antes de <c>app.Run()</c>.</para>
/// </remarks>
public static class DatabaseInitializationExtensions
{
    /// <summary>
    /// Verifica que las tablas de los cuatro DbContexts existan, creándolas
    /// si es necesario.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Crea un <c>IServiceScope</c> a partir del contenedor de DI
    ///   para resolver servicios con ciclo de vida Scoped.</description></item>
    ///   <item><description>Resuelve los cuatro DbContexts: <see cref="UserDbContext"/>,
    ///   <see cref="TalleresDbContext"/>, <see cref="PedidosDbContext"/>,
    ///   <see cref="MaterialsDbContext"/>.</description></item>
    ///   <item><description>Resuelve un logger con categoría <c>Program</c> para registrar
    ///   el resultado de la inicialización.</description></item>
    ///   <item><description>Llama a <c>Database.EnsureCreatedAsync()</c> en cada DbContext.</description></item>
    ///   <item><description>Registra que la base de datos fue verificada exitosamente.</description></item>
    /// </list>
    /// <para><b>DbContexts inicializados:</b></para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>DbContext</term>
    ///     <description>Tablas creadas</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>MaterialsDbContext</c></term>
    ///     <description>Materiales, Cuerdas (con datos semilla para 5 torneos)</description>
    ///   </item>
    ///   <item>
    ///     <term><c>UserDbContext</c></term>
    ///     <description>Users (con índice único en Email y Username)</description>
    ///   </item>
    ///   <item>
    ///     <term><c>TalleresDbContext</c></term>
    ///     <description>Partidos (Tournaments) con <c>OwnsMany WorkerMachineAssignments</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>PedidosDbContext</c></term>
    ///     <description>Pedidos, PedidoLineas</description>
    ///   </item>
    /// </list>
    /// <para><b>Nota técnica:</b> <c>EnsureCreatedAsync()</c> crea la base de datos y todas
    /// las tablas si no existen. A diferencia de <c>EnsureDeletedAsync</c> + <c>EnsureCreatedAsync</c>,
    /// no elimina datos existentes. Es seguro para arranques repetidos de la aplicación.
    /// En producción, se recomienda usar migraciones (<c>MigrateAsync()</c>) en lugar de
    /// <c>EnsureCreatedAsync()</c> para control de cambios en el esquema.</para>
    /// </remarks>
    /// <param name="app">Instancia de <c>WebApplication</c> en construcción.</param>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        Log.Information("Inicializando base de datos...");

        using var scope = app.Services.CreateScope();
        var user = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var partidos = scope.ServiceProvider.GetRequiredService<TalleresDbContext>();
        var pedidos = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
        var materials= scope.ServiceProvider.GetRequiredService<MaterialsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        await materials.Database.EnsureCreatedAsync();
        await user.Database.EnsureCreatedAsync();
        await partidos.Database.EnsureCreatedAsync();
        await pedidos.Database.EnsureCreatedAsync();
        
        logger.LogInformation("Base de datos verificada (tablas creadas si no existían)");
    }
}