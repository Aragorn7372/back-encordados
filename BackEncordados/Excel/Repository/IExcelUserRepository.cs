using BackEncordados.Excel.Dto;

namespace BackEncordados.Excel.Repository;

public interface IExcelUserRepository
{
    Task<Dictionary<Ulid, (string Username, string Name)>> GetUsersDictByIdsAsync(List<Ulid> userIds);
    Task<List<ExcelUsersDto>> GetUsersByTournamentAsync(Ulid tournamentId);
}
