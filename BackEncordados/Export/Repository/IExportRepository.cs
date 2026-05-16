using BackEncordados.Export.Dto;

namespace BackEncordados.Export.Repository;

public interface IExportRepository
{
    Task<ExportDataDto> GetAllDataAsync();
    Task ClearAllDataAsync();
    Task ImportDataAsync(ExportDataDto data);
}