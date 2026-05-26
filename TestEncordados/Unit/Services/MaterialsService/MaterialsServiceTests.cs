using BackEncordados.Common.Dto;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Common.Utils;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Model;
using BackEncordados.Materials.Repository.Materials;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MaterialsServiceType = BackEncordados.Materials.Service.Materials.MaterialsService;
using IMaterialsRepositoryType = BackEncordados.Materials.Repository.Materials.IMaterialsRepository;
using TestEncordados.Unit.Fixtures;

namespace TestEncordados.Unit.Services.MaterialsService;

public class MaterialsServiceTests
{
private readonly Mock<IMaterialsRepositoryType> _mockRepo;
    private readonly Mock<ILogger<MaterialsServiceType>> _mockLogger;
    private readonly Mock<ICloudinaryService> _mockCloudinary;
    private readonly MaterialsServiceType _service;

    public MaterialsServiceTests()
    {
        _mockRepo = new Mock<IMaterialsRepositoryType>();
        _mockLogger = new Mock<ILogger<MaterialsServiceType>>();
        _mockCloudinary = new Mock<ICloudinaryService>();
        _service = new MaterialsServiceType(_mockLogger.Object, _mockRepo.Object, _mockCloudinary.Object);
    }

    private static MaterialFilterDto CreateFilter(Ulid? tournamentId = null, string search = "", int page = 1, int size = 10) => new(tournamentId, search, page, size);

    [Test]
    public async Task FindAllAsync_ReturnsPageResponse()
    {
        var filter = CreateFilter();
        var materials = new List<Material>
        {
            MaterialBuilder.Create(id: 1L, marca: "Head", modelo: "Pro"),
            MaterialBuilder.Create(id: 2L, marca: "Wilson", modelo: "Blade")
        };
        var pagedResult = (Items: materials.AsEnumerable(), TotalCount: 2);

        _mockRepo.Setup(r => r.FindAllAsync(It.IsAny<MaterialFilterDto>()))
            .ReturnsAsync(pagedResult);

        var result = await _service.FindAllAsync(filter);

        result.Content.Should().HaveCount(2);
        result.TotalElements.Should().Be(2);
    }

    [Test]
    public async Task FindAllAsync_EmptyList_ReturnsEmptyPage()
    {
        var filter = CreateFilter();
        var pagedResult = (Items: Enumerable.Empty<Material>(), TotalCount: 0);

        _mockRepo.Setup(r => r.FindAllAsync(It.IsAny<MaterialFilterDto>()))
            .ReturnsAsync(pagedResult);

        var result = await _service.FindAllAsync(filter);

        result.Content.Should().BeEmpty();
        result.TotalElements.Should().Be(0);
    }

    [Test]
    public async Task FindByNameAsync_ExistingMaterial_ReturnsSuccess()
    {
        var name = "Head Pro";
        var material = MaterialBuilder.Create(marca: "Head", modelo: "Pro");

        _mockRepo.Setup(r => r.FindByNameAsync(name)).ReturnsAsync(material);

        var result = await _service.FindByNameAsync(name);

        result.IsSuccess.Should().BeTrue();
        result.Value.Marca.Should().Be("Head");
    }

    [Test]
    public async Task FindByNameAsync_NonExistingMaterial_ReturnsNotFoundError()
    {
        var name = "NonExistent";

        _mockRepo.Setup(r => r.FindByNameAsync(name)).ReturnsAsync((Material?)null);

        var result = await _service.FindByNameAsync(name);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<MaterialNotFoundError>();
    }

    [Test]
    public async Task FindByIdAsync_ExistingMaterial_ReturnsSuccess()
    {
        var id = 1L;
        var material = MaterialBuilder.Create(id: id);

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(material);

        var result = await _service.FindByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(id);
    }

    [Test]
    public async Task FindByIdAsync_NonExistingMaterial_ReturnsNotFoundError()
    {
        var id = 999L;

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((Material?)null);

        var result = await _service.FindByIdAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<MaterialNotFoundError>();
    }

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsSuccess()
    {
        var request = new MaterialRequestDto
        {
            Marca = "Head",
            Modelo = "Pro",
            Stock = 10,
            Precio = 25.99,
            Type = "Grip",
            TournamentId = Ulid.NewUlid()
        };
        var created = MaterialBuilder.Create(marca: "Head", modelo: "Pro");

        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Material>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Marca.Should().Be("Head");
    }

    [Test]
    public async Task CreateAsync_RepositoryFailure_ReturnsConflictError()
    {
        var request = new MaterialRequestDto
        {
            Marca = "Head",
            Modelo = "Pro",
            Stock = 10,
            Precio = 25.99,
            Type = "Grip",
            TournamentId = Ulid.NewUlid()
        };

        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Material>())).ReturnsAsync((Material?)null);

        var result = await _service.CreateAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<MaterialConflictError>();
    }

    [Test]
    public async Task UpdateAsync_ExistingMaterial_ReturnsSuccess()
    {
        var id = 1L;
        var existing = MaterialBuilder.Create(id: id, marca: "Old", modelo: "Model");
        var patch = new MaterialPatchDto { Marca = "New", Modelo = "Updated" };
        var updated = MaterialBuilder.Create(id: id, marca: "New", modelo: "Updated");

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Material>(), id)).ReturnsAsync(updated);

        var result = await _service.UpdateAsync(id, patch);

        result.IsSuccess.Should().BeTrue();
        result.Value.Marca.Should().Be("New");
    }

    [Test]
    public async Task UpdateAsync_NonExistingMaterial_ReturnsNotFoundError()
    {
        var id = 999L;
        var patch = new MaterialPatchDto { Marca = "New" };

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((Material?)null);

        var result = await _service.UpdateAsync(id, patch);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<MaterialNotFoundError>();
    }

    [Test]
    public async Task DeleteAsync_ExistingMaterial_ReturnsSuccess()
    {
        var id = 1L;
        var material = MaterialBuilder.Create(id: id);

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(material);
        _mockRepo.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _service.DeleteAsync(id);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_NonExistingMaterial_ReturnsNotFoundError()
    {
        var id = 999L;

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((Material?)null);

        var result = await _service.DeleteAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<MaterialNotFoundError>();
    }
}