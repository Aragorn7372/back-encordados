using BackEncordados.Common.Database.Config;
using BackEncordados.Excel.Dto;
using BackEncordados.Excel.Repository;
using BackEncordados.Materials.Model;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Testcontainers.PostgreSql;
using Cuerda = BackEncordados.Materials.Model.Cuerdas;
using StringFormat = BackEncordados.Materials.Model.FormatoCuerda;

namespace TestEncordados.Integration.Repositories;

public class ExcelRepositoryTests
{
    private PostgreSqlContainer _postgresContainer = null!;
    private PedidosDbContext _pedidosContext = null!;
    private UserDbContext _userContext = null!;
    private TalleresDbContext _talleresContext = null!;
    private MaterialsDbContext _materialsContext = null!;
    private ExcelRepository _repository = null!;
    private Mock<ILogger<MaterialsExcelRepository>> _materialsLoggerMock = null!;
    private Mock<ILogger<UserExcelRepository>> _userLoggerMock = null!;
    private Mock<ILogger<TalleresExcelRepository>> _talleresLoggerMock = null!;
    private Mock<ILogger<PedidosExcelRepository>> _pedidosLoggerMock = null!;
    private Mock<ILogger<ExcelRepository>> _excelLoggerMock = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Start PostgreSQL container (only for UserDbContext and MaterialsDbContext)
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
        
        await _postgresContainer.StartAsync();

        var connectionString = _postgresContainer.GetConnectionString();

        // Setup Pedidos Context with InMemory (used with MongoDB in prod)
        var pedidosOptions = new DbContextOptionsBuilder<PedidosDbContext>()
            .UseInMemoryDatabase("PedidosTestDb")
            .Options;
        _pedidosContext = new PedidosDbContext(pedidosOptions);
        await _pedidosContext.Database.EnsureCreatedAsync();

        // Setup User Context with PostgreSQL
        var userOptions = new DbContextOptionsBuilder<UserDbContext>()
            .UseNpgsql(connectionString + ";Database=users_test")
            .Options;
        _userContext = new UserDbContext(userOptions);
        await _userContext.Database.EnsureCreatedAsync();

        // Setup Talleres Context with InMemory (used with MongoDB in prod)
        var talleresOptions = new DbContextOptionsBuilder<TalleresDbContext>()
            .UseInMemoryDatabase("TalleresTestDb")
            .Options;
        _talleresContext = new TalleresDbContext(talleresOptions);
        await _talleresContext.Database.EnsureCreatedAsync();

        // Setup Materials Context with PostgreSQL
        var materialsOptions = new DbContextOptionsBuilder<MaterialsDbContext>()
            .UseNpgsql(connectionString + ";Database=materials_test")
            .Options;
        _materialsContext = new MaterialsDbContext(materialsOptions);
        await _materialsContext.Database.EnsureCreatedAsync();

        _materialsLoggerMock = new Mock<ILogger<MaterialsExcelRepository>>();
        _userLoggerMock = new Mock<ILogger<UserExcelRepository>>();
        _talleresLoggerMock = new Mock<ILogger<TalleresExcelRepository>>();
        _pedidosLoggerMock = new Mock<ILogger<PedidosExcelRepository>>();
        _excelLoggerMock = new Mock<ILogger<ExcelRepository>>();

        var materialsRepo = new MaterialsExcelRepository(_materialsContext, _materialsLoggerMock.Object);
        var userRepo = new UserExcelRepository(_userContext, _userLoggerMock.Object);
        var talleresRepo = new TalleresExcelRepository(_talleresContext, _talleresLoggerMock.Object);
        var pedidosRepo = new PedidosExcelRepository(_pedidosContext, _pedidosLoggerMock.Object);

        _repository = new ExcelRepository(
            pedidosRepo,
            userRepo,
            talleresRepo,
            materialsRepo,
            _excelLoggerMock.Object
        );
    }

    [TearDown]
    public async Task TearDown()
    {
        // Clean up Pedidos
        var pedidos = _pedidosContext.Pedidos.ToList();
        foreach (var pedido in pedidos)
        {
            _pedidosContext.Pedidos.Remove(pedido);
        }

        var pedidoLineas = _pedidosContext.PedidoLineas.ToList();
        foreach (var linea in pedidoLineas)
        {
            _pedidosContext.PedidoLineas.Remove(linea);
        }
        await _pedidosContext.SaveChangesAsync();

        // Clean up Users
        var users = _userContext.Users.IgnoreQueryFilters().ToList();
        foreach (var user in users)
        {
            _userContext.Users.Remove(user);
        }
        await _userContext.SaveChangesAsync();

        // Clean up Tournaments
        var tournaments = _talleresContext.Partidos.IgnoreQueryFilters().ToList();
        foreach (var tournament in tournaments)
        {
            _talleresContext.Partidos.Remove(tournament);
        }
        await _talleresContext.SaveChangesAsync();

        // Clean up Materials
        var materials = _materialsContext.Materiales.ToList();
        foreach (var material in materials)
        {
            _materialsContext.Materiales.Remove(material);
        }

        var cuerdas = _materialsContext.Cuerdas.ToList();
        foreach (var cuerda in cuerdas)
        {
            _materialsContext.Cuerdas.Remove(cuerda);
        }
        await _materialsContext.SaveChangesAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _pedidosContext.DisposeAsync();
        await _userContext.DisposeAsync();
        await _talleresContext.DisposeAsync();
        await _materialsContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    #region GetPedidosByTournamentAsync Tests

    [Test]
    public async Task GetPedidosByTournamentAsync_WithNoPedidos_ReturnsEmptyList()
    {
        var tournamentId = Ulid.NewUlid();

        var result = await _repository.GetPedidosByTournamentAsync(tournamentId);

        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetPedidosByTournamentAsync_WithPedidos_ReturnsMatching()
    {
        var tournamentId = Ulid.NewUlid();
        var otherTournamentId = Ulid.NewUlid();

        var pedido = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Price = 50.0,
            Machine = "Machine1"
        };
        _pedidosContext.Pedidos.Add(pedido);

        var otherPedido = new Pedidos
        {
            TournamentId = otherTournamentId,
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Price = 30.0,
            Machine = "Machine2"
        };
        _pedidosContext.Pedidos.Add(otherPedido);
        await _pedidosContext.SaveChangesAsync();

        var result = await _repository.GetPedidosByTournamentAsync(tournamentId);

        result.Should().ContainSingle();
        result.First().Price.Should().Be(50.0);
    }

    #endregion

    #region GetUsersByIdsAsync Tests

    [Test]
    public async Task GetUsersByIdsAsync_WithNoIds_ReturnsEmpty()
    {
        var result = await _repository.GetUsersByIdsAsync(new List<Ulid>());

        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetUsersByIdsAsync_WithExistingIds_ReturnsLookup()
    {
        var userId = Ulid.NewUlid();
        var user = new User
        {
            Id = userId,
            Username = "test_user",
            Email = "test@test.com",
            PasswordHash = "hash",
            Name = "Test User",
            IsDeleted = false
        };
        _userContext.Users.Add(user);
        await _userContext.SaveChangesAsync();

        var result = await _repository.GetUsersByIdsAsync(new List<Ulid> { userId });

        result.Should().ContainKey(userId);
        result[userId].Username.Should().Be("test_user");
        result[userId].Name.Should().Be("Test User");
    }

    #endregion

    #region GetUsersByTournamentAsync Tests

    [Test]
    public async Task GetUsersByTournamentAsync_WithUsers_ReturnsMatching()
    {
        var tournamentId = Ulid.NewUlid();
        var user = new User
        {
            Username = "tournament_user",
            Email = "tuser@test.com",
            PasswordHash = "hash",
            Name = "Tournament User",
            TournamentId = tournamentId,
            IsDeleted = false
        };
        _userContext.Users.Add(user);
        await _userContext.SaveChangesAsync();

        var result = await _repository.GetUsersByTournamentAsync(tournamentId);

        result.Should().ContainSingle();
        result.First().Username.Should().Be("tournament_user");
    }

    #endregion

    #region GetMaterialsByTournamentAsync Tests

    [Test]
    public async Task GetMaterialsByTournamentAsync_WithMaterials_ReturnsMatching()
    {
        var tournamentId = Ulid.NewUlid();
        var material = new Material
        {
            TournamentId = tournamentId,
            Marca = "Wilson",
            Modelo = "Pro Staff",
            Stock = 5,
            Precio = 199.99,
            Type = MaterialType.Grip,
            IsDeleted = false
        };
        _materialsContext.Materiales.Add(material);
        await _materialsContext.SaveChangesAsync();

        var result = await _repository.GetMaterialsByTournamentAsync(tournamentId);

        result.Should().ContainSingle();
        result.First().Marca.Should().Be("Wilson");
    }

    #endregion

    #region GetCuerdasByTournamentAsync Tests

    [Test]
    public async Task GetCuerdasByTournamentAsync_WithCuerdas_ReturnsMatching()
    {
        var tournamentId = Ulid.NewUlid();
        var cuerda = new Cuerda
        {
            TournamentId = tournamentId,
            Marca = "Babolat",
            Modelo = "Pure Strike",
            Stock = 10,
            Precio = 12.50,
            Calibre = 1.25,
            StringFormat = StringFormat.Reel,
            StringsType = StringsType.NaturalGut,
            IsDeleted = false
        };
        _materialsContext.Cuerdas.Add(cuerda);
        await _materialsContext.SaveChangesAsync();

        var result = await _repository.GetCuerdasByTournamentAsync(tournamentId);

        result.Should().ContainSingle();
        result.First().Marca.Should().Be("Babolat");
    }

    #endregion

    #region GetTournamentByIdAsync Tests

    [Test]
    public async Task GetTournamentByIdAsync_WithExisting_ReturnsTournament()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments
        {
            Id = tournamentId,
            Owner = Ulid.NewUlid(),
            Title = "Test Tournament",
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(1),
            IsDeleted = false
        };
        _talleresContext.Partidos.Add(tournament);
        await _talleresContext.SaveChangesAsync();

        var result = await _repository.GetTournamentByIdAsync(tournamentId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Tournament");
    }

    [Test]
    public async Task GetTournamentByIdAsync_WithNonExistent_ReturnsNull()
    {
        var result = await _repository.GetTournamentByIdAsync(Ulid.NewUlid());

        result.Should().BeNull();
    }

    #endregion

    #region GetPedidoLineasByPedidoIdsAsync Tests

    [Test]
    public async Task GetPedidoLineasByPedidoIdsAsync_WithLineas_ReturnsMatching()
    {
        var tournamentId = Ulid.NewUlid();
        var pedido = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Price = 100.0,
            Machine = "M1"
        };
        _pedidosContext.Pedidos.Add(pedido);
        await _pedidosContext.SaveChangesAsync();

        var linea = new PedidoLinea
        {
            PedidoId = pedido.Id,
            RaquetModel = "Pro Staff",
            Nudos = 16,
            DateString = DateTime.UtcNow,
            Logotype = true,
            Color = "Red",
            StringSetup = new StringSetup { StringV = "Babolat", TensionV = 50 },
            Status = Status.PENDING
        };
        _pedidosContext.PedidoLineas.Add(linea);
        await _pedidosContext.SaveChangesAsync();

        var result = await _repository.GetPedidoLineasByPedidoIdsAsync(new List<Ulid> { pedido.Id });

        result.Should().ContainSingle();
        result.First().RaquetModel.Should().Be("Pro Staff");
    }

    #endregion

    #region IsUserSupervisorOfTournamentAsync Tests

    [Test]
    public async Task IsUserSupervisorOfTournamentAsync_WithSupervisor_ReturnsTrue()
    {
        var tournamentId = Ulid.NewUlid();
        var supervisorId = Ulid.NewUlid();
        var now = DateTime.UtcNow;

        var tournament = new Tournaments
        {
            Id = tournamentId,
            Owner = Ulid.NewUlid(),
            Title = "Test Tournament",
            StartTournament = now,
            EndTournament = now.AddDays(1),
            SupervisorList = new List<Ulid> { supervisorId },
            IsDeleted = false
        };
        _talleresContext.Partidos.Add(tournament);
        await _talleresContext.SaveChangesAsync();

        var result = await _repository.IsUserSupervisorOfTournamentAsync(supervisorId, tournamentId);

        result.Should().BeTrue();
    }

    [Test]
    public async Task IsUserSupervisorOfTournamentAsync_WithNonSupervisor_ReturnsFalse()
    {
        var tournamentId = Ulid.NewUlid();
        var supervisorId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();

        var tournament = new Tournaments
        {
            Id = tournamentId,
            Owner = Ulid.NewUlid(),
            Title = "Test Tournament",
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(1),
            SupervisorList = new List<Ulid> { supervisorId },
            IsDeleted = false
        };
        _talleresContext.Partidos.Add(tournament);
        await _talleresContext.SaveChangesAsync();

        var result = await _repository.IsUserSupervisorOfTournamentAsync(userId, tournamentId);

        result.Should().BeFalse();
    }

    [Test]
    public async Task IsUserSupervisorOfTournamentAsync_WithNonExistentTournament_ReturnsFalse()
    {
        var tournamentId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();

        var result = await _repository.IsUserSupervisorOfTournamentAsync(userId, tournamentId);

        result.Should().BeFalse();
    }

    [Test]
    public async Task IsUserSupervisorOfTournamentAsync_WithEmptySupervisorList_ReturnsFalse()
    {
        var tournamentId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();

        var tournament = new Tournaments
        {
            Id = tournamentId,
            Owner = Ulid.NewUlid(),
            Title = "Test Tournament",
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(1),
            SupervisorList = new List<Ulid>(),
            IsDeleted = false
        };
        _talleresContext.Partidos.Add(tournament);
        await _talleresContext.SaveChangesAsync();

        var result = await _repository.IsUserSupervisorOfTournamentAsync(userId, tournamentId);

        result.Should().BeFalse();
    }

    [Test]
    public async Task IsUserSupervisorOfTournamentAsync_WithMultipleSupervisors_IdentifiesCorrectOne()
    {
        var tournamentId = Ulid.NewUlid();
        var supervisor1 = Ulid.NewUlid();
        var supervisor2 = Ulid.NewUlid();
        var supervisor3 = Ulid.NewUlid();

        var tournament = new Tournaments
        {
            Id = tournamentId,
            Owner = Ulid.NewUlid(),
            Title = "Test Tournament",
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(1),
            SupervisorList = new List<Ulid> { supervisor1, supervisor2, supervisor3 },
            IsDeleted = false
        };
        _talleresContext.Partidos.Add(tournament);
        await _talleresContext.SaveChangesAsync();

        var result1 = await _repository.IsUserSupervisorOfTournamentAsync(supervisor1, tournamentId);
        var result2 = await _repository.IsUserSupervisorOfTournamentAsync(supervisor2, tournamentId);
        var result3 = await _repository.IsUserSupervisorOfTournamentAsync(supervisor3, tournamentId);
        var resultFalse = await _repository.IsUserSupervisorOfTournamentAsync(Ulid.NewUlid(), tournamentId);

        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
        resultFalse.Should().BeFalse();
    }

    #endregion

    #region IsUserOwnerOfTournamentAsync Tests

    [Test]
    public async Task IsUserOwnerOfTournamentAsync_WithOwner_ReturnsTrue()
    {
        var tournamentId = Ulid.NewUlid();
        var ownerId = Ulid.NewUlid();

        var tournament = new Tournaments
        {
            Id = tournamentId,
            Owner = ownerId,
            Title = "Test Tournament",
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(1),
            IsDeleted = false
        };
        _talleresContext.Partidos.Add(tournament);
        await _talleresContext.SaveChangesAsync();

        var result = await _repository.IsUserOwnerOfTournamentAsync(ownerId, tournamentId);

        result.Should().BeTrue();
    }

    [Test]
    public async Task IsUserOwnerOfTournamentAsync_WithNonOwner_ReturnsFalse()
    {
        var tournamentId = Ulid.NewUlid();
        var ownerId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();

        var tournament = new Tournaments
        {
            Id = tournamentId,
            Owner = ownerId,
            Title = "Test Tournament",
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(1),
            IsDeleted = false
        };
        _talleresContext.Partidos.Add(tournament);
        await _talleresContext.SaveChangesAsync();

        var result = await _repository.IsUserOwnerOfTournamentAsync(userId, tournamentId);

        result.Should().BeFalse();
    }

    [Test]
    public async Task IsUserOwnerOfTournamentAsync_WithNonExistentTournament_ReturnsFalse()
    {
        var tournamentId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();

        var result = await _repository.IsUserOwnerOfTournamentAsync(userId, tournamentId);

        result.Should().BeFalse();
    }

    #endregion
}
