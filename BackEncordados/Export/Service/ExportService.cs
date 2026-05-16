using BackEncordados.Common.Dto;
using BackEncordados.Export.Archive;
using BackEncordados.Export.Repository;

namespace BackEncordados.Export.Service;

public class ExportService(
    IExportRepository repository,
    IExportArchiveManager archiveManager,
    ILogger<ExportService> logger
) : IExportService
{
    public async Task<byte[]> ExportDatabaseAsync()
    {
        logger.LogInformation("Starting database export");

        var data = await repository.GetAllDataAsync();
        var zipData = await archiveManager.CreateZipAsync(data);

        logger.LogInformation("Export completed successfully");
        return zipData;
    }

    public async Task<ExportManifestDto> GetManifestAsync(byte[] zipData)
    {
        return await archiveManager.GetManifestAsync(zipData);
    }

    public async Task ImportDatabaseAsync(Stream zipStream)
    {
        logger.LogInformation("Starting database import");

        var data = await archiveManager.ExtractZipAsync(zipStream);
        await repository.ClearAllDataAsync();
        await repository.ImportDataAsync(data);

        logger.LogInformation("Import completed successfully");
    }
}