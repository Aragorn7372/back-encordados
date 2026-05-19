using BackEncordados.Excel.Dto;

namespace BackEncordados.Excel.Archive;

public interface IExcelArchiveManager
{
    Task<byte[]> CreateExcelAsync(IEnumerable<TournamentExcelRowDto> data, string tournamentName);
    Task<byte[]> CreateAdvancedExcelAsync(ExcelAdvancedDataDto data, List<string> types, string tournamentName);
    Task<ExcelAdvancedDataDto> ReadExcelAsync(Stream stream);
}