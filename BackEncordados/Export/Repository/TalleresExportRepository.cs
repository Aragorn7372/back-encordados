using BackEncordados.Common.Database.Config;
using BackEncordados.Talleres.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Export.Repository;

public class TalleresExportRepository(
    TalleresDbContext talleresDbContext,
    ILogger<TalleresExportRepository> logger
) : ITalleresExportRepository
{
    public async Task<List<Tournaments>> GetTournamentsDataAsync()
    {
        var tournaments = await talleresDbContext.Partidos.IgnoreQueryFilters().ToListAsync();
        logger.LogInformation("Fetched {Count} tournaments", tournaments.Count);
        return tournaments;
    }

    public async Task ClearTournamentsAsync()
    {
        if (talleresDbContext.Database.IsInMemory())
        {
            var tournaments = await talleresDbContext.Partidos.IgnoreQueryFilters().ToListAsync();
            talleresDbContext.Partidos.RemoveRange(tournaments);
            await talleresDbContext.SaveChangesAsync();
            logger.LogInformation("Cleared tournaments (in-memory)");
        }
        else
        {
            var tournaments = await talleresDbContext.Partidos.IgnoreQueryFilters().ToListAsync();
            talleresDbContext.Partidos.RemoveRange(tournaments);
            await talleresDbContext.SaveChangesAsync();
            logger.LogInformation("Cleared tournaments (production)");
        }
    }

    public async Task ImportTournamentsAsync(List<Tournaments> tournaments)
    {
        if (!tournaments.Any()) return;

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
