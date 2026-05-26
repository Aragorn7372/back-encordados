using System.Security.Claims;
using BackEncordados.Common.Dto;
using BackEncordados.Export.Controller;
using BackEncordados.Export.Service;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TestEncordados.Unit.Export.Controller;

public class ExportControllerTests
{
    private readonly Mock<IExportService> _mockService = new();

    private static readonly byte[] SampleZip = [0x50, 0x4B, 0x03, 0x04];
    private static readonly ExportManifestDto SampleManifest = new()
    {
        Version = "1.0",
        Description = "Test manifest",
        Entities = [new ExportEntityInfo { Name = "Users", RecordCount = 5, FileName = "users.json" }]
    };

    private ExportController CreateController()
    {
        var controller = new ExportController(
            NullLogger<ExportController>.Instance,
            _mockService.Object
        );
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, "ADMIN")], "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    private static Mock<IFormFile> CreateMockFile(string fileName, byte[] content)
    {
        var mockFile = new Mock<IFormFile>();
        var stream = new MemoryStream(content);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(content.Length);
        return mockFile;
    }

    #region Export

    [Test]
    public async Task Export_ReturnsFileContentResult()
    {
        _mockService.Setup(s => s.ExportDatabaseAsync()).ReturnsAsync(SampleZip);
        var controller = CreateController();

        var result = await controller.Export();

        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/zip");
        fileResult.FileDownloadName.Should().Match("database_export_*.zip");
        fileResult.FileContents.Should().BeSameAs(SampleZip);
    }

    [Test]
    public async Task Export_WhenServiceThrows_Returns500()
    {
        _mockService.Setup(s => s.ExportDatabaseAsync()).ThrowsAsync(new Exception("DB error"));
        var controller = CreateController();

        var result = await controller.Export();

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Import

    [Test]
    public async Task Import_WithNullFile_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Import(null!);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task Import_WithEmptyFile_ReturnsBadRequest()
    {
        var mockFile = CreateMockFile("test.zip", []);
        var controller = CreateController();

        var result = await controller.Import(mockFile.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task Import_WithNonZipExtension_ReturnsBadRequest()
    {
        var mockFile = CreateMockFile("data.txt", SampleZip);
        var controller = CreateController();

        var result = await controller.Import(mockFile.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task Import_Success_ReturnsOk()
    {
        _mockService.Setup(s => s.ImportDatabaseAsync(It.IsAny<Stream>())).Returns(Task.CompletedTask);
        var mockFile = CreateMockFile("export.zip", SampleZip);
        var controller = CreateController();

        var result = await controller.Import(mockFile.Object);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task Import_WhenServiceThrows_Returns500()
    {
        _mockService.Setup(s => s.ImportDatabaseAsync(It.IsAny<Stream>())).ThrowsAsync(new Exception("Import error"));
        var mockFile = CreateMockFile("export.zip", SampleZip);
        var controller = CreateController();

        var result = await controller.Import(mockFile.Object);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetManifest

    [Test]
    public async Task GetManifest_WithNullFile_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.GetManifest(null!);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task GetManifest_WithEmptyFile_ReturnsBadRequest()
    {
        var mockFile = CreateMockFile("test.zip", []);
        var controller = CreateController();

        var result = await controller.GetManifest(mockFile.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task GetManifest_Success_ReturnsManifest()
    {
        _mockService.Setup(s => s.GetManifestAsync(It.IsAny<byte[]>())).ReturnsAsync(SampleManifest);
        var mockFile = CreateMockFile("export.zip", SampleZip);
        var controller = CreateController();

        var result = await controller.GetManifest(mockFile.Object);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(SampleManifest);
    }

    [Test]
    public async Task GetManifest_WhenServiceThrows_Returns500()
    {
        _mockService.Setup(s => s.GetManifestAsync(It.IsAny<byte[]>())).ThrowsAsync(new Exception("Manifest error"));
        var mockFile = CreateMockFile("export.zip", SampleZip);
        var controller = CreateController();

        var result = await controller.GetManifest(mockFile.Object);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion
}
