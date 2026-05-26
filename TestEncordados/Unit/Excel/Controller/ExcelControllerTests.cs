using System;
using System.Security.Claims;
using BackEncordados.Excel.Controller;
using BackEncordados.Excel.Dto;
using BackEncordados.Excel.Service;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace TestEncordados.Unit.Excel.Controller;

/// <summary>
/// ExcelController test suite.
/// 
/// NOTE: The controller has a catch block for FormatException that returns 400 BadRequest.
/// However, Ulid.Parse() in .NET 10 throws ArgumentException (not FormatException) for invalid ULID strings,
/// so the FormatException catch block is never executed in practice.
/// We do not test the FormatException catch block because it represents unreachable code.
/// Generic exceptions are caught and return 500 InternalServerError, which we test via ServiceThrows tests.
/// </summary>
public class ExcelControllerTests
{
    private readonly Mock<IExcelService> _mockService = new();
    private static readonly byte[] SampleExcel = [0x50, 0x4B, 0x03, 0x04]; // Minimal ZIP/XLSX header

    private ExcelController CreateController(string role = "Supervisor")
    {
        var controller = new ExcelController(
            NullLogger<ExcelController>.Instance,
            _mockService.Object
        );
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Ulid.NewUlid().ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
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

    #region ExportTournament

    [Test]
    public async Task ExportTournament_ReturnsFileContentResult()
    {
        var tournamentId = Ulid.NewUlid();
        _mockService.Setup(s => s.ExportTournamentAsync(It.IsAny<Ulid>(), tournamentId))
            .ReturnsAsync(SampleExcel);
        var controller = CreateController("Supervisor");

        var result = await controller.ExportTournament(tournamentId);

        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        fileResult.FileDownloadName.Should().MatchRegex($"torneo_{tournamentId}_\\d{{4}}-\\d{{2}}-\\d{{2}}\\.xlsx");
        fileResult.FileContents.Should().BeSameAs(SampleExcel);
    }

    [Test]
    public async Task ExportTournament_WhenUserIdMissing_ReturnsUnauthorized()
    {
        var tournamentId = Ulid.NewUlid();
        var controller = new ExcelController(
            NullLogger<ExcelController>.Instance,
            _mockService.Object
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        var result = await controller.ExportTournament(tournamentId);

        result.Should().BeOfType<UnauthorizedObjectResult>().Which.Value.Should().BeEquivalentTo(new { message = "User ID not found in token" });
    }

    [Test]
    public async Task ExportTournament_WhenUnauthorizedAccessException_ReturnsForbidden()
    {
        var tournamentId = Ulid.NewUlid();
        _mockService.Setup(s => s.ExportTournamentAsync(It.IsAny<Ulid>(), tournamentId))
            .ThrowsAsync(new UnauthorizedAccessException());
        var controller = CreateController("Supervisor");

        var result = await controller.ExportTournament(tournamentId);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);
        objectResult.Value.Should().BeEquivalentTo(new { message = "No tienes permisos para exportar este torneo" });
    }

    [Test]
    public async Task ExportTournament_WhenServiceThrows_ReturnsInternalServerError()
    {
        var tournamentId = Ulid.NewUlid();
        _mockService.Setup(s => s.ExportTournamentAsync(It.IsAny<Ulid>(), tournamentId))
            .ThrowsAsync(new Exception("Export error"));
        var controller = CreateController("Supervisor");

        var result = await controller.ExportTournament(tournamentId);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region ExportAdvanced

    [Test]
    public async Task ExportAdvanced_ReturnsFileContentResult()
    {
        var tournamentId = Ulid.NewUlid();
        _mockService.Setup(s => s.ExportAdvancedAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>()))
            .ReturnsAsync(SampleExcel);
        var controller = CreateController("Owner");

        var result = await controller.ExportAdvanced(tournamentId, "users,materials");

        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        fileResult.FileDownloadName.Should().MatchRegex($"torneo_advanced_{tournamentId}_\\d{{4}}-\\d{{2}}-\\d{{2}}\\.xlsx");
        fileResult.FileContents.Should().BeSameAs(SampleExcel);
    }

    [Test]
    public async Task ExportAdvanced_WithNullTypes_ReturnsFileContentResult()
    {
        var tournamentId = Ulid.NewUlid();
        _mockService.Setup(s => s.ExportAdvancedAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>()))
            .ReturnsAsync(SampleExcel);
        var controller = CreateController("Owner");

        var result = await controller.ExportAdvanced(tournamentId, null);

        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Should().BeSameAs(SampleExcel);
    }

    [Test]
    public async Task ExportAdvanced_WhenUserIdMissing_ReturnsUnauthorized()
    {
        var tournamentId = Ulid.NewUlid();
        var controller = new ExcelController(
            NullLogger<ExcelController>.Instance,
            _mockService.Object
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Owner")], "TestAuth")) }
        };

        var result = await controller.ExportAdvanced(tournamentId, "users");

        result.Should().BeOfType<UnauthorizedObjectResult>().Which.Value.Should().BeEquivalentTo(new { message = "User ID not found in token" });
    }

    [Test]
    public async Task ExportAdvanced_WhenRoleMissing_ReturnsUnauthorized()
    {
        var tournamentId = Ulid.NewUlid();
        var controller = new ExcelController(
            NullLogger<ExcelController>.Instance,
            _mockService.Object
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Ulid.NewUlid().ToString())], "TestAuth")) }
        };

        var result = await controller.ExportAdvanced(tournamentId, "users");

        result.Should().BeOfType<UnauthorizedObjectResult>().Which.Value.Should().BeEquivalentTo(new { message = "Role not found in token" });
    }

    [Test]
    public async Task ExportAdvanced_WhenUnauthorizedAccessException_ReturnsForbidden()
    {
        var tournamentId = Ulid.NewUlid();
        _mockService.Setup(s => s.ExportAdvancedAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException());
        var controller = CreateController("Owner");

        var result = await controller.ExportAdvanced(tournamentId, "users");

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);
        objectResult.Value.Should().BeEquivalentTo(new { message = "No tienes permisos para exportar este torneo" });
    }

    [Test]
    public async Task ExportAdvanced_WhenServiceThrows_ReturnsInternalServerError()
    {
        var tournamentId = Ulid.NewUlid();
        _mockService.Setup(s => s.ExportAdvancedAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Advanced export error"));
        var controller = CreateController("Owner");

        var result = await controller.ExportAdvanced(tournamentId, "users");

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task ExportAdvanced_WithEmptyTypesString_ParsesEmptyList()
    {
        var tournamentId = Ulid.NewUlid();
        _mockService.Setup(s => s.ExportAdvancedAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>()))
            .ReturnsAsync(SampleExcel);
        var controller = CreateController("Owner");

        var result = await controller.ExportAdvanced(tournamentId, "");

        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Should().BeSameAs(SampleExcel);
        _mockService.Verify(s => s.ExportAdvancedAsync(It.IsAny<Ulid>(), tournamentId, 
            It.Is<List<string>>(list => list.Count == 0), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ExportAdvanced_WithTypesContainingSpaces_RemovesEmptyEntries()
    {
        var tournamentId = Ulid.NewUlid();
        _mockService.Setup(s => s.ExportAdvancedAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>()))
            .ReturnsAsync(SampleExcel);
        var controller = CreateController("Owner");

        var result = await controller.ExportAdvanced(tournamentId, "users, materials, cuerdas");

        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Should().BeSameAs(SampleExcel);
        _mockService.Verify(s => s.ExportAdvancedAsync(It.IsAny<Ulid>(), tournamentId, 
            It.Is<List<string>>(list => list.Count == 3 && list[0] == "users" && list[1] == " materials" && list[2] == " cuerdas"), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ExportAdvanced_WithMultipleTypes_SplitsCorrectly()
    {
        var tournamentId = Ulid.NewUlid();
        _mockService.Setup(s => s.ExportAdvancedAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>()))
            .ReturnsAsync(SampleExcel);
        var controller = CreateController("Owner");

        var result = await controller.ExportAdvanced(tournamentId, "users,materials,cuerdas,tournament,pedidos");

        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Should().BeSameAs(SampleExcel);
        _mockService.Verify(s => s.ExportAdvancedAsync(It.IsAny<Ulid>(), tournamentId, 
            It.Is<List<string>>(list => list.Count == 5), It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region ImportAdvanced

    [Test]
    public async Task ImportAdvanced_WithNullFile_ReturnsBadRequest()
    {
        var tournamentId = Ulid.NewUlid();
        var controller = CreateController("Owner");

        var result = await controller.ImportAdvanced(null!, tournamentId, "users");

        result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeEquivalentTo(new { message = "No file provided" });
    }

    [Test]
    public async Task ImportAdvanced_WithEmptyFile_ReturnsBadRequest()
    {
        var tournamentId = Ulid.NewUlid();
        var mockFile = CreateMockFile("test.xlsx", Array.Empty<byte>());
        var controller = CreateController("Owner");

        var result = await controller.ImportAdvanced(mockFile.Object, tournamentId, "users");

        result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeEquivalentTo(new { message = "No file provided" });
    }

    [Test]
    public async Task ImportAdvanced_WithInvalidExtension_ReturnsBadRequest()
    {
        var tournamentId = Ulid.NewUlid();
        var mockFile = CreateMockFile("data.txt", SampleExcel);
        var controller = CreateController("Owner");

        var result = await controller.ImportAdvanced(mockFile.Object, tournamentId, "users");

        result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeEquivalentTo(new { message = "File must be an .xlsx file" });
    }

    [Test]
    public async Task ImportAdvanced_ReturnsOkResult()
    {
        var tournamentId = Ulid.NewUlid();
        var importResult = new ExcelImportResultDto { UsersCreated = 5, UsersUpdated = 2 };
        _mockService.Setup(s => s.ImportAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .ReturnsAsync(importResult);
        var mockFile = CreateMockFile("test.xlsx", SampleExcel);
        var controller = CreateController("Owner");

        var result = await controller.ImportAdvanced(mockFile.Object, tournamentId, "users,materials");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(importResult);
    }

    [Test]
    public async Task ImportAdvanced_WhenUserIdMissing_ReturnsUnauthorized()
    {
        var tournamentId = Ulid.NewUlid();
        var mockFile = CreateMockFile("test.xlsx", SampleExcel);
        var controller = new ExcelController(
            NullLogger<ExcelController>.Instance,
            _mockService.Object
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Owner")], "TestAuth")) }
        };

        var result = await controller.ImportAdvanced(mockFile.Object, tournamentId, "users");

        result.Should().BeOfType<UnauthorizedObjectResult>().Which.Value.Should().BeEquivalentTo(new { message = "User ID not found in token" });
    }

    [Test]
    public async Task ImportAdvanced_WhenRoleMissing_ReturnsUnauthorized()
    {
        var tournamentId = Ulid.NewUlid();
        var mockFile = CreateMockFile("test.xlsx", SampleExcel);
        var controller = new ExcelController(
            NullLogger<ExcelController>.Instance,
            _mockService.Object
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Ulid.NewUlid().ToString())], "TestAuth")) }
        };

        var result = await controller.ImportAdvanced(mockFile.Object, tournamentId, "users");

        result.Should().BeOfType<UnauthorizedObjectResult>().Which.Value.Should().BeEquivalentTo(new { message = "Role not found in token" });
    }

    [Test]
    public async Task ImportAdvanced_WhenUnauthorizedAccessException_ReturnsForbidden()
    {
        var tournamentId = Ulid.NewUlid();
        var mockFile = CreateMockFile("test.xlsx", SampleExcel);
        _mockService.Setup(s => s.ImportAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .ThrowsAsync(new UnauthorizedAccessException());
        var controller = CreateController("Owner");

        var result = await controller.ImportAdvanced(mockFile.Object, tournamentId, "users");

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);
        objectResult.Value.Should().BeEquivalentTo(new { message = "No tienes permisos para importar a este torneo" });
    }

    [Test]
    public async Task ImportAdvanced_WhenServiceThrows_ReturnsInternalServerError()
    {
        var tournamentId = Ulid.NewUlid();
        var mockFile = CreateMockFile("test.xlsx", SampleExcel);
        _mockService.Setup(s => s.ImportAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .ThrowsAsync(new Exception("Import error"));
        var controller = CreateController("Owner");

        var result = await controller.ImportAdvanced(mockFile.Object, tournamentId, "users");

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task ImportAdvanced_WithUppercaseExtension_Succeeds()
    {
        var tournamentId = Ulid.NewUlid();
        var importResult = new ExcelImportResultDto { UsersCreated = 1 };
        var mockFile = CreateMockFile("test.XLSX", SampleExcel);
        _mockService.Setup(s => s.ImportAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .ReturnsAsync(importResult);
        var controller = CreateController("Owner");

        var result = await controller.ImportAdvanced(mockFile.Object, tournamentId, "users");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(importResult);
    }

    [Test]
    public async Task ImportAdvanced_WithMixedCaseExtension_Succeeds()
    {
        var tournamentId = Ulid.NewUlid();
        var importResult = new ExcelImportResultDto { UsersCreated = 1 };
        var mockFile = CreateMockFile("test.XlSx", SampleExcel);
        _mockService.Setup(s => s.ImportAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .ReturnsAsync(importResult);
        var controller = CreateController("Owner");

        var result = await controller.ImportAdvanced(mockFile.Object, tournamentId, "users");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(importResult);
    }

    [Test]
    public async Task ImportAdvanced_WithUppercaseInvalidExtension_ReturnsBadRequest()
    {
        var tournamentId = Ulid.NewUlid();
        var mockFile = CreateMockFile("data.TXT", SampleExcel);
        var controller = CreateController("Owner");

        var result = await controller.ImportAdvanced(mockFile.Object, tournamentId, "users");

        result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeEquivalentTo(new { message = "File must be an .xlsx file" });
    }

    [Test]
    public async Task ImportAdvanced_WithEmptyTypesString_ParsesEmptyList()
    {
        var tournamentId = Ulid.NewUlid();
        var importResult = new ExcelImportResultDto { UsersCreated = 0 };
        var mockFile = CreateMockFile("test.xlsx", SampleExcel);
        _mockService.Setup(s => s.ImportAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .ReturnsAsync(importResult);
        var controller = CreateController("Owner");

        var result = await controller.ImportAdvanced(mockFile.Object, tournamentId, "");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(importResult);
        _mockService.Verify(s => s.ImportAsync(It.IsAny<Ulid>(), tournamentId, 
            It.Is<List<string>>(list => list.Count == 0), It.IsAny<string>(), It.IsAny<Stream>()), Times.Once);
    }

    [Test]
    public async Task ImportAdvanced_WithTypesContainingSpaces_RemovesEmptyEntries()
    {
        var tournamentId = Ulid.NewUlid();
        var importResult = new ExcelImportResultDto { UsersCreated = 0 };
        var mockFile = CreateMockFile("test.xlsx", SampleExcel);
        _mockService.Setup(s => s.ImportAsync(It.IsAny<Ulid>(), tournamentId, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .ReturnsAsync(importResult);
        var controller = CreateController("Owner");

        var result = await controller.ImportAdvanced(mockFile.Object, tournamentId, "users, materials");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(importResult);
        _mockService.Verify(s => s.ImportAsync(It.IsAny<Ulid>(), tournamentId, 
            It.Is<List<string>>(list => list.Count == 2 && list[0] == "users" && list[1] == " materials"), It.IsAny<string>(), It.IsAny<Stream>()), Times.Once);
    }

    #endregion
}
