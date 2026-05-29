using BackEncordados.Common.Database.Config;
using BackEncordados.Talleres.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Excel.Repository;

public class TalleresExcelRepository(
    TalleresDbContext talleresDbContext,
    ILogger<TalleresExcelRepository> logger
) : ITalleresExcelRepository
{
    public async Task<Tournaments?> GetTournamentByIdAsync(Ulid tournamentId)
    {
        var tournament = await talleresDbContext.Partidos
            .Where(t => t.Id == tournamentId)
            .FirstOrDefaultAsync();

        if (tournament == null)
            logger.LogWarning("Tournament {TournamentId} not found", tournamentId);
        else
            logger.LogInformation("Fetched tournament {TournamentId}", tournamentId);

        return tournament;
    }

    public async Task<bool> IsUserSupervisorOfTournamentAsync(Ulid userId, Ulid tournamentId)
    {
        var tournament = await GetTournamentByIdAsync(tournamentId);

        if (tournament == null)
            return false;

        return tournament.SupervisorList.Contains(userId);
    }

    public async Task<bool> IsUserOwnerOfTournamentAsync(Ulid userId, Ulid tournamentId)
    {
        var tournament = await GetTournamentByIdAsync(tournamentId);

        if (tournament == null)
            return false;

        return tournament.Owner == userId;
    }
}
