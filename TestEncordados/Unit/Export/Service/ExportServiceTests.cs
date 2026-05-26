using BackEncordados.Common.Dto;
using BackEncordados.Export.Archive;
using BackEncordados.Export.Dto;
using BackEncordados.Export.Repository;
using BackEncordados.Export.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TestEncordados.Unit.Export.Service;

public class ExportServiceTests
{
    private Mock<IExportRepository> _mockRepo = null!;
    private Mock<IExportArchiveManager> _mockArchive = null!;
    private ExportService _service = null!;

    private static readonly ExportDataDto SampleData = new();
    private static readonly byte[] SampleZip = [0x50, 0x4B, 0x03, 0x04];
    private static readonly ExportManifestDto SampleManifest = new()
    {
        Version = "1.0",
        Description = "Test manifest",
        Entities = [new ExportEntityInfo { Name = "Users", RecordCount = 5, FileName = "users.json" }]
    };
    private static readonly Stream SampleStream = new MemoryStream();

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IExportRepository>();
        _mockArchive = new Mock<IExportArchiveManager>();
        _service = new ExportService(
            _mockRepo.Object,
            _mockArchive.Object,
            NullLogger<ExportService>.Instance
        );
    }

    #region ExportDatabaseAsync

    [Test]
    public async Task ExportDatabaseAsync_ReturnsZipBytes()
    {
        _mockRepo.Setup(r => r.GetAllDataAsync()).ReturnsAsync(SampleData);
        _mockArchive.Setup(a => a.CreateZipAsync(SampleData)).ReturnsAsync(SampleZip);

        var result = await _service.ExportDatabaseAsync();

        result.Should().BeSameAs(SampleZip);
    }

    [Test]
    public async Task ExportDatabaseAsync_WhenRepositoryThrows_PropagatesException()
    {
        var expected = new InvalidOperationException("DB error");
        _mockRepo.Setup(r => r.GetAllDataAsync()).ThrowsAsync(expected);

        var act = () => _service.ExportDatabaseAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB error");
    }

    #endregion

    #region GetManifestAsync

    [Test]
    public async Task GetManifestAsync_ReturnsManifestFromArchive()
    {
        _mockArchive.Setup(a => a.GetManifestAsync(SampleZip)).ReturnsAsync(SampleManifest);

        var result = await _service.GetManifestAsync(SampleZip);

        result.Should().BeSameAs(SampleManifest);
    }

    [Test]
    public async Task GetManifestAsync_WhenArchiveThrows_PropagatesException()
    {
        var expected = new InvalidOperationException("Archive error");
        _mockArchive.Setup(a => a.GetManifestAsync(SampleZip)).ThrowsAsync(expected);

        var act = () => _service.GetManifestAsync(SampleZip);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Archive error");
    }

    #endregion

    #region ImportDatabaseAsync

    [Test]
    public async Task ImportDatabaseAsync_ExecutesInOrder()
    {
        _mockArchive.Setup(a => a.ExtractZipAsync(SampleStream)).ReturnsAsync(SampleData);

        var importSequence = new MockSequence();
        _mockRepo.InSequence(importSequence).Setup(r => r.ClearAllDataAsync()).Returns(Task.CompletedTask);
        _mockRepo.InSequence(importSequence).Setup(r => r.ImportDataAsync(SampleData)).Returns(Task.CompletedTask);

        await _service.ImportDatabaseAsync(SampleStream);

        _mockArchive.Verify(a => a.ExtractZipAsync(SampleStream), Times.Once);
        _mockRepo.Verify(r => r.ClearAllDataAsync(), Times.Once);
        _mockRepo.Verify(r => r.ImportDataAsync(SampleData), Times.Once);
    }

    [Test]
    public async Task ImportDatabaseAsync_WhenExtractThrows_SkipsClearAndImport()
    {
        var expected = new InvalidOperationException("Extract error");
        _mockArchive.Setup(a => a.ExtractZipAsync(SampleStream)).ThrowsAsync(expected);

        var act = () => _service.ImportDatabaseAsync(SampleStream);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Extract error");
        _mockRepo.Verify(r => r.ClearAllDataAsync(), Times.Never);
        _mockRepo.Verify(r => r.ImportDataAsync(It.IsAny<ExportDataDto>()), Times.Never);
    }

    [Test]
    public async Task ImportDatabaseAsync_WhenClearThrows_SkipsImport()
    {
        _mockArchive.Setup(a => a.ExtractZipAsync(SampleStream)).ReturnsAsync(SampleData);
        var expected = new InvalidOperationException("Clear error");
        _mockRepo.Setup(r => r.ClearAllDataAsync()).ThrowsAsync(expected);

        var act = () => _service.ImportDatabaseAsync(SampleStream);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Clear error");
        _mockRepo.Verify(r => r.ImportDataAsync(It.IsAny<ExportDataDto>()), Times.Never);
    }

    #endregion
}
