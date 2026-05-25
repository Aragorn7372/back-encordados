using BackEncordados.Common.Dto;
using BackEncordados.Common.Service.Cloudinary;
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
    private readonly Mock<ICloudinaryService> _mockCloudinary;
    private readonly CuerdasServiceType _service;

    public CuerdasServiceTests()
    {
        _mockRepo = new Mock<ICuerdasRepositoryType>();
        _mockLogger = new Mock<ILogger<CuerdasServiceType>>();
        _mockCloudinary = new Mock<ICloudinaryService>();
        _service = new CuerdasServiceType(_mockLogger.Object, _mockRepo.Object, _mockCloudinary.Object);
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
            Calibre = 1.25,
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
            Calibre = 1.25,
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
        var patch = new CuerdaPatchDto { Marca = "New", Modelo = "Updated", Calibre = 1.25 };
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
        var patch = new CuerdaPatchDto { Marca = "New", Calibre = 1.25 };

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((Cuerdas?)null);

        var result = await _service.UpdateAsync(id, patch);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CuerdaNotFoundError>();
    }

    [Test]
    public async Task DeleteAsync_ExistingCuerda_ReturnsSuccess()
    {
        var id = 1L;
        var cuerda = CuerdasBuilder.Create(id: id);

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(cuerda);
        _mockRepo.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _service.DeleteAsync(id);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_NonExistingCuerda_ReturnsNotFoundError()
    {
        var id = 999L;

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((Cuerdas?)null);

        var result = await _service.DeleteAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CuerdaNotFoundError>();
    }

    [Test]
    public async Task FindAllAsync_WithSizeZero_DoesNotThrowDivideByZero()
    {
        var filter = CreateFilter(size: 0); 
        var cuerdas = new List<Cuerdas>(); // Lista vacía
        var pagedResult = (Items: cuerdas.AsEnumerable(), TotalCount: 0);

        _mockRepo.Setup(r => r.FindAllAsync(It.IsAny<CuerdaFilterdto>()))
            .ReturnsAsync(pagedResult);

        var result = await _service.FindAllAsync(filter);

        result.TotalPages.Should().Be(0);
        result.Content.Should().BeEmpty();
    }


    [Test]
    public async Task UpdateAsync_WithNegativePricesAndStock_DoesNotUpdateThoseFields()
    {
        var id = 1L;
        var existing = CuerdasBuilder.Create(id: id); 
        existing.Precio = 50.0;
        existing.Stock = 20;

        var patch = new CuerdaPatchDto { Precio = -5.0, Stock = -10, Calibre = 0 }; 
        
        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Cuerdas>(), id)).ReturnsAsync(existing);

        var result = await _service.UpdateAsync(id, patch);

        result.IsSuccess.Should().BeTrue();
        result.Value.Precio.Should().Be(50.0); // Debería mantener el precio original
        result.Value.Stock.Should().Be(20);    // Debería mantener el stock original
    }

    [Test]
    public async Task UpdateAsync_WithEnums_UpdatesFormatAndTypeSuccessfully()
    {
        var id = 1L;
        var existing = CuerdasBuilder.Create(id: id);
        
        var patch = new CuerdaPatchDto { StringFormat = "Set", StringsType = "Multifilament", Calibre = 0 }; 
        var updated = CuerdasBuilder.Create(id: id);
        updated.StringFormat = Enum.Parse<FormatoCuerda>("Set");
        updated.StringsType = Enum.Parse<StringsType>("Multifilament");

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Cuerdas>(), id)).ReturnsAsync(updated);

        var result = await _service.UpdateAsync(id, patch);

        result.IsSuccess.Should().BeTrue();
        result.Value.StringFormat.Should().Be("Set");
        result.Value.StringsType.Should().Be("Multifilament");
    }

    [Test]
    public async Task UpdateAsync_RepositoryFailsOnUpdate_ReturnsNotFoundError()
    {
        var id = 1L;
        var existing = CuerdasBuilder.Create(id: id);
        var patch = new CuerdaPatchDto { Marca = "New", Calibre = 1.25 };

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Cuerdas>(), id)).ReturnsAsync((Cuerdas?)null);

        var result = await _service.UpdateAsync(id, patch);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<CuerdaNotFoundError>();
    }
}