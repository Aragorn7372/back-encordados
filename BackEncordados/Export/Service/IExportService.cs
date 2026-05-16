using BackEncordados.Common.Dto;

namespace BackEncordados.Export.Service;

public interface IExportService
{
    Task<byte[]> ExportDatabaseAsync();
    Task<ExportManifestDto> GetManifestAsync(byte[] zipData);
    Task ImportDatabaseAsync(Stream zipStream);
}