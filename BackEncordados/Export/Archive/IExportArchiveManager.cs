using BackEncordados.Common.Dto;
using BackEncordados.Export.Dto;

namespace BackEncordados.Export.Archive;

public interface IExportArchiveManager
{
    Task<byte[]> CreateZipAsync(ExportDataDto data);
    Task<ExportDataDto> ExtractZipAsync(Stream zipStream);
    Task<ExportManifestDto> GetManifestAsync(byte[] zipData);
}