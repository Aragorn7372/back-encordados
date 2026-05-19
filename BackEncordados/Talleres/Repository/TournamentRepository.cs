using BackEncordados.Common.Database.Config;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Talleres.Repository;

public class TournamentRepository(TalleresDbContext context, ILogger<TournamentRepository> logger)
    : ITournamentRepository
{
    public async Task<(IEnumerable<Tournaments> Items, int TotalCount)> FindAllAsync(FilterTournamentDto filter) {
        logger.LogInformation("Buscando torneos");
        var query = context.Partidos.AsQueryable();
        query = query.Where(x => !x.IsDeleted );
        if(filter.UserId != null) {
            query = query.Where(x => x.WorkersList.Contains(filter.UserId.Value)
            || x.Owner==filter.UserId.Value
            || x.SupervisorList.Contains(filter.UserId.Value));
        }
        if (!string.IsNullOrWhiteSpace(filter.Search)) {
            if (Ulid.TryParse(filter.Search, out var ulid)) {
                query = query.Where(x => x.Id == ulid);
            }

            if (filter.Search.Length > 0) {
                query = query.Where(x => EF.Functions.Like(x.Title, $"%{filter.Search}%"));
            }
        }

        var totalCount = await query.CountAsync();
        bool isDesc = filter.Direction.ToLower().Equals("desc");
        query = filter.SortBy.ToLower() switch {
            "title" => isDesc ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
            "start" => isDesc ? query.OrderByDescending(x => x.StartTournament) : query.OrderBy(x => x.StartTournament),
            "end" => isDesc ? query.OrderByDescending(x => x.EndTournament) : query.OrderBy(x => x.EndTournament),
            "createdat" => isDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _ => isDesc ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
         };
        var items = await query.Skip(filter.Page * filter.Size).Take(filter.Size).ToListAsync();
        return (items, totalCount);
    }

    public async Task<Tournaments?> FindByIdAsync(Ulid id)
    {
        logger.LogInformation("Buscando torneo con ID {Id}", id);
        return await context.Partidos.Include(x=>x.WorkerMachineAssignments).FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
    }

    public async Task<Tournaments?> FindByNameAsync(string name)
    {
        logger.LogInformation("Buscando torneo con Nombre {Nombre}", name);
        return await context.Partidos.Include(x=>x.WorkerMachineAssignments).FirstOrDefaultAsync(t => t.Title == name && !t.IsDeleted);
    }

    public async Task<Tournaments> SaveAsync(Tournaments tournament)
    {
        logger.LogInformation("Guardando torneo");
        var saved= await  context.Partidos.AddAsync(tournament);
        await context.SaveChangesAsync();
        return saved.Entity;
    }

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
        
        var saved=context.Partidos.Update(existingTournament);
        await context.SaveChangesAsync();

        return saved.Entity;
    }

    public async Task<bool> DeleteAsync(Ulid id)
    {
        logger.LogInformation("Deletando torneo  con ID {Id}", id);
        var existingTournament =await FindByIdAsync(id);
        if (existingTournament == null || existingTournament.IsDeleted)
            return false;
        existingTournament.IsDeleted = true;
        context.Partidos.Update(existingTournament);
        await context.SaveChangesAsync();
        return true;
    }

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
        var saved=context.Partidos.Update(existingTournament);
        await context.SaveChangesAsync();

        return saved.Entity;
    }

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
        var saved =context.Partidos.Update(existingTournament);
        await context.SaveChangesAsync();

        return saved.Entity;
    }

    public async Task<IEnumerable<WorkerMachineAssignment>?> GetAssignedWorkerMachinesAsync(Ulid tournamentId)
    {
        logger.LogInformation("Obteniendo máquinas asignadas para el torneo con ID {Id}", tournamentId);
        return await context.Partidos
            .FirstOrDefaultAsync(x => x.Id == tournamentId && !x.IsDeleted) is { } tournament ? tournament.WorkerMachineAssignments : null;
    }

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
        
        var saved=context.Partidos.Update(existingTournament);
        await context.SaveChangesAsync();

        return saved.Entity;
    }

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
        
        var saved =context.Partidos.Update(existingTournament);
        await context.SaveChangesAsync();

        return saved.Entity;
    }
}