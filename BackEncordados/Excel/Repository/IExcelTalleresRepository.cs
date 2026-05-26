using BackEncordados.Excel.Dto;

namespace BackEncordados.Excel.Repository;

public interface IExcelTalleresRepository
{
    Task<bool> IsUserSupervisorOfTournamentAsync(Ulid userId, Ulid tournamentId);
    Task<bool> IsUserOwnerOfTournamentAsync(Ulid userId, Ulid tournamentId);
    Task<ExcelTournamentDto?> GetTournamentByIdAsync(Ulid tournamentId);
}
