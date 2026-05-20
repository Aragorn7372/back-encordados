using BackEncordados.Common.Database.Config;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Model;
using BackEncordados.Materials.Repository.Materials;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace TestEncordados.Integration.Repositories;

public class MaterialsRepositoryTests
{
    private PostgreSqlContainer _postgres = null!;
    private MaterialsDbContext _context = null!;
    private MaterialsRepository _repository = null!;
    private Mock<ILogger<MaterialsRepository>> _loggerMock = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("materials_test_isolated")
            .Build();
        
        await _postgres.StartAsync();
        
        var options = new DbContextOptionsBuilder<MaterialsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        
        _context = new MaterialsDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        
        _loggerMock = new Mock<ILogger<MaterialsRepository>>();
        _repository = new MaterialsRepository(_loggerMock.Object, _context);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Test]
    public async Task CreateAsync_ValidMaterial_ReturnsMaterialWithId()
    {
        var tournamentId = Ulid.NewUlid();
        var material = new Material
        {
            TournamentId = tournamentId,
            Marca = "Head",
            Modelo = "Extreme",
            Stock = 50,
            Precio = 35.99,
            Type = MaterialType.Grip,
            IsDeleted = false
        };

        var result = await _repository.CreateAsync(material);

        result.Id.Should().BeGreaterThan(0);
        result.Marca.Should().Be("Head");
    }

    [Test]
    public async Task FindByIdAsync_ExistingMaterial_ReturnsMaterial()
    {
        var tournamentId = Ulid.NewUlid();
        var material = new Material
        {
            TournamentId = tournamentId,
            Marca = "Wilson",
            Modelo = "Pro Staff",
            Stock = 30,
            Precio = 29.99,
            Type = MaterialType.Overgrip,
            IsDeleted = false
        };
        var savedMaterial = await _repository.CreateAsync(material);

        var result = await _repository.FindByIdAsync(savedMaterial.Id);

        result.Should().NotBeNull();
        result!.Marca.Should().Be("Wilson");
    }

    [Test]
    public async Task FindByIdAsync_NonExistingMaterial_ReturnsNull()
    {
        var result = await _repository.FindByIdAsync(99999);

        result.Should().BeNull();
    }

    [Test]
    public async Task FindByNameAsync_ExistingMaterial_ReturnsMaterial()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = "Babolat",
            Modelo = $"RPM_{uniqueSuffix}",
            Stock = 20,
            Precio = 19.99,
            Type = MaterialType.Overgrip,
            IsDeleted = false
        });

        var result = await _repository.FindByNameAsync($"RPM_{uniqueSuffix}");

        result.Should().NotBeNull();
        result!.Modelo.Should().Be($"RPM_{uniqueSuffix}");
    }

    [Test]
    public async Task FindByNameAsync_NonExistingMaterial_ReturnsNull()
    {
        var result = await _repository.FindByNameAsync("Non Existing " + Ulid.NewUlid());

        result.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_ExistingMaterial_UpdatesMaterial()
    {
        var tournamentId = Ulid.NewUlid();
        var material = new Material
        {
            TournamentId = tournamentId,
            Marca = "UpdateTest",
            Modelo = "Model",
            Stock = 10,
            Precio = 10.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        };
        var savedMaterial = await _repository.CreateAsync(material);
        
        savedMaterial.Stock = 100;
        savedMaterial.Precio = 25.99;

        var result = await _repository.UpdateAsync(savedMaterial, savedMaterial.Id);

        result.Should().NotBeNull();
        result!.Stock.Should().Be(100);
        result.Precio.Should().Be(25.99);
    }

    [Test]
    public async Task DeleteAsync_ExistingMaterial_MarksAsDeleted()
    {
        var tournamentId = Ulid.NewUlid();
        var material = new Material
        {
            TournamentId = tournamentId,
            Marca = "DeleteTest",
            Modelo = "Model",
            Stock = 5,
            Precio = 5.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        };
        var savedMaterial = await _repository.CreateAsync(material);
        var materialId = savedMaterial.Id;

        var result = await _repository.DeleteAsync(materialId);

        result.Should().BeTrue();
        
        var deletedMaterial = await _context.Materiales.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == materialId);
        deletedMaterial.Should().NotBeNull();
        deletedMaterial!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_NonExistingMaterial_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(99999);

        result.Should().BeFalse();
    }

    [Test]
    public async Task FindAllAsync_ReturnsNonDeletedMaterials()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"mat1_{uniqueSuffix}",
            Modelo = "m1",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"mat2_{uniqueSuffix}",
            Modelo = "m2",
            Stock = 20,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = true
        });

        var filter = new MaterialFilterDto(null, $"mat1_{uniqueSuffix}", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.Should().AllSatisfy(m => m.IsDeleted.Should().BeFalse());
    }

    [Test]
    public async Task FindAllAsync_WithSearch_FiltersByMarca()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"Search_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = "Other",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, $"Search_{uniqueSuffix}", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().Marca.Should().Be($"Search_{uniqueSuffix}");
    }

    [Test]
    public async Task FindAllAsync_WithTournamentId_FiltersByTournament()
    {
        var tournamentId1 = Ulid.NewUlid();
        var tournamentId2 = Ulid.NewUlid();
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId1,
            Marca = "Tournament1",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId2,
            Marca = "Tournament2",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(tournamentId1, "", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().TournamentId.Should().Be(tournamentId1);
    }

    [Test]
    public async Task FindAllAsync_Pagination_WorksCorrectly()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        var filterSearch = $"unique_pag_{uniqueSuffix}";
        
        for (int i = 1; i <= 3; i++)
        {
            await _repository.CreateAsync(new Material
            {
                TournamentId = tournamentId,
                Marca = $"Mat{i}_{filterSearch}",
                Modelo = "Model",
                Stock = i,
                Precio = i * 10,
                Type = MaterialType.Grip,
                IsDeleted = false
            });
        }

        var filter1 = new MaterialFilterDto(null, filterSearch, 0, 2, "id", "asc");
        var (items1, totalCount1) = await _repository.FindAllAsync(filter1);

        totalCount1.Should().Be(3);
        items1.Count().Should().Be(2);

        var filter2 = new MaterialFilterDto(null, filterSearch, 1, 2, "id", "asc");
        var (items2, totalCount2) = await _repository.FindAllAsync(filter2);

        totalCount2.Should().Be(3);
        items2.Count().Should().Be(1);
    }
}