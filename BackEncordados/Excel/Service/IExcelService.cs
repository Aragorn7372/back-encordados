using BackEncordados.Excel.Dto;

namespace BackEncordados.Excel.Service;

public interface IExcelService
{
    Task<byte[]> ExportTournamentAsync(Ulid userId, Ulid tournamentId);
    Task<byte[]> ExportAdvancedAsync(Ulid userId, Ulid tournamentId, List<string> types, string role);
    Task<ExcelImportResultDto> ImportAsync(Ulid userId, Ulid tournamentId, List<string> types, string role, Stream excelStream);
}