using BackEncordados.Excel.Dto;

namespace BackEncordados.Excel.Repository;

public interface IExcelRepository
{
    Task<IEnumerable<TournamentExcelRowDto>> GetTournamentDataAsync(Ulid tournamentId);
    Task<bool> IsUserSupervisorOfTournamentAsync(Ulid userId, Ulid tournamentId);
    Task<bool> IsUserOwnerOfTournamentAsync(Ulid userId, Ulid tournamentId);
    Task<ExcelAdvancedDataDto> GetAdvancedDataAsync(Ulid tournamentId, List<string> types);
}