using BackEncordados.Talleres.Model;

namespace BackEncordados.Excel.Repository;

public interface ITalleresExcelRepository
{
    Task<Tournaments?> GetTournamentByIdAsync(Ulid tournamentId);
    Task<bool> IsUserSupervisorOfTournamentAsync(Ulid userId, Ulid tournamentId);
    Task<bool> IsUserOwnerOfTournamentAsync(Ulid userId, Ulid tournamentId);
}
