using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Model;
using BackEncordados.Materials.Repository.Strings;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using CuerdasServiceType = BackEncordados.Materials.Service.Cuerdas.CuerdasService;
using ICuerdasRepositoryType = BackEncordados.Materials.Repository.Strings.ICuerdasRepository;
using TestEncordados.Unit.Fixtures;

namespace TestEncordados.Unit.Services.MaterialsService;

public class CuerdasServiceTests
{
private readonly Mock<ICuerdasRepositoryType> _mockRepo;
    private readonly Mock<ILogger<CuerdasServiceType>> _mockLogger;
    private readonly CuerdasServiceType _service;

    public CuerdasServiceTests()
    {
        _mockRepo = new Mock<ICuerdasRepositoryType>();
        _mockLogger = new Mock<ILogger<CuerdasServiceType>>();
        _service = new CuerdasServiceType(_mockLogger.Object, _mockRepo.Object);
    }

    private static CuerdaFilterdto CreateFilter(Ulid? tournamentId = null, string search = "", int page = 1, int size = 10) => new(tournamentId, search, page, size);

    [Test]
    public async Task FindAllAsync_ReturnsPageResponse()
    {
        var filter = CreateFilter();
        var cuerdas = new List<Cuerdas>
        {
            CuerdasBuilder.Create(id: 1L, marca: "Babolat", modelo: "Pro Tour"),
            CuerdasBuilder.Create(id: 2L, marca: "Luxilon", modelo: "ALU Power")
        };
        var pagedResult = (Items: cuerdas.AsEnumerable(), TotalCount: 2);

        _mockRepo.Setup(r => r.FindAllAsync(It.IsAny<CuerdaFilterdto>()))
            .ReturnsAsync(pagedResult);

        var result = await _service.FindAllAsync(filter);

        result.Content.Should().HaveCount(2);
        result.TotalElements.Should().Be(2);
    }

    [Test]
    public async Task FindByNameAsync_ExistingCuerda_ReturnsSuccess()
    {
        var name = "Babolat Pro Tour";
        var cuerda = CuerdasBuilder.Create(marca: "Babolat", modelo: "Pro Tour");

        _mockRepo.Setup(r => r.FindByNameAsync(name)).ReturnsAsync(cuerda);

        var result = await _service.FindByNameAsync(name);

        result.IsSuccess.Should().BeTrue();
        result.Value.Marca.Should().Be("Babolat");
    }

    [Test]
    public async Task FindByNameAsync_NonExistingCuerda_ReturnsNotFoundError()
    {
        var name = "NonExistent";

        _mockRepo.Setup(r => r.FindByNameAsync(name)).ReturnsAsync((Cuerdas?)null);

        var result = await _service.FindByNameAsync(name);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CuerdaNotFoundError>();
    }

    [Test]
    public async Task FindByIdAsync_ExistingCuerda_ReturnsSuccess()
    {
        var id = 1L;
        var cuerda = CuerdasBuilder.Create(id: id);

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(cuerda);

        var result = await _service.FindByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(id);
    }

    [Test]
    public async Task FindByIdAsync_NonExistingCuerda_ReturnsNotFoundError()
    {
        var id = 999L;

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((Cuerdas?)null);

        var result = await _service.FindByIdAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CuerdaNotFoundError>();
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsSuccess()
    {
        var request = new CuerdaRequestDto
        {
            Marca = "Babolat",
            Modelo = "Pro Tour",
            Stock = 10,
            Precio = 25.99,
            StringFormat = "Reel",
            StringsType = "Polyester",
            TournamentId = Ulid.NewUlid()
        };
        var created = CuerdasBuilder.Create(marca: "Babolat", modelo: "Pro Tour");

        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Cuerdas>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Marca.Should().Be("Babolat");
    }

    [Test]
    public async Task CreateAsync_RepositoryFailure_ReturnsConflictError()
    {
        var request = new CuerdaRequestDto
        {
            Marca = "Babolat",
            Modelo = "Pro Tour",
            Stock = 10,
            Precio = 25.99,
            StringFormat = "Reel",
            StringsType = "Polyester",
            TournamentId = Ulid.NewUlid()
        };

        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Cuerdas>())).ReturnsAsync((Cuerdas?)null);

        var result = await _service.CreateAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
    }

    [Test]
    public async Task UpdateAsync_ExistingCuerda_ReturnsSuccess()
    {
        var id = 1L;
        var existing = CuerdasBuilder.Create(id: id, marca: "Old", modelo: "Model");
        var patch = new CuerdaPatchDto { Marca = "New", Modelo = "Updated" };
        var updated = CuerdasBuilder.Create(id: id, marca: "New", modelo: "Updated");

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Cuerdas>(), id)).ReturnsAsync(updated);

        var result = await _service.UpdateAsync(id, patch);

        result.IsSuccess.Should().BeTrue();
        result.Value.Marca.Should().Be("New");
    }

    [Test]
    public async Task UpdateAsync_NonExistingCuerda_ReturnsNotFoundError()
    {
        var id = 999L;
        var patch = new CuerdaPatchDto { Marca = "New" };

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((Cuerdas?)null);

        var result = await _service.UpdateAsync(id, patch);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CuerdaNotFoundError>();
    }

    [Test]
    public async Task DeleteAsync_ExistingCuerda_ReturnsSuccess()
    {
        var id = 1L;

        _mockRepo.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _service.DeleteAsync(id);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_NonExistingCuerda_ReturnsNotFoundError()
    {
        var id = 999L;

        _mockRepo.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

        var result = await _service.DeleteAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CuerdaNotFoundError>();
    }
}