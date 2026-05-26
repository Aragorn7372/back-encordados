using BackEncordados.Common.Database.Config;
using BackEncordados.Excel.Dto;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Excel.Repository;

public class ExcelTalleresRepository(TalleresDbContext talleresDbContext) : IExcelTalleresRepository
{
    public async Task<bool> IsUserSupervisorOfTournamentAsync(Ulid userId, Ulid tournamentId)
    {
        var tournament = await talleresDbContext.Partidos
            .Where(t => t.Id == tournamentId)
            .FirstOrDefaultAsync();

        if (tournament == null)
            return false;

        return tournament.SupervisorList.Contains(userId);
    }

    public async Task<bool> IsUserOwnerOfTournamentAsync(Ulid userId, Ulid tournamentId)
    {
        var tournament = await talleresDbContext.Partidos
            .Where(t => t.Id == tournamentId)
            .FirstOrDefaultAsync();

        if (tournament == null)
            return false;

        return tournament.Owner == userId;
    }

    public async Task<ExcelTournamentDto?> GetTournamentByIdAsync(Ulid tournamentId)
    {
        var tournament = await talleresDbContext.Partidos
            .Where(t => t.Id == tournamentId)
            .FirstOrDefaultAsync();

        if (tournament == null)
            return null;

        return new ExcelTournamentDto
        {
            Id = tournament.Id.ToString(),
            Owner = tournament.Owner.ToString(),
            Title = tournament.Title,
            StartTournament = tournament.StartTournament,
            EndTournament = tournament.EndTournament,
            Logotype = tournament.Logotype,
            WorkersList = string.Join(";", tournament.WorkersList),
            SupervisorList = string.Join(";", tournament.SupervisorList)
        };
    }
}
