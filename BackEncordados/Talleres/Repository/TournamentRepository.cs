using BackEncordados.Common.Database.Config;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Talleres.Repository;

public class TournamentRepository(TalleresDbContext context, ILogger<TournamentRepository> logger)
    : ITournamentRepository
{
    public Task<(IEnumerable<Tournaments> Items, int TotalCount)> FindAllAsync(FilterTournamentDto filter)
    {
        throw new NotImplementedException();
    }

    public async Task<Tournaments?> FindByIdAsync(long id)
    {
        logger.LogInformation("Buscando torneo con ID {Id}", id);
        return await context.Partidos.Include(x=>x.WorkerMachineAssignments).FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted == false);
    }

    public async Task<Tournaments?> FindByNameAsync(string name)
    {
        logger.LogInformation("Buscando torneo con Nombre {Nombre}", name);
        return await context.Partidos.Include(x=>x.WorkerMachineAssignments).FirstOrDefaultAsync(t => t.title == name && t.IsDeleted == false);
    }

    public async Task<Tournaments> SaveAsync(Tournaments tournament)
    {
        logger.LogInformation("Guardando torneo");
        var saved= await  context.Partidos.AddAsync(tournament);
        await context.SaveChangesAsync();
        return saved.Entity;
    }

    public async Task<Tournaments?> UpdateAsync(long id, Tournaments tournament)
    {
        logger.LogInformation("Actualizando torneo con ID {Id}", id);

        var existingTournament = await context.Partidos.FindAsync(id);

        if (existingTournament == null || existingTournament.IsDeleted)
            return null;

        // PATCH: solo campos simples (NO listas)

        if (!string.IsNullOrWhiteSpace(tournament.title))
            existingTournament.title = tournament.title;

        if (tournament.StartTournament != default)
            existingTournament.StartTournament = tournament.StartTournament;

        if (tournament.EndTournament != default)
            existingTournament.EndTournament = tournament.EndTournament;

        if (!string.IsNullOrWhiteSpace(tournament.logotype))
            existingTournament.logotype = tournament.logotype;

        // bool no nullable → se actualiza siempre o puedes decidir lógica
        existingTournament.IsDeleted = tournament.IsDeleted;
        
        var saved=context.Partidos.Update(existingTournament);
        await context.SaveChangesAsync();

        return saved.Entity;
    }

    public async Task<bool> DeleteAsync(long id)
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

    public async Task<Tournaments?> AsignWorker(long id, Guid workerId, string machineName)
    {
        logger.LogInformation(
            "Asignando trabajador con guid {workerId} al torneo con ID {Id}",
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
            existingTournament.WorkerMachineAssignments.Add(
                new WorkerMachineAssignment
                {
                    WorkerId = workerId,
                    MachineName = machineName
                });
        }
        var saved=context.Partidos.Update(existingTournament);
        await context.SaveChangesAsync();

        return saved.Entity;
    }

    public async Task<Tournaments?> RemoveWorker(long id, Guid workerId)
    {
        logger.LogInformation(
            "Eliminando trabajador con guid {workerId} del torneo con ID {Id}",
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

    public async Task<Tournaments?> PurchaseTournament(long id, Guid purchasedId)
    {
        logger.LogInformation(
            "Compran en torneo con ID {Id}, id de compra {purchasedId}",
            id, purchasedId);
        var existingTournament = await context.Partidos
            .FirstOrDefaultAsync(x => x.Id == id);
        if (existingTournament == null || existingTournament.IsDeleted ||
            existingTournament.EndTournament < DateTime.UtcNow)
            return null;
        existingTournament.PurchasedList.Add(purchasedId);
        var saved = context.Partidos.Update(existingTournament);
        await context.SaveChangesAsync();
        return saved.Entity;
    }
}