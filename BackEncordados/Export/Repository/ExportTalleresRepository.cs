using BackEncordados.Common.Database.Config;
using BackEncordados.Talleres.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Export.Repository;

public class ExportTalleresRepository(
    TalleresDbContext talleresDbContext,
    ILogger<ExportTalleresRepository> logger
) : IExportTalleresRepository
{
    public async Task<List<Tournaments>> GetAllTournamentsAsync()
    {
        return await talleresDbContext.Partidos.IgnoreQueryFilters().ToListAsync();
    }

    public async Task ClearTournamentsAsync()
    {
        var tournaments = await talleresDbContext.Partidos.IgnoreQueryFilters().ToListAsync();
        talleresDbContext.Partidos.RemoveRange(tournaments);
        await talleresDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared tournaments");
    }

    public async Task ImportTournamentsAsync(List<Tournaments> tournaments)
    {
        var tournamentAssignments = new List<(Ulid TournamentId, List<WorkerMachineAssignment> Assignments)>();

        foreach (var tournament in tournaments)
        {
            tournamentAssignments.Add((tournament.Id, tournament.WorkerMachineAssignments.ToList()));
            tournament.WorkerMachineAssignments = new List<WorkerMachineAssignment>();
            await talleresDbContext.Partidos.AddAsync(tournament);
        }
        await talleresDbContext.SaveChangesAsync();
        logger.LogInformation("Imported {Count} tournaments", tournaments.Count);

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
}
