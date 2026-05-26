using BackEncordados.Common.Database.Config;
using BackEncordados.Export.Dto;
using BackEncordados.Materials.Model;
using Microsoft.EntityFrameworkCore;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Model;

namespace BackEncordados.Export.Repository;

/// <summary>
/// Implementación de <see cref="IExportRepository"/> que gestiona la obtención,
/// limpieza e importación global de todas las entidades de la base de datos.
/// </summary>
/// <remarks>
/// <para>Opera sobre los cuatro DbContexts del sistema, seleccionando estrategias
/// de borrado según el proveedor de base de datos y respetando el orden de
/// dependencias entre entidades tanto para limpieza como para importación.</para>
/// <para><b>Dependencias inyectadas:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Parámetro</term>
///     <term>DbContext</term>
///     <description>Entidades gestionadas</description>
///   </listheader>
///   <item>
///     <term><c>userDbContext</c></term>
///     <term><see cref="UserDbContext"/></term>
///     <description>Users</description>
///   </item>
///   <item>
///     <term><c>materialsDbContext</c></term>
///     <term><see cref="MaterialsDbContext"/></term>
///     <description>Materiales, Cuerdas</description>
///   </item>
///   <item>
///     <term><c>pedidosDbContext</c></term>
///     <term><see cref="PedidosDbContext"/></term>
///     <description>Pedidos, PedidoLineas</description>
///   </item>
///   <item>
///     <term><c>talleresDbContext</c></term>
///     <term><see cref="TalleresDbContext"/></term>
///     <description>Torneos (con WorkerMachineAssignments)</description>
///   </item>
/// </list>
/// </remarks>
/// <param name="userDbContext">DbContext de usuarios.</param>
/// <param name="materialsDbContext">DbContext de materiales y cuerdas.</param>
/// <param name="pedidosDbContext">DbContext de pedidos y líneas.</param>
/// <param name="talleresDbContext">DbContext de torneos.</param>
/// <param name="logger">Logger para seguimiento de operaciones.</param>
public class ExportRepository(
    UserDbContext userDbContext,
    MaterialsDbContext materialsDbContext,
    PedidosDbContext pedidosDbContext,
    TalleresDbContext talleresDbContext,
    ILogger<ExportRepository> logger
) : IExportRepository
{
    /// <summary>
    /// Obtiene todas las entidades de la base de datos para exportación.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Consulta <c>TalleresDbContext.Partidos</c> con <c>IgnoreQueryFilters()</c>
    ///   (incluye soft-deleted). Sin <c>AsNoTracking()</c> porque se necesita seguimiento
    ///   si se reimportan (aunque en exportación es solo lectura).</description></item>
    ///   <item><description>Consulta <c>UserDbContext.Users</c> con <c>AsNoTracking().IgnoreQueryFilters()</c>.</description></item>
    ///   <item><description>Consulta <c>MaterialsDbContext.Materiales</c> con <c>AsNoTracking().IgnoreQueryFilters()</c>.</description></item>
    ///   <item><description>Consulta <c>MaterialsDbContext.Cuerdas</c> con <c>AsNoTracking().IgnoreQueryFilters()</c>.</description></item>
    ///   <item><description>Consulta <c>PedidosDbContext.Pedidos</c> (sin AsNoTracking porque se modifican
    ///   las líneas).</description></item>
    ///   <item><description>Carga <c>PedidoLineas</c> por separado en un <c>ToLookup</c> indexado por
    ///   <c>PedidoId</c>, y asigna manualmente cada grupo a <c>pedido.Lineas</c>.</description></item>
    /// </list>
    /// <para><b>Nota técnica:</b> Se evita usar <c>.Include(p => p.Lineas)</c> porque las
    /// relaciones de navegación de EF Core crean referencias circulares que complican
    /// la serialización con Newtonsoft.Json incluso con <c>ReferenceLoopHandling.Ignore</c>.
    /// La carga manual con Lookup es más predecible.</para>
    /// </remarks>
    /// <returns>DTO con todas las listas de entidades pobladas.</returns>
    public async Task<ExportDataDto> GetAllDataAsync()
    {
        logger.LogInformation("Fetching all data from database");

        var data = new ExportDataDto();

        data.Tournaments = await talleresDbContext.Partidos.IgnoreQueryFilters().ToListAsync();
        logger.LogInformation("Fetched {Count} tournaments", data.Tournaments.Count);

        data.Users = await userDbContext.Users.AsNoTracking().IgnoreQueryFilters().ToListAsync();
        logger.LogInformation("Fetched {Count} users", data.Users.Count);

        data.Materials = await materialsDbContext.Materiales.AsNoTracking().IgnoreQueryFilters().ToListAsync();
        logger.LogInformation("Fetched {Count} materials", data.Materials.Count);

        data.Cuerdas = await materialsDbContext.Cuerdas.AsNoTracking().IgnoreQueryFilters().ToListAsync();
        logger.LogInformation("Fetched {Count} cuerdas", data.Cuerdas.Count);

        var pedidos = await pedidosDbContext.Pedidos.ToListAsync();
        var lineasLookup = (await pedidosDbContext.PedidoLineas.ToListAsync())
            .ToLookup(x => x.PedidoId);


        foreach (var pedido in pedidos)
        {
            pedido.Lineas = lineasLookup[pedido.Id].ToList();
        }
        data.Pedidos = pedidos;
        logger.LogInformation("Fetched {Count} pedidos", data.Pedidos.Count);

        return data;
    }

    /// <summary>
    /// Elimina todos los datos de la base de datos en orden inverso de dependencias.
    /// </summary>
    /// <remarks>
    /// <para>Selecciona automáticamente la estrategia según el proveedor:</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Condición</term>
    ///     <description>Estrategia</description>
    ///     <description>Método delegado</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>Database.IsInMemory() == true</c></term>
    ///     <description><c>RemoveRange</c> + <c>SaveChangesAsync</c> en lotes por DbContext</description>
    ///     <description><c>ClearAllDataInMemoryAsync</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>Database.IsInMemory() == false</c></term>
    ///     <description><c>ExecuteDeleteAsync</c> para tablas bulk (más rápido),
    ///     <c>RemoveRange</c> para tablas con dependencias de OwnsMany</description>
    ///     <description><c>ClearAllDataProductionAsync</c></description>
    ///   </item>
    /// </list>
    /// <para><b>Orden de eliminación (ambas estrategias):</b></para>
    /// <list type="number">
    ///   <item><description>PedidoLineas — primero porque dependen de Pedidos.</description></item>
    ///   <item><description>Pedidos — dependen de Users (PlayerId) y Tournaments (TournamentId).</description></item>
    ///   <item><description>Cuerdas — no tienen FK a otras entidades.</description></item>
    ///   <item><description>Materiales — no tienen FK a otras entidades.</description></item>
    ///   <item><description>Users — pueden tener TournamentId (FK a Tournaments).</description></item>
    ///   <item><description>Tournaments — última porque otras entidades pueden referenciarlos.</description></item>
    /// </list>
    /// </remarks>
    public async Task ClearAllDataAsync()
    {
        logger.LogInformation("Clearing all data in reverse order");

        if (userDbContext.Database.IsInMemory())
        {
            await ClearAllDataInMemoryAsync();
        }
        else
        {
            await ClearAllDataProductionAsync();
        }
    }

    /// <summary>
    /// Elimina todos los datos usando estrategia InMemory (RemoveRange + SaveChanges).
    /// </summary>
    /// <remarks>
    /// <para>Usa <c>RemoveRange</c> + <c>SaveChangesAsync</c> en lotes separados por DbContext:</para>
    /// <list type="number">
    ///   <item><description>PedidoLineas → <c>RemoveRange</c></description></item>
    ///   <item><description>Pedidos → <c>RemoveRange</c> + <c>SaveChangesAsync</c></description></item>
    ///   <item><description>Cuerdas → <c>RemoveRange</c></description></item>
    ///   <item><description>Materiales → <c>RemoveRange</c> + <c>SaveChangesAsync</c></description></item>
    ///   <item><description>Users (con <c>IgnoreQueryFilters</c>) → <c>RemoveRange</c> + <c>SaveChangesAsync</c></description></item>
    ///   <item><description>Tournaments (con <c>IgnoreQueryFilters</c>) → <c>RemoveRange</c> + <c>SaveChangesAsync</c></description></item>
    /// </list>
    /// <para><b>Nota:</b> La base de datos InMemory de EF Core no soporta <c>ExecuteDeleteAsync</c>,
    /// por lo que se usa <c>RemoveRange</c> que carga las entidades en memoria antes de eliminarlas.</para>
    /// </remarks>
    private async Task ClearAllDataInMemoryAsync()
    {
        logger.LogInformation("Using in-memory delete strategy");

        var pedidoLineas = await pedidosDbContext.PedidoLineas.ToListAsync();
        pedidosDbContext.PedidoLineas.RemoveRange(pedidoLineas);
        
        var pedidos = await pedidosDbContext.Pedidos.ToListAsync();
        pedidosDbContext.Pedidos.RemoveRange(pedidos);
        await pedidosDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared pedidos");

        var cuerdas = await materialsDbContext.Cuerdas.ToListAsync();
        materialsDbContext.Cuerdas.RemoveRange(cuerdas);

        var materials = await materialsDbContext.Materiales.ToListAsync();
        materialsDbContext.Materiales.RemoveRange(materials);
        await materialsDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared materials");

        var users = await userDbContext.Users.IgnoreQueryFilters().ToListAsync();
        userDbContext.Users.RemoveRange(users);
        await userDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared users");

        var tournaments = await talleresDbContext.Partidos.IgnoreQueryFilters().ToListAsync();
        talleresDbContext.Partidos.RemoveRange(tournaments);
        await talleresDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared tournaments");
    }

    /// <summary>
    /// Elimina todos los datos usando estrategia de producción.
    /// </summary>
    /// <remarks>
    /// <para>Combina <c>ExecuteDeleteAsync</c> (más rápido para tablas grandes)
    /// con <c>RemoveRange</c> + <c>SaveChangesAsync</c> donde sea necesario:</para>
    /// <list type="number">
    ///   <item><description>PedidoLineas → <c>RemoveRange</c> (necesario por dependencia con OwnsMany).</description></item>
    ///   <item><description>Pedidos → <c>RemoveRange</c> + <c>SaveChangesAsync</c>.</description></item>
    ///   <item><description>Cuerdas → <c>ExecuteDeleteAsync</c> (SQL directo, sin cargar en memoria).</description></item>
    ///   <item><description>Materiales → <c>ExecuteDeleteAsync</c> (SQL directo, sin cargar en memoria).</description></item>
    ///   <item><description>Users → <c>ExecuteDeleteAsync</c> (SQL directo, sin cargar en memoria).</description></item>
    ///   <item><description>Tournaments (con <c>IgnoreQueryFilters</c>) → <c>RemoveRange</c> + <c>SaveChangesAsync</c>
    ///   (necesario por OwnsMany WorkerMachineAssignments).</description></item>
    /// </list>
    /// <para><b>Ventaja de ExecuteDeleteAsync:</b> Genera SQL <c>DELETE FROM [...]</c> directamente
    /// sin cargar entidades en memoria, mucho más rápido para tablas grandes.</para>
    /// </remarks>
    private async Task ClearAllDataProductionAsync()
    {
        logger.LogInformation("Using production delete strategy (ExecuteDeleteAsync for PostgreSQL, RemoveRange for MongoDB)");

        var pedidoLineas = await pedidosDbContext.PedidoLineas.ToListAsync();
        pedidosDbContext.PedidoLineas.RemoveRange(pedidoLineas);

        var pedidos = await pedidosDbContext.Pedidos.ToListAsync();
        pedidosDbContext.Pedidos.RemoveRange(pedidos);

        await pedidosDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared pedidos");

        await materialsDbContext.Cuerdas.ExecuteDeleteAsync();
        await materialsDbContext.Materiales.ExecuteDeleteAsync();
        logger.LogInformation("Cleared materials");

        await userDbContext.Users.ExecuteDeleteAsync();
        logger.LogInformation("Cleared users");

        var tournaments = await talleresDbContext.Partidos.IgnoreQueryFilters().ToListAsync();
        talleresDbContext.Partidos.RemoveRange(tournaments);
        await talleresDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared tournaments");
    }

    /// <summary>
    /// Importa todas las entidades en la base de datos respetando el orden de dependencias.
    /// </summary>
    /// <remarks>
    /// <para><b>Orden de importación detallado:</b></para>
    /// <list type="number">
    ///   <item><description><b>Torneos</b> — Se insertan primero porque otras entidades pueden
    ///   referenciarlos (FK). Requiere un manejo especial para <c>WorkerMachineAssignments</c>:
    ///   <list type="number">
    ///     <item><description>Se extraen las asignaciones de cada torneo y se guardan en una lista temporal.</description></item>
    ///     <item><description>Se limpia <c>WorkerMachineAssignments</c> del torneo (se setea a lista vacía).</description></item>
    ///     <item><description>Se inserta el torneo con <c>AddAsync</c>.</description></item>
    ///     <item><description>Después de <c>SaveChangesAsync</c>, se reconsulta cada torneo y se agregan
    ///     las asignaciones con IDs numéricos secuenciales (<c>nextAssignmentId</c>).</description></item>
    ///     <item><description>Se guarda nuevamente con <c>SaveChangesAsync</c>.</description></item>
    ///   </list>
    ///   Este proceso es necesario porque <c>WorkerMachineAssignments</c> es una colección
    ///   OwnsMany que requiere IDs únicos enteros, y los IDs originales pueden colisionar
    ///   con registros existentes.</description></item>
    ///   <item><description><b>Usuarios</b> — Se insertan después de torneos. Pueden tener
    ///   <c>TournamentId</c> opcional referenciando un torneo existente.</description></item>
    ///   <item><description><b>Materiales</b> — Se insertan con <c>AddRangeAsync</c>.</description></item>
    ///   <item><description><b>Cuerdas</b> — Se insertan con <c>AddRangeAsync</c>.</description></item>
    ///   <item><description><b>Pedidos</b> — Últimos porque dependen de usuarios (PlayerId, AssignedTo)
    ///   y torneos (TournamentId). Se insertan con <c>AddRangeAsync</c> e incluyen las
    ///   líneas de pedido anidadas en la relación de navegación <c>Lineas</c>.</description></item>
    /// </list>
    /// <para>Cada paso verifica si hay datos (<c>.Any()</c>) antes de insertar, y ejecuta
    /// <c>SaveChangesAsync</c> después de cada módulo para garantizar que los IDs
    /// generados por la BD estén disponibles para el siguiente paso.</para>
    /// </remarks>
    /// <param name="data">DTO con todas las listas de entidades a importar.</param>
    public async Task ImportDataAsync(ExportDataDto data)
    {
        logger.LogInformation("Importing data in correct order");

        if (data.Tournaments.Any())
        {
            var tournamentAssignments = new List<(Ulid TournamentId, List<WorkerMachineAssignment> Assignments)>();

            foreach (var tournament in data.Tournaments)
            {
                tournamentAssignments.Add((tournament.Id, tournament.WorkerMachineAssignments.ToList()));
                tournament.WorkerMachineAssignments = new List<WorkerMachineAssignment>();
                await talleresDbContext.Partidos.AddAsync(tournament);
            }
            await talleresDbContext.SaveChangesAsync();
            logger.LogInformation("Imported {Count} tournaments", data.Tournaments.Count);

            long nextAssignmentId = 1;
            foreach (var (tournamentId, assignments) in tournamentAssignments)
            {
                var existingTournament = await talleresDbContext.Partidos.FirstOrDefaultAsync(t => t.Id == tournamentId);
                if (existingTournament != null)
                {
                    foreach (var assignment in assignments)
                    {
                        existingTournament.WorkerMachineAssignments.Add(new WorkerMachineAssignment
                        {
                            Id = nextAssignmentId++,
                            WorkerId = assignment.WorkerId,
                            MachineName = assignment.MachineName
                        });
                    }
                }
            }
            await talleresDbContext.SaveChangesAsync();
            logger.LogInformation("Imported worker machine assignments for {Count} tournaments", tournamentAssignments.Count);
        }

        if (data.Users.Any())
        {
            await userDbContext.Users.AddRangeAsync(data.Users);
            await userDbContext.SaveChangesAsync();
            logger.LogInformation("Imported {Count} users", data.Users.Count);
        }

        if (data.Materials.Any())
        {
            await materialsDbContext.Materiales.AddRangeAsync(data.Materials);
            await materialsDbContext.SaveChangesAsync();
            logger.LogInformation("Imported {Count} materials", data.Materials.Count);
        }

        if (data.Cuerdas.Any())
        {
            await materialsDbContext.Cuerdas.AddRangeAsync(data.Cuerdas);
            await materialsDbContext.SaveChangesAsync();
            logger.LogInformation("Imported {Count} cuerdas", data.Cuerdas.Count);
        }

        if (data.Pedidos.Any())
        {
            await pedidosDbContext.Pedidos.AddRangeAsync(data.Pedidos);
            await pedidosDbContext.SaveChangesAsync();
            logger.LogInformation("Imported {Count} pedidos", data.Pedidos.Count);
        }

        logger.LogInformation("Data import completed");
    }
}