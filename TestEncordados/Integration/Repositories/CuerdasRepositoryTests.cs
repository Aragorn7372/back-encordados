using BackEncordados.Common.Database.Config;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Model;
using BackEncordados.Materials.Repository.Strings;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace TestEncordados.Integration.Repositories;

public class CuerdasRepositoryTests
{
    private PostgreSqlContainer _postgres = null!;
    private MaterialsDbContext _context = null!;
    private CuerdasRepository _repository = null!;
    private Mock<ILogger<CuerdasRepository>> _loggerMock = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("cuerdas_test_isolated")
            .Build();
        
        await _postgres.StartAsync();
        
        var options = new DbContextOptionsBuilder<MaterialsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        
        _context = new MaterialsDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        
        _loggerMock = new Mock<ILogger<CuerdasRepository>>();
        _repository = new CuerdasRepository(_loggerMock.Object, _context);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // ========== CREATE TESTS ==========

    [Test]
    public async Task CreateAsync_ValidCuerda_ReturnsCuerdaWithId()
    {
        var tournamentId = Ulid.NewUlid();
        var cuerda = new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = "Babolat",
            Modelo = "Synthetic Gut",
            Stock = 50,
            Precio = 35.99,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.SyntheticGut,
            IsDeleted = false
        };

        var result = await _repository.CreateAsync(cuerda);

        result.Id.Should().BeGreaterThan(0);
        result.Marca.Should().Be("Babolat");
        result.StringFormat.Should().Be(FormatoCuerda.Set);
        result.StringsType.Should().Be(StringsType.SyntheticGut);
    }

    [Test]
    public async Task CreateAsync_WithZeroStock_CreatesSuccessfully()
    {
        var cuerda = new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Wilson",
            Modelo = "Natural Gut",
            Stock = 0,
            Precio = 50.0,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.NaturalGut,
            IsDeleted = false
        };

        var result = await _repository.CreateAsync(cuerda);

        result.Id.Should().BeGreaterThan(0);
        result.Stock.Should().Be(0);
    }

    [Test]
    public async Task CreateAsync_WithZeroPrice_CreatesSuccessfully()
    {
        var cuerda = new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Yonex",
            Modelo = "Polyester",
            Stock = 10,
            Precio = 0.0,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        };

        var result = await _repository.CreateAsync(cuerda);

        result.Id.Should().BeGreaterThan(0);
        result.Precio.Should().Be(0.0);
    }

    [Test]
    public async Task CreateAsync_WithNegativeStock_CreatesButStoresValue()
    {
        var cuerda = new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Head",
            Modelo = "Hybrid",
            Stock = -5,
            Precio = 45.0,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Hybrid,
            IsDeleted = false
        };

        var result = await _repository.CreateAsync(cuerda);

        result.Id.Should().BeGreaterThan(0);
        result.Stock.Should().Be(-5);
    }

    [Test]
    public async Task CreateAsync_WithNegativePrice_CreatesButStoresValue()
    {
        var cuerda = new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Prince",
            Modelo = "Multifilament",
            Stock = 10,
            Precio = -99.99,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        };

        var result = await _repository.CreateAsync(cuerda);

        result.Id.Should().BeGreaterThan(0);
        result.Precio.Should().Be(-99.99);
    }

    // ========== FIND BY ID TESTS ==========

    [Test]
    public async Task FindByIdAsync_ExistingCuerda_ReturnsCuerda()
    {
        var tournamentId = Ulid.NewUlid();
        var cuerda = new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = "Tecnifibre",
            Modelo = "Multifilament Pro",
            Stock = 30,
            Precio = 29.99,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        };
        var savedCuerda = await _repository.CreateAsync(cuerda);

        var result = await _repository.FindByIdAsync(savedCuerda.Id);

        result.Should().NotBeNull();
        result!.Marca.Should().Be("Tecnifibre");
        result.StringsType.Should().Be(StringsType.Multifilament);
    }

    [Test]
    public async Task FindByIdAsync_NonExistingCuerda_ReturnsNull()
    {
        var result = await _repository.FindByIdAsync(99999);

        result.Should().BeNull();
    }

    // ========== FIND BY NAME TESTS ==========

    [Test]
    public async Task FindByNameAsync_ExistingCuerdaByMarca_ReturnsCuerda()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueMarca = $"FindByName_Marca_{Ulid.NewUlid()}";
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = uniqueMarca,
            Modelo = "Model",
            Stock = 20,
            Precio = 19.99,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var result = await _repository.FindByNameAsync(uniqueMarca);

        result.Should().NotBeNull();
        result!.Marca.Should().Be(uniqueMarca);
    }

    [Test]
    public async Task FindByNameAsync_ExistingCuerdaByModelo_ReturnsCuerda()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueModelo = $"FindByName_Modelo_{Ulid.NewUlid()}";
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = "Brand",
            Modelo = uniqueModelo,
            Stock = 20,
            Precio = 19.99,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.SyntheticGut,
            IsDeleted = false
        });

        var result = await _repository.FindByNameAsync(uniqueModelo);

        result.Should().NotBeNull();
        result!.Modelo.Should().Be(uniqueModelo);
    }

    [Test]
    public async Task FindByNameAsync_NonExistingCuerda_ReturnsNull()
    {
        var result = await _repository.FindByNameAsync("NonExistent_" + Ulid.NewUlid());

        result.Should().BeNull();
    }

    [Test]
    public async Task FindByNameAsync_DeletedCuerda_ReturnsNull()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueMarca = $"FindByName_Deleted_{Ulid.NewUlid()}";
        
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = uniqueMarca,
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        await _repository.DeleteAsync(cuerda.Id);
        var result = await _repository.FindByNameAsync(uniqueMarca);

        result.Should().BeNull();
    }

    [Test]
    public async Task FindByNameAsync_EmptyString_ReturnsNull()
    {
        var result = await _repository.FindByNameAsync("");

        result.Should().BeNull();
    }

    // ========== FIND ALL BASIC TESTS ==========

    [Test]
    public async Task FindAllAsync_ReturnsNonDeletedCuerdas()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"mat1_{uniqueSuffix}",
            Modelo = "m1",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"mat2_{uniqueSuffix}",
            Modelo = "m2",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament,
            IsDeleted = true
        });

        var filter = new CuerdaFilterdto(null, $"mat1_{uniqueSuffix}", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.Should().AllSatisfy(c => c.IsDeleted.Should().BeFalse());
    }

    [Test]
    public async Task FindAllAsync_WithTournamentId_FiltersByTournament()
    {
        var tournamentId1 = Ulid.NewUlid();
        var tournamentId2 = Ulid.NewUlid();
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId1,
            Marca = "Tournament1",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId2,
            Marca = "Tournament2",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.NaturalGut,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(tournamentId1, "", 0, 10, "id", "asc");
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
            await _repository.CreateAsync(new Cuerdas
            {
                TournamentId = tournamentId,
                Marca = $"Mat{i}_{filterSearch}",
                Modelo = "Model",
                Stock = i,
                Precio = i * 10,
                StringFormat = FormatoCuerda.Reel,
                StringsType = StringsType.Polyester,
                IsDeleted = false
            });
        }

        var filter1 = new CuerdaFilterdto(null, filterSearch, 0, 2, "id", "asc");
        var (items1, totalCount1) = await _repository.FindAllAsync(filter1);

        totalCount1.Should().Be(3);
        items1.Count().Should().Be(2);

        var filter2 = new CuerdaFilterdto(null, filterSearch, 1, 2, "id", "asc");
        var (items2, totalCount2) = await _repository.FindAllAsync(filter2);

        totalCount2.Should().Be(3);
        items2.Count().Should().Be(1);
    }

    // ========== SEARCH TESTS ==========

    [Test]
    public async Task FindAllAsync_SearchByModelo_FiltersCorrectly()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = "Brand1",
            Modelo = $"UniqueModel_{uniqueSuffix}",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = "Brand2",
            Modelo = "OtherModel",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, $"UniqueModel_{uniqueSuffix}", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().Modelo.Should().Contain(uniqueSuffix);
    }

    [Test]
    public async Task FindAllAsync_SearchByStringFormat_FiltersCorrectly()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"ReelBrand_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"SetBrand_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, "Reel", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().BeGreaterThanOrEqualTo(1);
        items.Should().Contain(c => c.StringFormat == FormatoCuerda.Reel);
    }

    [Test]
    public async Task FindAllAsync_SearchByStringsType_FiltersCorrectly()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"PolyBrand_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"SyntheticBrand_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.SyntheticGut,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, "Polyester", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().BeGreaterThanOrEqualTo(1);
        items.Should().Contain(c => c.StringsType == StringsType.Polyester);
    }

    [Test]
    public async Task FindAllAsync_SearchById_FiltersCorrectly()
    {
        var tournamentId = Ulid.NewUlid();
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = "Brand",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, cuerda.Id.ToString(), 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().BeGreaterThanOrEqualTo(1);
        items.Should().Contain(c => c.Id == cuerda.Id);
    }

    [Test]
    public async Task FindAllAsync_EmptySearch_ReturnsAll()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"Mat1_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"Mat2_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(tournamentId, "", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(2);
        items.Count().Should().Be(2);
    }

    [Test]
    public async Task FindAllAsync_NoMatches_ReturnsEmptyResult()
    {
        var uniqueSearch = $"NonExistent_{Ulid.NewUlid()}";
        var filter = new CuerdaFilterdto(null, uniqueSearch, 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(0);
        items.Should().BeEmpty();
    }

    // ========== SORTING TESTS ==========

    [Test]
    public async Task FindAllAsync_SortByMarcaAscending_ReturnsSortedByMarca()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_Marca_ASC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"Zebra_{searchPrefix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"Apple_{searchPrefix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"Mango_{searchPrefix}",
            Modelo = "Model",
            Stock = 30,
            Precio = 30,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.SyntheticGut,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, searchPrefix, 0, 10, "marca", "asc");
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
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"Zebra_{searchPrefix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"Apple_{searchPrefix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.NaturalGut,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, searchPrefix, 0, 10, "marca", "desc");
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
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = "Brand",
            Modelo = $"ModelZ_{searchPrefix}",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = "Brand",
            Modelo = $"ModelA_{searchPrefix}",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.SyntheticGut,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, searchPrefix, 0, 10, "modelo", "asc");
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
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = "Brand",
            Modelo = $"ModelZ_{searchPrefix}",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = "Brand",
            Modelo = $"ModelA_{searchPrefix}",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, searchPrefix, 0, 10, "modelo", "desc");
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
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 100,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 5,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.SyntheticGut,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 50,
            Precio = 30,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, searchPrefix, 0, 10, "stock", "asc");
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
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 100,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 5,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.NaturalGut,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, searchPrefix, 0, 10, "stock", "desc");
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
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 10,
            Precio = 99.99,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 20,
            Precio = 9.99,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.SyntheticGut,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 30,
            Precio = 49.99,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Hybrid,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, searchPrefix, 0, 10, "precio", "asc");
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
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 10,
            Precio = 99.99,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 20,
            Precio = 9.99,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, searchPrefix, 0, 10, "precio", "desc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList[0].Precio.Should().Be(99.99);
        itemsList[1].Precio.Should().Be(9.99);
    }

    [Test]
    public async Task FindAllAsync_SortByStringFormatAscending_ReturnsSortedByStringFormat()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_Format_ASC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.SyntheticGut,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, searchPrefix, 0, 10, "stringformat", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList[0].StringFormat.Should().Be(FormatoCuerda.Reel);
        itemsList[1].StringFormat.Should().Be(FormatoCuerda.Set);
    }

    [Test]
    public async Task FindAllAsync_SortByStringFormatDescending_ReturnsSortedByStringFormatDesc()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_Format_DESC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, searchPrefix, 0, 10, "stringformat", "desc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList[0].StringFormat.Should().Be(FormatoCuerda.Set);
        itemsList[1].StringFormat.Should().Be(FormatoCuerda.Reel);
    }

    [Test]
    public async Task FindAllAsync_SortByStringsTypeAscending_ReturnsSortedByStringsType()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_StringType_ASC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.SyntheticGut,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, searchPrefix, 0, 10, "stringstype", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList.Should().HaveCountGreaterThanOrEqualTo(2);
        // Verify both types are present
        itemsList.Should().Contain(c => c.StringsType == StringsType.Polyester);
        itemsList.Should().Contain(c => c.StringsType == StringsType.SyntheticGut);
        // Verify they're sorted - Polyester (0) should come before SyntheticGut (2)
        var polyesterIndex = itemsList.FindIndex(c => c.StringsType == StringsType.Polyester);
        var syntheticIndex = itemsList.FindIndex(c => c.StringsType == StringsType.SyntheticGut);
        polyesterIndex.Should().BeLessThan(syntheticIndex);
    }

    [Test]
    public async Task FindAllAsync_SortByStringsTypeDescending_ReturnsSortedByStringsTypeDesc()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        var searchPrefix = $"Sort_StringType_DESC_{uniqueSuffix}";
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.SyntheticGut,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = searchPrefix,
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(null, searchPrefix, 0, 10, "stringstype", "desc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList[0].StringsType.Should().Be(StringsType.SyntheticGut);
        itemsList[1].StringsType.Should().Be(StringsType.Polyester);
    }

    [Test]
    public async Task FindAllAsync_InvalidSortBy_DefaultsToIdSorting()
    {
        var tournamentId = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        var id1 = (await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"Mat_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        })).Id;
        
        var id2 = (await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = $"Mat_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        })).Id;

        var filter = new CuerdaFilterdto(null, uniqueSuffix, 0, 10, "invalid_sort_field", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        var itemsList = items.ToList();
        itemsList.Should().HaveCountGreaterThanOrEqualTo(2);
        itemsList[0].Id.Should().BeLessThan(itemsList[1].Id);
    }

    [Test]
    public async Task FindAllAsync_SearchCombinations_TournamentAndSearch()
    {
        var tournamentId1 = Ulid.NewUlid();
        var tournamentId2 = Ulid.NewUlid();
        var uniqueSuffix = Ulid.NewUlid().ToString()[..8];
        
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId1,
            Marca = $"SearchMat_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId2,
            Marca = $"SearchMat_{uniqueSuffix}",
            Modelo = "Model",
            Stock = 20,
            Precio = 20,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.SyntheticGut,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(tournamentId1, $"SearchMat_{uniqueSuffix}", 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().TournamentId.Should().Be(tournamentId1);
    }

    // ========== UPDATE TESTS ==========

    [Test]
    public async Task UpdateAsync_NonExistingCuerda_ReturnsNull()
    {
        var cuerda = new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        };

        var result = await _repository.UpdateAsync(cuerda, 99999);

        result.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_WithZeroStock_UpdatesStock()
    {
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 50,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var updateCuerda = new Cuerdas
        {
            Stock = 0,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament
        };

        var result = await _repository.UpdateAsync(updateCuerda, cuerda.Id);

        result.Should().NotBeNull();
        result!.Stock.Should().Be(0);
    }

    [Test]
    public async Task UpdateAsync_WithNegativeStock_SkipsUpdate()
    {
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 50,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var updateCuerda = new Cuerdas
        {
            Stock = -10,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament
        };

        var result = await _repository.UpdateAsync(updateCuerda, cuerda.Id);

        result.Should().NotBeNull();
        result!.Stock.Should().Be(50); // Should remain unchanged
    }

    [Test]
    public async Task UpdateAsync_WithZeroPrice_UpdatesPrice()
    {
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var updateCuerda = new Cuerdas
        {
            Stock = 10,
            Precio = 0.0,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament
        };

        var result = await _repository.UpdateAsync(updateCuerda, cuerda.Id);

        result.Should().NotBeNull();
        result!.Precio.Should().Be(0.0);
    }

    [Test]
    public async Task UpdateAsync_WithNegativePrice_SkipsUpdate()
    {
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var updateCuerda = new Cuerdas
        {
            Stock = 10,
            Precio = -10.0,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament
        };

        var result = await _repository.UpdateAsync(updateCuerda, cuerda.Id);

        result.Should().NotBeNull();
        result!.Precio.Should().Be(25.0); // Should remain unchanged
    }

    [Test]
    public async Task UpdateAsync_StringFormatChange_UpdatesFormat()
    {
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var updateCuerda = new Cuerdas
        {
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        };

        var result = await _repository.UpdateAsync(updateCuerda, cuerda.Id);

        result.Should().NotBeNull();
        result!.StringFormat.Should().Be(FormatoCuerda.Set);
    }

    [Test]
    public async Task UpdateAsync_StringsTypeChange_UpdatesType()
    {
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var updateCuerda = new Cuerdas
        {
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.SyntheticGut,
            IsDeleted = false
        };

        var result = await _repository.UpdateAsync(updateCuerda, cuerda.Id);

        result.Should().NotBeNull();
        result!.StringsType.Should().Be(StringsType.SyntheticGut);
    }

    [Test]
    public async Task UpdateAsync_PartialUpdate_OnlyMarcaChange()
    {
        var originalModelo = $"OriginalModelo_{Ulid.NewUlid()}";
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "OldMarca",
            Modelo = originalModelo,
            Stock = 50,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var updateCuerda = new Cuerdas
        {
            Marca = "NewMarca",
            Modelo = null!,
            Stock = -1,
            Precio = -1,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        };

        var result = await _repository.UpdateAsync(updateCuerda, cuerda.Id);

        result.Should().NotBeNull();
        result!.Marca.Should().Be("NewMarca");
        result.Modelo.Should().Be(originalModelo);
        result.Stock.Should().Be(50);
        result.Precio.Should().Be(25.0);
        result.StringFormat.Should().Be(FormatoCuerda.Set); // Always updates
        result.StringsType.Should().Be(StringsType.Multifilament); // Always updates
    }

    [Test]
    public async Task UpdateAsync_PartialUpdate_OnlyStringFormatChange()
    {
        var originalMarca = $"OriginalMarca_{Ulid.NewUlid()}";
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = originalMarca,
            Modelo = "Model",
            Stock = 10,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var updateCuerda = new Cuerdas
        {
            Marca = null!,
            Modelo = null!,
            Stock = -1,
            Precio = -1,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        };

        var result = await _repository.UpdateAsync(updateCuerda, cuerda.Id);

        result.Should().NotBeNull();
        result!.Marca.Should().Be(originalMarca);
        result.StringFormat.Should().Be(FormatoCuerda.Set);
        result.Stock.Should().Be(10);
    }

    [Test]
    public async Task UpdateAsync_Persistence_VerifyInDatabase()
    {
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Original",
            Modelo = "Model",
            Stock = 10,
            Precio = 25.0,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var updateCuerda = new Cuerdas
        {
            Marca = "Updated",
            Modelo = null!,
            Stock = -1,
            Precio = -1,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Multifilament,
            IsDeleted = false
        };

        await _repository.UpdateAsync(updateCuerda, cuerda.Id);
        var retrievedCuerda = await _repository.FindByIdAsync(cuerda.Id);

        retrievedCuerda.Should().NotBeNull();
        retrievedCuerda!.Marca.Should().Be("Updated");
        retrievedCuerda.StringFormat.Should().Be(FormatoCuerda.Set);
    }

    // ========== DELETE TESTS ==========

    [Test]
    public async Task DeleteAsync_ExistingCuerda_MarksAsDeleted()
    {
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "DeleteTest",
            Modelo = "Model",
            Stock = 5,
            Precio = 5.0,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });
        var cuerdaId = cuerda.Id;

        var result = await _repository.DeleteAsync(cuerdaId);

        result.Should().BeTrue();
        
        var deletedCuerda = await _context.Cuerdas.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == cuerdaId);
        deletedCuerda.Should().NotBeNull();
        deletedCuerda!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_NonExistingCuerda_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(99999);

        result.Should().BeFalse();
    }

    [Test]
    public async Task DeleteAsync_AlreadyDeletedCuerda_ReturnsTrueAgain()
    {
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = Ulid.NewUlid(),
            Marca = "Test",
            Modelo = "Test",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var firstDelete = await _repository.DeleteAsync(cuerda.Id);
        var secondDelete = await _repository.DeleteAsync(cuerda.Id);

        firstDelete.Should().BeTrue();
        secondDelete.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_DeletedCuerdaNotInFindResults()
    {
        var tournamentId = Ulid.NewUlid();
        var cuerda = await _repository.CreateAsync(new Cuerdas
        {
            TournamentId = tournamentId,
            Marca = "ToDelete",
            Modelo = "Model",
            Stock = 10,
            Precio = 10,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester,
            IsDeleted = false
        });

        var filter = new CuerdaFilterdto(tournamentId, "ToDelete", 0, 10, "id", "asc");
        var (beforeDelete, countBefore) = await _repository.FindAllAsync(filter);
        countBefore.Should().BeGreaterThanOrEqualTo(1);

        await _repository.DeleteAsync(cuerda.Id);

        var (afterDelete, countAfter) = await _repository.FindAllAsync(filter);
        countAfter.Should().Be(countBefore - 1);
        afterDelete.Should().NotContain(c => c.Id == cuerda.Id);
    }
}
