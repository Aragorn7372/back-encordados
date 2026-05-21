using System.IO.Compression;
using System.Text.Json;
using BackEncordados.Common.Dto;
using BackEncordados.Export.Archive;
using BackEncordados.Export.Dto;
using BackEncordados.Materials.Model;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace TestEncordados.Integration.Export.Archive;

public class ExportArchiveManagerTests
{
    private readonly ExportArchiveManager _manager = new(NullLogger<ExportArchiveManager>.Instance);

    private static readonly JsonSerializerSettings NewtonsoftSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    private static readonly JsonSerializerOptions SystemTextJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static byte[] CreateTestZip(Dictionary<string, string> entries)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }
        return ms.ToArray();
    }

    private static ExportDataDto CreateSampleData()
    {
        return new ExportDataDto
        {
            Users = [new User()],
            Tournaments = [new Tournaments()],
            Materials = [new Material()],
            Cuerdas = [new Cuerdas()],
            Pedidos = [new Pedidos()]
        };
    }

    private static string SerializeJson<T>(T obj) =>
        JsonConvert.SerializeObject(obj, Formatting.Indented, NewtonsoftSettings);

    #region CreateZipAsync

    [Test]
    public async Task CreateZipAsync_WithAllData_ReturnsZipWithAllEntries()
    {
        var data = CreateSampleData();

        var zipBytes = await _manager.CreateZipAsync(data);

        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        var entryNames = archive.Entries.Select(e => e.Name).ToHashSet();
        entryNames.Should().Contain(["users.json", "tournaments.json", "materials.json", "cuerdas.json", "orders.json", "manifest.json"]);
        entryNames.Should().HaveCount(6);
    }

    [Test]
    public async Task CreateZipAsync_WithAllData_ManifestHasCorrectCounts()
    {
        var data = CreateSampleData();

        var zipBytes = await _manager.CreateZipAsync(data);

        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        var manifestEntry = archive.GetEntry("manifest.json")!;
        using var reader = new StreamReader(manifestEntry.Open());
        var manifestJson = await reader.ReadToEndAsync();
        var manifest = System.Text.Json.JsonSerializer.Deserialize<ExportManifestDto>(manifestJson, SystemTextJsonOptions);

        manifest.Should().NotBeNull();
        manifest!.Entities.Should().HaveCount(5);
        manifest.Entities.Should().AllSatisfy(e => e.RecordCount.Should().Be(1));
    }

    [Test]
    public async Task CreateZipAsync_WithEmptyData_ReturnsValidZip()
    {
        var data = new ExportDataDto();

        var zipBytes = await _manager.CreateZipAsync(data);

        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        archive.Entries.Should().HaveCount(6);

        var manifestEntry = archive.GetEntry("manifest.json")!;
        using var reader = new StreamReader(manifestEntry.Open());
        var manifestJson = await reader.ReadToEndAsync();
        var manifest = System.Text.Json.JsonSerializer.Deserialize<ExportManifestDto>(manifestJson, SystemTextJsonOptions);
        manifest.Should().NotBeNull();
        manifest!.Entities.Should().HaveCount(5);
        manifest.Entities.Should().AllSatisfy(e => e.RecordCount.Should().Be(0));
    }

    #endregion

    #region ExtractZipAsync

    [Test]
    public async Task ExtractZipAsync_WithFullZip_ReturnsDeserializedData()
    {
        var data = CreateSampleData();
        var entries = new Dictionary<string, string>
        {
            ["users.json"] = SerializeJson(data.Users),
            ["tournaments.json"] = SerializeJson(data.Tournaments),
            ["materials.json"] = SerializeJson(data.Materials),
            ["cuerdas.json"] = SerializeJson(data.Cuerdas),
            ["orders.json"] = SerializeJson(data.Pedidos)
        };
        var zipBytes = CreateTestZip(entries);

        var result = await _manager.ExtractZipAsync(new MemoryStream(zipBytes));

        result.Users.Should().HaveCount(1);
        result.Tournaments.Should().HaveCount(1);
        result.Materials.Should().HaveCount(1);
        result.Cuerdas.Should().HaveCount(1);
        result.Pedidos.Should().HaveCount(1);
    }

    [Test]
    public async Task ExtractZipAsync_WithPartialZip_SkipsMissingFiles()
    {
        var entries = new Dictionary<string, string>
        {
            ["users.json"] = SerializeJson(new List<User> { new() })
        };
        var zipBytes = CreateTestZip(entries);

        var result = await _manager.ExtractZipAsync(new MemoryStream(zipBytes));

        result.Users.Should().HaveCount(1);
        result.Tournaments.Should().BeEmpty();
        result.Materials.Should().BeEmpty();
        result.Cuerdas.Should().BeEmpty();
        result.Pedidos.Should().BeEmpty();
    }

    [Test]
    public async Task ExtractZipAsync_WithInvalidBytes_Throws()
    {
        var invalidBytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        var act = () => _manager.ExtractZipAsync(new MemoryStream(invalidBytes));

        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetManifestAsync

    [Test]
    public async Task GetManifestAsync_WithValidManifest_ReturnsManifest()
    {
        var manifest = new ExportManifestDto
        {
            Version = "2.0",
            Description = "Test export",
            Entities = [new ExportEntityInfo { Name = "users", RecordCount = 10, FileName = "users.json" }]
        };
        var manifestJson = System.Text.Json.JsonSerializer.Serialize(manifest, SystemTextJsonOptions);
        var zipBytes = CreateTestZip(new Dictionary<string, string>
        {
            ["manifest.json"] = manifestJson
        });

        var result = await _manager.GetManifestAsync(zipBytes);

        result.Version.Should().Be("2.0");
        result.Description.Should().Be("Test export");
        result.Entities.Should().HaveCount(1);
        result.Entities[0].Name.Should().Be("users");
    }

    [Test]
    public async Task GetManifestAsync_WhenManifestMissing_Throws()
    {
        var zipBytes = CreateTestZip(new Dictionary<string, string>
        {
            ["other.json"] = "{}"
        });

        var act = () => _manager.GetManifestAsync(zipBytes);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Manifest not found in zip");
    }

    [Test]
    public async Task GetManifestAsync_WhenManifestInvalid_Throws()
    {
        var zipBytes = CreateTestZip(new Dictionary<string, string>
        {
            ["manifest.json"] = "{invalid}"
        });

        var act = () => _manager.GetManifestAsync(zipBytes);

        await act.Should().ThrowAsync<System.Text.Json.JsonException>();
    }

    [Test]
    public async Task GetManifestAsync_WhenManifestJsonIsNull_Throws()
    {
        var zipBytes = CreateTestZip(new Dictionary<string, string>
        {
            ["manifest.json"] = "null"
        });

        var act = () => _manager.GetManifestAsync(zipBytes);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to deserialize manifest");
    }

    #endregion

    #region Roundtrip

    [Test]
    public async Task Roundtrip_CreateThenExtract_ProducesSameData()
    {
        var original = new ExportDataDto
        {
            Users = [new User { Username = "testuser", Email = "test@test.com", Name = "Test", Role = "USER" }],
            Tournaments = [new Tournaments { Title = "Test Tournament" }],
            Materials = [new Material { Marca = "Babolat" }],
            Cuerdas = [new Cuerdas { Marca = "Luxilon" }],
            Pedidos = [new Pedidos { Machine = "Alpha" }]
        };

        var zipBytes = await _manager.CreateZipAsync(original);
        var result = await _manager.ExtractZipAsync(new MemoryStream(zipBytes));

        result.Users.Should().HaveCount(1);
        result.Users[0].Username.Should().Be("testuser");
        result.Tournaments.Should().HaveCount(1);
        result.Tournaments[0].Title.Should().Be("Test Tournament");
        result.Materials.Should().HaveCount(1);
        result.Materials[0].Marca.Should().Be("Babolat");
        result.Cuerdas.Should().HaveCount(1);
        result.Cuerdas[0].Marca.Should().Be("Luxilon");
        result.Pedidos.Should().HaveCount(1);
        result.Pedidos[0].Machine.Should().Be("Alpha");
    }

    #endregion
}
