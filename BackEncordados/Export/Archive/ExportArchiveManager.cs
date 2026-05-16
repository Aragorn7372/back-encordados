using System.IO.Compression;
using System.Text.Json;
using BackEncordados.Common.Dto;
using BackEncordados.Export.Dto;
using BackEncordados.Materials.Model;
using BackEncordados.Talleres.Model;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;

namespace BackEncordados.Export.Archive;

public class ExportArchiveManager(ILogger<ExportArchiveManager> logger) : IExportArchiveManager
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerSettings NewtonsoftSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    public async Task<byte[]> CreateZipAsync(ExportDataDto data)
    {
        logger.LogInformation("Creating ZIP archive");

        var tempDir = IOPath.Combine(IOPath.GetTempPath(), $"archive_{Guid.NewGuid()}");
        IODirectory.CreateDirectory(tempDir);

        try
        {
            var manifest = new ExportManifestDto
            {
                ExportedAt = DateTime.UtcNow,
                Entities = new List<ExportEntityInfo>()
            };

            var usersJson = JsonConvert.SerializeObject(data.Users, Formatting.Indented, NewtonsoftSettings);
            await IOFile.WriteAllTextAsync(IOPath.Combine(tempDir, "users.json"), usersJson);
            manifest.Entities.Add(new ExportEntityInfo { Name = "users", RecordCount = data.Users.Count, FileName = "users.json" });
            logger.LogInformation("Added {Count} users to archive", data.Users.Count);

            var tournamentsJson = JsonConvert.SerializeObject(data.Tournaments, Formatting.Indented, NewtonsoftSettings);
            await IOFile.WriteAllTextAsync(IOPath.Combine(tempDir, "tournaments.json"), tournamentsJson);
            manifest.Entities.Add(new ExportEntityInfo { Name = "tournaments", RecordCount = data.Tournaments.Count, FileName = "tournaments.json" });
            logger.LogInformation("Added {Count} tournaments to archive", data.Tournaments.Count);

            var materialsJson = JsonConvert.SerializeObject(data.Materials, Formatting.Indented, NewtonsoftSettings);
            await IOFile.WriteAllTextAsync(IOPath.Combine(tempDir, "materials.json"), materialsJson);
            manifest.Entities.Add(new ExportEntityInfo { Name = "materials", RecordCount = data.Materials.Count, FileName = "materials.json" });
            logger.LogInformation("Added {Count} materials to archive", data.Materials.Count);

            var cuerdasJson = JsonConvert.SerializeObject(data.Cuerdas, Formatting.Indented, NewtonsoftSettings);
            await IOFile.WriteAllTextAsync(IOPath.Combine(tempDir, "cuerdas.json"), cuerdasJson);
            manifest.Entities.Add(new ExportEntityInfo { Name = "cuerdas", RecordCount = data.Cuerdas.Count, FileName = "cuerdas.json" });
            logger.LogInformation("Added {Count} cuerdas to archive", data.Cuerdas.Count);

            var pedidosJson = JsonConvert.SerializeObject(data.Pedidos, Formatting.Indented, NewtonsoftSettings);
            await IOFile.WriteAllTextAsync(IOPath.Combine(tempDir, "orders.json"), pedidosJson);
            manifest.Entities.Add(new ExportEntityInfo { Name = "orders", RecordCount = data.Pedidos.Count, FileName = "orders.json" });
            logger.LogInformation("Added {Count} pedidos to archive", data.Pedidos.Count);

            var manifestJson = JsonSerializer.Serialize(manifest, _jsonOptions);
            await IOFile.WriteAllTextAsync(IOPath.Combine(tempDir, "manifest.json"), manifestJson);

            var zipPath = IOPath.Combine(IOPath.GetTempPath(), $"export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip");
            if (IOFile.Exists(zipPath))
                IOFile.Delete(zipPath);

            ZipFile.CreateFromDirectory(tempDir, zipPath);
            logger.LogInformation("ZIP created: {ZipPath}", zipPath);

            return await IOFile.ReadAllBytesAsync(zipPath);
        }
        finally
        {
            if (IODirectory.Exists(tempDir))
                IODirectory.Delete(tempDir, true);
        }
    }

    public async Task<ExportDataDto> ExtractZipAsync(Stream zipStream)
    {
        logger.LogInformation("Extracting ZIP archive");

        var tempDir = IOPath.Combine(IOPath.GetTempPath(), $"extract_{Guid.NewGuid()}");
        IODirectory.CreateDirectory(tempDir);

        try
        {
            var zipPath = IOPath.Combine(tempDir, "data.zip");
            await using (var fileStream = new FileStream(zipPath, FileMode.Create))
            {
                await zipStream.CopyToAsync(fileStream);
            }

            ZipFile.ExtractToDirectory(zipPath, tempDir, true);

            var data = new ExportDataDto();

            var tournamentsPath = IOPath.Combine(tempDir, "tournaments.json");
            if (IOFile.Exists(tournamentsPath))
            {
                var json = await IOFile.ReadAllTextAsync(tournamentsPath);
                data.Tournaments = JsonConvert.DeserializeObject<List<Tournaments>>(json) ?? new List<Tournaments>();
                logger.LogInformation("Extracted {Count} tournaments", data.Tournaments.Count);
            }

            var usersPath = IOPath.Combine(tempDir, "users.json");
            if (IOFile.Exists(usersPath))
            {
                var json = await IOFile.ReadAllTextAsync(usersPath);
                data.Users = JsonConvert.DeserializeObject<List<Usuarios.Model.User>>(json) ?? new List<Usuarios.Model.User>();
                logger.LogInformation("Extracted {Count} users", data.Users.Count);
            }

            var materialsPath = IOPath.Combine(tempDir, "materials.json");
            if (IOFile.Exists(materialsPath))
            {
                var json = await IOFile.ReadAllTextAsync(materialsPath);
                data.Materials = JsonConvert.DeserializeObject<List<Material>>(json) ?? new List<Material>();
                logger.LogInformation("Extracted {Count} materials", data.Materials.Count);
            }

            var cuerdasPath = IOPath.Combine(tempDir, "cuerdas.json");
            if (IOFile.Exists(cuerdasPath))
            {
                var json = await IOFile.ReadAllTextAsync(cuerdasPath);
                data.Cuerdas = JsonConvert.DeserializeObject<List<Materials.Model.Cuerdas>>(json) ?? new List<Materials.Model.Cuerdas>();
                logger.LogInformation("Extracted {Count} cuerdas", data.Cuerdas.Count);
            }

            var ordersPath = IOPath.Combine(tempDir, "orders.json");
            if (IOFile.Exists(ordersPath))
            {
                var json = await IOFile.ReadAllTextAsync(ordersPath);
                data.Pedidos = JsonConvert.DeserializeObject<List<Purchased.Model.Pedidos>>(json) ?? new List<Purchased.Model.Pedidos>();
                logger.LogInformation("Extracted {Count} pedidos", data.Pedidos.Count);
            }

            return data;
        }
        finally
        {
            if (IODirectory.Exists(tempDir))
                IODirectory.Delete(tempDir, true);
        }
    }

    public async Task<ExportManifestDto> GetManifestAsync(byte[] zipData)
    {
        var tempFile = IOPath.Combine(IOPath.GetTempPath(), $"manifest_{Guid.NewGuid()}.zip");
        await IOFile.WriteAllBytesAsync(tempFile, zipData);

        try
        {
            using var archive = ZipFile.OpenRead(tempFile);
            var manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry == null)
                throw new InvalidOperationException("Manifest not found in zip");

            await using var stream = manifestEntry.Open();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<ExportManifestDto>(json, _jsonOptions) 
                ?? throw new InvalidOperationException("Failed to deserialize manifest");
        }
        finally
        {
            IOFile.Delete(tempFile);
        }
    }
}