using BackEncordados.Excel.Dto;

namespace BackEncordados.Excel.Repository;

public interface IExcelMaterialsRepository
{
    Task<List<ExcelMaterialsDto>> GetMaterialsByTournamentAsync(Ulid tournamentId);
    Task<List<ExcelCuerdasDto>> GetCuerdasByTournamentAsync(Ulid tournamentId);
}
