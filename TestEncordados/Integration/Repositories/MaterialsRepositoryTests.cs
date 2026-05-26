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

    // ========== SORTING TESTS ==========

    [Test]
    public async Task FindAllAsync_SortByMarcaAscending_ReturnsSortedByMarca()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_Marca_ASC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"Zebra_{searchPrefix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"Apple_{searchPrefix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"Mango_{searchPrefix}",
            Modelo = "Model",
            Stock = 30,
            Precio = 30,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, searchPrefix, 0, 10, "marca", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList.Should().HaveCount(3);
        itemsList[0].Marca.Should().Be($"Apple_{searchPrefix}");
        itemsList[1].Marca.Should().Be($"Mango_{searchPrefix}");
        itemsList[2].Marca.Should().Be($"Zebra_{searchPrefix}");
    }

    [Test]
    public async Task FindAllAsync_SortByMarcaDescending_ReturnsSortedByMarcaDesc()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_Marca_DESC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"Zebra_{searchPrefix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"Apple_{searchPrefix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, searchPrefix, 0, 10, "marca", "desc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList[0].Marca.Should().Be($"Zebra_{searchPrefix}");
        itemsList[1].Marca.Should().Be($"Apple_{searchPrefix}");
    }

    [Test]
    public async Task FindAllAsync_SortByModeloAscending_ReturnsSortedByModelo()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_Modelo_ASC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = "Brand",
            Modelo = $"ModelZ_{searchPrefix}",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = "Brand",
            Modelo = $"ModelA_{searchPrefix}",
            Stock = 20,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, searchPrefix, 0, 10, "modelo", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList[0].Modelo.Should().Be($"ModelA_{searchPrefix}");
        itemsList[1].Modelo.Should().Be($"ModelZ_{searchPrefix}");
    }

    [Test]
    public async Task FindAllAsync_SortByModeloDescending_ReturnsSortedByModeloDesc()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_Modelo_DESC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = "Brand",
            Modelo = $"ModelZ_{searchPrefix}",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = "Brand",
            Modelo = $"ModelA_{searchPrefix}",
            Stock = 20,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, searchPrefix, 0, 10, "modelo", "desc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList[0].Modelo.Should().Be($"ModelZ_{searchPrefix}");
        itemsList[1].Modelo.Should().Be($"ModelA_{searchPrefix}");
    }

    [Test]
    public async Task FindAllAsync_SortByStockAscending_ReturnsSortedByStock()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_Stock_ASC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 100,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 5,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 50,
            Precio = 30,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, searchPrefix, 0, 10, "stock", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList[0].Stock.Should().Be(5);
        itemsList[1].Stock.Should().Be(50);
        itemsList[2].Stock.Should().Be(100);
    }

    [Test]
    public async Task FindAllAsync_SortByStockDescending_ReturnsSortedByStockDesc()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_Stock_DESC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 100,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 5,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, searchPrefix, 0, 10, "stock", "desc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList[0].Stock.Should().Be(100);
        itemsList[1].Stock.Should().Be(5);
    }

    [Test]
    public async Task FindAllAsync_SortByPrecioAscending_ReturnsSortedByPrecio()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_Precio_ASC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 10,
            Precio = 99.99,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 20,
            Precio = 9.99,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 30,
            Precio = 49.99,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, searchPrefix, 0, 10, "precio", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList[0].Precio.Should().Be(9.99);
        itemsList[1].Precio.Should().Be(49.99);
        itemsList[2].Precio.Should().Be(99.99);
    }

    [Test]
    public async Task FindAllAsync_SortByPrecioDescending_ReturnsSortedByPrecioDesc()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_Precio_DESC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 10,
            Precio = 99.99,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 20,
            Precio = 9.99,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, searchPrefix, 0, 10, "precio", "desc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList[0].Precio.Should().Be(99.99);
        itemsList[1].Precio.Should().Be(9.99);
    }

    [Test]
    public async Task FindAllAsync_InvalidSortBy_DefaultsToIdSorting()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        var id1 = (await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"Mat_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        })).Id;
        
        var id2 = (await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"Mat_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = false
        })).Id;

        var filter = new MaterialFilterDto(null, uniqueSuffix, 0, 10, "invalid_sort_field", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList.Should().HaveCountGreaterThanOrEqualTo(2);
        itemsList[0].Id.Should().BeLessThan(itemsList[1].Id);
    }

    // ========== SEARCH EDGE CASES ==========

    [Test]
    public async Task FindAllAsync_SearchByModelo_FiltersCorrectly()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = "Brand1",
            Modelo = $"UniqueModel_{uniqueSuffix}",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = "Brand2",
            Modelo = "OtherModel",
            Stock = 20,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, $"UniqueModel_{uniqueSuffix}", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().Modelo.Should().Contain(uniqueSuffix);
    }

    [Test]
    public async Task FindAllAsync_SearchByType_FiltersCorrectly()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"GripBrand_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"OvergripBrand_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            Type = MaterialType.Overgrip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, "Grip", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().BeGreaterThanOrEqualTo(1);
        items.Should().Contain(m => m.Type == MaterialType.Grip);
    }

    [Test]
    public async Task FindAllAsync_SearchById_FiltersCorrectly()
    {
        var tournamentId = Ulid.NewUlid();
        var material = await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = "Brand",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, material.Id.ToString(), 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().BeGreaterThanOrEqualTo(1);
        items.Should().Contain(m => m.Id == material.Id);
    }

    [Test]
    public async Task FindAllAsync_EmptySearch_ReturnsAll()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"Mat1_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"Mat2_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(tournamentId, "", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(2);
        items.Count().Should().Be(2);
    }

    [Test]
    public async Task FindAllAsync_NoMatches_ReturnsEmptyResult()
    {
        var uniqueSearch = $"NonExistent_{Ulid.NewUlid()}";
        var filter = new MaterialFilterDto(null, uniqueSearch, 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Test]
    public async Task FindAllAsync_SearchWithSpecialCharacters_HandlesCorrectly()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = $"Brand-Special{uniqueSuffix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(null, $"Brand-Special", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().BeGreaterThanOrEqualTo(1);
        items.Should().Contain(m => m.Marca.Contains("Brand-Special"));
    }

    [Test]
    public async Task FindAllAsync_SearchCombinations_TournamentAndSearch()
    {
        var tournamentId1 = Ulid.NewUlid();
        var tournamentId2 = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId1,
            Marca = $"SearchMat_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId2,
            Marca = $"SearchMat_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(tournamentId1, $"SearchMat_{uniqueSuffix}", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().TournamentId.Should().Be(tournamentId1);
    }

    // ========== UPDATE EDGE CASES ==========

    [Test]
    public async Task UpdateAsync_NonExistingMaterial_ReturnsNull()
    {
        var material = new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        };

        var result = await _repository.UpdateAsync(material, 99999);

        result.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_WithZeroStock_UpdatesStock()
    {
        var material = await _repository.CreateAsync(new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 50,
            Precio = 25.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var updateMaterial = new Material
        {
            Stock = 0,
            Precio = 25.0,
            Type = MaterialType.Grip
        };

        var result = await _repository.UpdateAsync(updateMaterial, material.Id);

        result.Should().NotBeNull();
        result!.Stock.Should().Be(0);
    }

    [Test]
    public async Task UpdateAsync_WithNegativeStock_SkipsUpdate()
    {
        var material = await _repository.CreateAsync(new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 50,
            Precio = 25.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var updateMaterial = new Material
        {
            Stock = -10,
            Precio = 25.0,
            Type = MaterialType.Grip
        };

        var result = await _repository.UpdateAsync(updateMaterial, material.Id);

        result.Should().NotBeNull();
        result!.Stock.Should().Be(50); // Should remain unchanged
    }

    [Test]
    public async Task UpdateAsync_WithZeroPrice_UpdatesPrice()
    {
        var material = await _repository.CreateAsync(new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 25.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var updateMaterial = new Material
        {
            Stock = 10,
            Precio = 0.0,
            Type = MaterialType.Grip
        };

        var result = await _repository.UpdateAsync(updateMaterial, material.Id);

        result.Should().NotBeNull();
        result!.Precio.Should().Be(0.0);
    }

    [Test]
    public async Task UpdateAsync_WithNegativePrice_SkipsUpdate()
    {
        var material = await _repository.CreateAsync(new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 25.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var updateMaterial = new Material
        {
            Stock = 10,
            Precio = -10.0,
            Type = MaterialType.Grip
        };

        var result = await _repository.UpdateAsync(updateMaterial, material.Id);

        result.Should().NotBeNull();
        result!.Precio.Should().Be(25.0); // Should remain unchanged
    }

    [Test]
    public async Task UpdateAsync_TypeChange_UpdatesType()
    {
        var material = await _repository.CreateAsync(new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 25.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var updateMaterial = new Material
        {
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 25.0,
            Type = MaterialType.Overgrip,
            IsDeleted = false
        };

        var result = await _repository.UpdateAsync(updateMaterial, material.Id);

        result.Should().NotBeNull();
        result!.Type.Should().Be(MaterialType.Overgrip);
    }

    [Test]
    public async Task UpdateAsync_OnlyMarcaChange_KeepsOthersUnchanged()
    {
        var originalModelo = $"OriginalModelo_{Ulid.NewUlid()}";
        var material = await _repository.CreateAsync(new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "OldMarca",
            Modelo = originalModelo,
            Stock = 50,
            Precio = 25.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var updateMaterial = new Material
        {
            Marca = "NewMarca",
            Modelo = null!,
            Stock = -1, // Negative so it won't update
            Precio = -1, // Negative so it won't update
            Type = MaterialType.Overgrip, // Type always updates
            IsDeleted = false
        };

        var result = await _repository.UpdateAsync(updateMaterial, material.Id);

        result.Should().NotBeNull();
        result!.Marca.Should().Be("NewMarca");
        result.Modelo.Should().Be(originalModelo);
        result.Stock.Should().Be(50);
        result.Precio.Should().Be(25.0);
        result.Type.Should().Be(MaterialType.Overgrip); // Type is ALWAYS updated by the repository
    }

    [Test]
    public async Task UpdateAsync_OnlyStockChange_KeepsOthersUnchanged()
    {
        var originalMarca = $"OriginalMarca_{Ulid.NewUlid()}";
        var material = await _repository.CreateAsync(new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = originalMarca,
            Modelo = "Model",
            Stock = 10,
            Precio = 25.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var updateMaterial = new Material
        {
            Marca = null!,
            Modelo = null!,
            Stock = 100,
            Precio = -1, // Negative so it won't be updated
            Type = MaterialType.Overgrip, // This will still be updated
            IsDeleted = false
        };

        var result = await _repository.UpdateAsync(updateMaterial, material.Id);

        result.Should().NotBeNull();
        result!.Stock.Should().Be(100);
        result.Marca.Should().Be(originalMarca);
        result.Precio.Should().Be(25.0); // Should keep original since we passed -1
        result.Type.Should().Be(MaterialType.Overgrip); // Type always updates
    }

    // ========== DELETE EDGE CASES ==========

    [Test]
    public async Task DeleteAsync_AlreadyDeletedMaterial_ReturnsTrue()
    {
        var material = await _repository.CreateAsync(new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var firstDelete = await _repository.DeleteAsync(material.Id);
        var secondDelete = await _repository.DeleteAsync(material.Id);

        firstDelete.Should().BeTrue();
        secondDelete.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_DeletedMaterialNotInFindAll_WithoutTournament()
    {
        var tournamentId = Ulid.NewUlid();
        var material = await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = "ToDelete",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var filter = new MaterialFilterDto(tournamentId, "ToDelete", 0, 10, "id", "asc");
        var (beforeDelete, countBefore) = await _repository.FindAllAsync(filter);
        countBefore.Should().BeGreaterThanOrEqualTo(1);

        await _repository.DeleteAsync(material.Id);

        var (afterDelete, countAfter) = await _repository.FindAllAsync(filter);
        countAfter.Should().Be(countBefore - 1);
        afterDelete.Should().NotContain(m => m.Id == material.Id);
    }

    // ========== FINDBYNAME EDGE CASES ==========

    [Test]
    public async Task FindByNameAsync_DeletedMaterial_ReturnsNull()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueModelo = $"DeletedModelo_{Ulid.NewUlid()}";
        
        var material = await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = "Brand",
            Modelo = uniqueModelo,
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        await _repository.DeleteAsync(material.Id);
        var result = await _repository.FindByNameAsync(uniqueModelo);

        result.Should().BeNull();
    }

    [Test]
    public async Task FindByNameAsync_EmptyString_ReturnsNull()
    {
        var result = await _repository.FindByNameAsync("");

        result.Should().BeNull();
    }

    // ========== CREATE EDGE CASES ==========

    [Test]
    public async Task CreateAsync_WithZeroStock_CreatesSuccessfully()
    {
        var material = new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Brand",
            Modelo = $"Model_{Ulid.NewUlid()}",
            Stock = 0,
            Precio = 25.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        };

        var result = await _repository.CreateAsync(material);

        result.Id.Should().BeGreaterThan(0);
        result.Stock.Should().Be(0);
    }

    [Test]
    public async Task CreateAsync_WithZeroPrice_CreatesSuccessfully()
    {
        var material = new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Brand",
            Modelo = $"Model_{Ulid.NewUlid()}",
            Stock = 10,
            Precio = 0.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        };

        var result = await _repository.CreateAsync(material);

        result.Id.Should().BeGreaterThan(0);
        result.Precio.Should().Be(0.0);
    }

    [Test]
    public async Task CreateAsync_WithNegativeStock_CreatesButStoresValue()
    {
        var material = new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Brand",
            Modelo = $"Model_{Ulid.NewUlid()}",
            Stock = -5,
            Precio = 25.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        };

        var result = await _repository.CreateAsync(material);

        result.Id.Should().BeGreaterThan(0);
        result.Stock.Should().Be(-5);
    }

    [Test]
    public async Task CreateAsync_WithNegativePrice_CreatesButStoresValue()
    {
        var material = new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Brand",
            Modelo = $"Model_{Ulid.NewUlid()}",
            Stock = 10,
            Precio = -99.99,
            Type = MaterialType.Grip,
            IsDeleted = false
        };

        var result = await _repository.CreateAsync(material);

        result.Id.Should().BeGreaterThan(0);
        result.Precio.Should().Be(-99.99);
    }

    // ========== DATA CONSISTENCY ==========

    [Test]
    public async Task UpdateAsync_Persistence_VerifyInDatabase()
    {
        var material = await _repository.CreateAsync(new Material
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Original",
            Modelo = "Model",
            Stock = 10,
            Precio = 25.0,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var updateMaterial = new Material
        {
            Marca = "Updated",
            Modelo = null!,
            Stock = 0,
            Precio = 0.0,
            Type = MaterialType.Overgrip,
            IsDeleted = false
        };

        await _repository.UpdateAsync(updateMaterial, material.Id);
        var retrievedMaterial = await _repository.FindByIdAsync(material.Id);

        retrievedMaterial.Should().NotBeNull();
        retrievedMaterial!.Marca.Should().Be("Updated");
    }

    [Test]
    public async Task CreateAndDelete_FollowedBySearch_DoesntReturnDeleted()
    {
        var tournamentId = Ulid.NewUlid();
        var marca = $"CreateAndDelete_{Ulid.NewUlid()}";
        
        await _repository.CreateAsync(new Material
        {
            TournamentId = tournamentId,
            Marca = marca,
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            Type = MaterialType.Grip,
            IsDeleted = false
        });

        var createdMaterial = await _repository.FindByNameAsync("Model");
        createdMaterial.Should().NotBeNull();
        var createdId = createdMaterial!.Id;

        await _repository.DeleteAsync(createdId);
        var searchAfterDelete = await _repository.FindByNameAsync("Model");

        if (searchAfterDelete != null)
        {
            searchAfterDelete.Id.Should().NotBe(createdId);
        }
    }
}