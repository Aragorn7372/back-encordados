using BackEncordados.Common.Database.Config;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Talleres.Repository;

/// <summary>
/// Implementación de <see cref="ITournamentRepository"/> que accede a la entidad <see cref="Tournaments"/>
/// a través de <see cref="TalleresDbContext"/> con Entity Framework Core.
/// </summary>
/// <remarks>
/// <para>Proporciona operaciones CRUD completas sobre torneos, además de gestión de asignaciones
/// de trabajadores, máquinas y supervisores.</para>
/// <para><b>Comportamientos clave:</b></para>
/// <list type="bullet">
///   <item><description>La consulta <c>FindAllAsync</c> filtra por usuario (<c>UserId</c>) combinando torneos donde el usuario es owner, worker o supervisor.</description></item>
///   <item><description>La búsqueda textual por <c>Search</c> aplica sobre el título con <c>StringComparison.OrdinalIgnoreCase</c>.</description></item>
///   <item><description>Las asignaciones de trabajadores se almacenan en <c>WorkerMachineAssignments</c> con IDs secuenciales (maxId + 1).</description></item>
///   <item><description>Todas las consultas excluyen torneos eliminados lógicamente (<c>!x.IsDeleted</c>).</description></item>
/// </list>
/// <para><b>Casos borde:</b></para>
/// <list type="bullet">
///   <item><description>Si el <c>Search</c> es un ULID válido, también filtra por ID exacto además del título.</description></item>
///   <item><description>La ordenación se realiza en memoria (LINQ to Objects) porque el filtro por WorkersList/SupervisorList no es traducible a SQL.</description></item>
/// </list>
/// </remarks>
public class TournamentRepository(TalleresDbContext context, ILogger<TournamentRepository> logger)
    : ITournamentRepository
{
    /// <summary>
    /// Obtiene torneos paginados con filtros por usuario y búsqueda textual.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Consulta base excluyendo torneos eliminados.</description></item>
    ///   <item><description>Si <c>filter.UserId</c> está presente, busca torneos donde el usuario sea owner, worker o supervisor, combinando los resultados.</description></item>
    ///   <item><description>Si no hay filtro de usuario, retorna todos los torneos activos.</description></item>
    ///   <item><description>Aplica filtro de búsqueda textual por título (y ULID si el search es parseable).</description></item>
    ///   <item><description>Ordena por el campo especificado (title, start, end, createdAt, id) y aplica paginación.</description></item>
    /// </list>
    /// <para><b>Nota:</b> La ordenación y paginación se realizan en memoria porque el filtro <c>WorkersList.Contains</c> y <c>SupervisorList.Contains</c> fuerza la materialización.</para>
    /// </remarks>
    /// <param name="filter">DTO con filtros (Search, UserId, Page, Size, SortBy, Direction).</param>
    /// <returns>Tupla con lista de torneos y conteo total.</returns>
    public async Task<(IEnumerable<Tournaments> Items, int TotalCount)> FindAllAsync(FilterTournamentDto filter) {
        logger.LogInformation("Buscando torneos");
        var query = context.Partidos.AsNoTracking().AsQueryable();
        query = query.Where(x => !x.IsDeleted);
        
        IEnumerable<Tournaments> items;
        int totalCount;
        
        if (filter.UserId != null) {
            var ownerQuery = context.Partidos.Where(x => !x.IsDeleted && x.Owner == filter.UserId.Value);
            var workerQuery = context.Partidos.Where(x => !x.IsDeleted);
            var allItems = await workerQuery.ToListAsync();
            var filteredItems = allItems.Where(x => 
                x.WorkersList.Contains(filter.UserId.Value) || 
                x.SupervisorList.Contains(filter.UserId.Value)).ToList();
            var ownerItems = await ownerQuery.ToListAsync();
            var combined = filteredItems.Union(ownerItems).ToList();
            items = combined.AsEnumerable();
            totalCount = combined.Count;
        } else {
            items = await query.ToListAsync();
            totalCount = items.Count();
        }

        if (!string.IsNullOrWhiteSpace(filter.Search)) {
            if (Ulid.TryParse(filter.Search, out var ulid)) {
                items = items.Where(x => x.Id == ulid).ToList();
            }

            if (filter.Search.Length > 0) {
                items = items.Where(x => x.Title.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        totalCount = items.Count();
        bool isDesc = filter.Direction.ToLower().Equals("desc");
        items = filter.SortBy.ToLower() switch {
            "title" => isDesc ? items.OrderByDescending(x => x.Title) : items.OrderBy(x => x.Title),
            "start" => isDesc ? items.OrderByDescending(x => x.StartTournament) : items.OrderBy(x => x.StartTournament),
            "end" => isDesc ? items.OrderByDescending(x => x.EndTournament) : items.OrderBy(x => x.EndTournament),
            "createdat" => isDesc ? items.OrderByDescending(x => x.CreatedAt) : items.OrderBy(x => x.CreatedAt),
            _ => isDesc ? items.OrderByDescending(x => x.Id) : items.OrderBy(x => x.Id)
         };
        items = items.Skip(filter.Page * filter.Size).Take(filter.Size).ToList();
        return (items, totalCount);
    }

    /// <summary>
    /// Busca un torneo por ULID incluyendo asignaciones trabajador-máquina.
    /// </summary>
    /// <param name="id">ULID del torneo.</param>
    /// <returns>Torneo con asignaciones cargadas (<c>Include</c>) o <c>null</c> si no existe o está eliminado.</returns>
    public async Task<Tournaments?> FindByIdAsync(Ulid id)
    {
        logger.LogInformation("Buscando torneo con ID {Id}", id);
        return await context.Partidos.Include(x=>x.WorkerMachineAssignments).FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
    }

    /// <summary>
    /// Busca un torneo por nombre exacto incluyendo asignaciones trabajador-máquina.
    /// </summary>
    /// <param name="name">Nombre exacto del torneo.</param>
    /// <returns>Torneo con asignaciones cargadas o <c>null</c> si no existe.</returns>
    public async Task<Tournaments?> FindByNameAsync(string name)
    {
        logger.LogInformation("Buscando torneo con Nombre {Nombre}", name);
        return await context.Partidos.AsNoTracking().Include(x=>x.WorkerMachineAssignments).FirstOrDefaultAsync(t => t.Title == name && !t.IsDeleted);
    }

    /// <summary>
    /// Persiste un nuevo torneo en la base de datos.
    /// </summary>
    /// <param name="tournament">Entidad <see cref="Tournaments"/> a guardar.</param>
    /// <returns>Torneo guardado con su ULID generado por la BD.</returns>
    public async Task<Tournaments> SaveAsync(Tournaments tournament)
    {
        logger.LogInformation("Guardando torneo");
        var saved= await  context.Partidos.AddAsync(tournament);
        await context.SaveChangesAsync();
        return saved.Entity;
    }

    /// <summary>
    /// Actualiza parcialmente un torneo existente.
    /// </summary>
    /// <remarks>
    /// <para>Aplica cambios solo en los campos que no estén en su valor por defecto:</para>
    /// <list type="bullet">
    ///   <item><description><c>Title</c>: se actualiza si no es null/empty/whitespace.</description></item>
    ///   <item><description><c>StartTournament</c>: se actualiza si es distinto de <c>default(DateTime)</c>.</description></item>
    ///   <item><description><c>EndTournament</c>: se actualiza si es distinto de <c>default(DateTime)</c>.</description></item>
    ///   <item><description><c>Logotype</c>: se actualiza si no es null/empty/whitespace.</description></item>
    ///   <item><description><c>IsDeleted</c>: se actualiza siempre (bool no nullable).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">ULID del torneo a actualizar.</param>
    /// <param name="tournament">Entidad con campos a actualizar.</param>
    /// <returns>Torneo actualizado o <c>null</c> si el torneo no existe o está eliminado.</returns>
    public async Task<Tournaments?> UpdateAsync(Ulid id, Tournaments tournament)
    {
        logger.LogInformation("Actualizando torneo con ID {Id}", id);

        var existingTournament = await context.Partidos.FindAsync(id);

        if (existingTournament == null || existingTournament.IsDeleted)
            return null;


        if (!string.IsNullOrWhiteSpace(tournament.Title))
            existingTournament.Title = tournament.Title;

        if (tournament.StartTournament != default)
            existingTournament.StartTournament = tournament.StartTournament;

        if (tournament.EndTournament != default)
            existingTournament.EndTournament = tournament.EndTournament;

        if (!string.IsNullOrWhiteSpace(tournament.Logotype))
            existingTournament.Logotype = tournament.Logotype;

        // bool no nullable → se actualiza siempre o puedes decidir lógica
        existingTournament.IsDeleted = tournament.IsDeleted;
        
        await context.SaveChangesAsync();

        return existingTournament;
    }

    /// <summary>
    /// Elimina un torneo mediante soft delete.
    /// </summary>
    /// <param name="id">ULID del torneo a eliminar.</param>
    /// <returns><c>true</c> si se eliminó correctamente, <c>false</c> si no existe o ya estaba eliminado.</returns>
    public async Task<bool> DeleteAsync(Ulid id)
    {
        logger.LogInformation("Deletando torneo  con ID {Id}", id);
        var existingTournament =await FindByIdAsync(id);
        if (existingTournament == null || existingTournament.IsDeleted)
            return false;
        existingTournament.IsDeleted = true;
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Asigna un trabajador a una máquina dentro del torneo.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el torneo por ID. Si no existe o está eliminado, retorna <c>null</c>.</description></item>
    ///   <item><description>Si el trabajador no está ya en <c>WorkersList</c>, lo agrega.</description></item>
    ///   <item><description>Si el trabajador no tiene ya una asignación de máquina, crea una nueva con <c>Id = maxId + 1</c>.</description></item>
    ///   <item><description>Persiste los cambios.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="workerId">ULID del trabajador a asignar.</param>
    /// <param name="machineName">Nombre de la máquina asignada.</param>
    /// <returns>Torneo actualizado o <c>null</c> si el torneo no existe.</returns>
    public async Task<Tournaments?> AsignWorker(Ulid id, Ulid workerId, string machineName)
    {
        logger.LogInformation(
            "Asignando trabajador con guid {WorkerId} al torneo con ID {Id}",
            workerId, id);

        var existingTournament = await context.Partidos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournament == null || existingTournament.IsDeleted)
            return null;

        if (!existingTournament.WorkersList.Contains(workerId))
            existingTournament.WorkersList.Add(workerId);

        var alreadyAssigned = existingTournament.WorkerMachineAssignments
            .Any(x => x.WorkerId == workerId);
        if (!alreadyAssigned)
        {
            var maxId = existingTournament.WorkerMachineAssignments.Any()
                ? existingTournament.WorkerMachineAssignments.Max(x => x.Id)
                : 0;
            existingTournament.WorkerMachineAssignments.Add(
                new WorkerMachineAssignment
                {
                    Id = maxId + 1,
                    WorkerId = workerId,
                    MachineName = machineName
                });
        }
        await context.SaveChangesAsync();

        return existingTournament;
    }

    /// <summary>
    /// Desasigna un trabajador del torneo y elimina todas sus asignaciones de máquina.
    /// </summary>
    /// <remarks>
    /// <para>Elimina el trabajador de <c>WorkersList</c> y todas las entradas de <c>WorkerMachineAssignments</c>
    /// que correspondan a ese <c>WorkerId</c> mediante <c>RemoveAll</c>.</para>
    /// </remarks>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="workerId">ULID del trabajador a desasignar.</param>
    /// <returns>Torneo actualizado o <c>null</c> si el torneo no existe.</returns>
    public async Task<Tournaments?> RemoveWorker(Ulid id, Ulid workerId)
    {
        logger.LogInformation(
            "Eliminando trabajador con guid {WorkerId} del torneo con ID {Id}",
            workerId, id);

        var existingTournament = await context.Partidos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournament == null || existingTournament.IsDeleted)
            return null;

        existingTournament.WorkersList.Remove(workerId);

        existingTournament.WorkerMachineAssignments
            .RemoveAll(x => x.WorkerId == workerId);
        await context.SaveChangesAsync();

        return existingTournament;
    }

    /// <summary>
    /// Obtiene todas las asignaciones trabajador-máquina de un torneo.
    /// </summary>
    /// <param name="tournamentId">ULID del torneo.</param>
    /// <returns>Lista de asignaciones o <c>null</c> si el torneo no existe.</returns>
    public async Task<IEnumerable<WorkerMachineAssignment>?> GetAssignedWorkerMachinesAsync(Ulid tournamentId)
    {
        logger.LogInformation("Obteniendo máquinas asignadas para el torneo con ID {Id}", tournamentId);
        return await context.Partidos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tournamentId && !x.IsDeleted) is { } tournament ? tournament.WorkerMachineAssignments : null;
    }

    /// <summary>
    /// Asigna un supervisor al torneo agregándolo a <c>SupervisorList</c>.
    /// </summary>
    /// <remarks>
    /// <para>Si el supervisor ya está en la lista, no se realizan cambios (no duplicados).</para>
    /// </remarks>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="supervisorId">ULID del supervisor a asignar.</param>
    /// <returns>Torneo actualizado o <c>null</c> si el torneo no existe.</returns>
    public async Task<Tournaments?> AsignSupervisor(Ulid id, Ulid supervisorId) {
        logger.LogInformation(
            "Asignando supervisor con ulid {WorkerId} al torneo con ID {Id}",
            supervisorId, id);

        var existingTournament = await context.Partidos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournament == null || existingTournament.IsDeleted)
            return null;

        if (!existingTournament.SupervisorList.Contains(supervisorId))
            existingTournament.SupervisorList.Add(supervisorId);
        
        await context.SaveChangesAsync();

        return existingTournament;
    }

    /// <summary>
    /// Desasigna un supervisor del torneo eliminándolo de <c>SupervisorList</c>.
    /// </summary>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="supervisorId">ULID del supervisor a desasignar.</param>
    /// <returns>Torneo actualizado o <c>null</c> si el torneo no existe.</returns>
    public async Task<Tournaments?> RemoveSupervisor(Ulid id, Ulid supervisorId) {
        logger.LogInformation(
            "Eliminando supervisor con ulid {SupervisorId} del torneo con ID {Id}",
            supervisorId, id);

        var existingTournament = await context.Partidos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournament == null || existingTournament.IsDeleted)
            return null;

        if (existingTournament.SupervisorList.Contains(supervisorId))
            existingTournament.SupervisorList.Remove(supervisorId);
        
        await context.SaveChangesAsync();

        return existingTournament;
    }
}