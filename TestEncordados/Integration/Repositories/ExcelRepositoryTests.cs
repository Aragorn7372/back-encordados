using BackEncordados.Common.Database.Config;
using BackEncordados.Excel.Dto;
using BackEncordados.Excel.Repository;
using BackEncordados.Materials.Model;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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

        _repository = new ExcelRepository(
            _pedidosContext,
            _userContext,
            _talleresContext,
            _materialsContext
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

    #region GetTournamentDataAsync Tests

    [Test]
    public async Task GetTournamentDataAsync_WithNoPedidos_ReturnsEmptyList()
    {
        var tournamentId = Ulid.NewUlid();

        var result = await _repository.GetTournamentDataAsync(tournamentId);

        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetTournamentDataAsync_WithSinglePedido_ReturnsTournamentRow()
    {
        var tournamentId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();

        // Create user
        var user = new User
        {
            Id = userId,
            Username = "player_test",
            Email = "player@test.com",
            PasswordHash = "hash",
            Name = "Player Test",
            IsDeleted = false
        };
        _userContext.Users.Add(user);
        await _userContext.SaveChangesAsync();

        // Create pedido
        var pedido = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = playerId,
            AssignedTo = Ulid.NewUlid(),
            Price = 50.0,
            Machine = "Machine1"
        };
        _pedidosContext.Pedidos.Add(pedido);
        await _pedidosContext.SaveChangesAsync();

        var result = await _repository.GetTournamentDataAsync(tournamentId);

        result.Should().ContainSingle();
        var row = result.First();
        row.RacketCount.Should().Be(1);
        row.TotalPrice.Should().Be(50.0m);
        row.Username.Should().Be("Unknown"); // PlayerId doesn't match userId
    }

    [Test]
    public async Task GetTournamentDataAsync_WithMultiplePedidosSamePlayer_CountsAllRackets()
    {
        var tournamentId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();

        // Create user
        var user = new User
        {
            Id = userId,
            Username = "player_multi",
            Email = "player_multi@test.com",
            PasswordHash = "hash",
            Name = "Multi Player",
            IsDeleted = false
        };
        _userContext.Users.Add(user);
        await _userContext.SaveChangesAsync();

        // Create multiple pedidos for same player but use userId as playerId
        for (int i = 0; i < 3; i++)
        {
            var pedido = new Pedidos
            {
                TournamentId = tournamentId,
                PlayerId = userId,
                AssignedTo = Ulid.NewUlid(),
                Price = 50.0 + i,
                Machine = $"Machine{i}"
            };
            _pedidosContext.Pedidos.Add(pedido);
        }
        await _pedidosContext.SaveChangesAsync();

        var result = await _repository.GetTournamentDataAsync(tournamentId);

        result.Should().ContainSingle();
        var row = result.First();
        row.RacketCount.Should().Be(3);
        row.TotalPrice.Should().Be(153.0m); // 50 + 51 + 52
        row.Username.Should().Be("player_multi");
    }

    [Test]
    public async Task GetTournamentDataAsync_WithMultiplePlayersAndPedidos_AggregatesAndSortsCorrectly()
    {
        var tournamentId = Ulid.NewUlid();
        var playerId1 = Ulid.NewUlid();
        var playerId2 = Ulid.NewUlid();

        // Create users
        var user1 = new User
        {
            Id = playerId1,
            Username = "zebra_player",
            Email = "zebra@test.com",
            PasswordHash = "hash",
            Name = "Zebra Player",
            IsDeleted = false
        };
        var user2 = new User
        {
            Id = playerId2,
            Username = "alpha_player",
            Email = "alpha@test.com",
            PasswordHash = "hash",
            Name = "Alpha Player",
            IsDeleted = false
        };
        _userContext.Users.AddRange(user1, user2);
        await _userContext.SaveChangesAsync();

        // Create pedidos for both players
        var pedidos = new[]
        {
            new Pedidos { TournamentId = tournamentId, PlayerId = playerId1, AssignedTo = Ulid.NewUlid(), Price = 50.0, Machine = "M1" },
            new Pedidos { TournamentId = tournamentId, PlayerId = playerId1, AssignedTo = Ulid.NewUlid(), Price = 60.0, Machine = "M2" },
            new Pedidos { TournamentId = tournamentId, PlayerId = playerId2, AssignedTo = Ulid.NewUlid(), Price = 70.0, Machine = "M3" },
        };
        _pedidosContext.Pedidos.AddRange(pedidos);
        await _pedidosContext.SaveChangesAsync();

        var result = await _repository.GetTournamentDataAsync(tournamentId);

        result.Should().HaveCount(2);
        var resultList = result.ToList();
        // Should be sorted by username: alpha_player, zebra_player
        resultList[0].Username.Should().Be("alpha_player");
        resultList[0].RacketCount.Should().Be(1);
        resultList[0].TotalPrice.Should().Be(70.0m);
        
        resultList[1].Username.Should().Be("zebra_player");
        resultList[1].RacketCount.Should().Be(2);
        resultList[1].TotalPrice.Should().Be(110.0m);
    }

    [Test]
    public async Task GetTournamentDataAsync_WithUnknownPlayerId_ReturnsSafeDefaults()
    {
        var tournamentId = Ulid.NewUlid();
        var unknownPlayerId = Ulid.NewUlid();

        var pedido = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = unknownPlayerId,
            AssignedTo = Ulid.NewUlid(),
            Price = 50.0,
            Machine = "M1"
        };
        _pedidosContext.Pedidos.Add(pedido);
        await _pedidosContext.SaveChangesAsync();

        var result = await _repository.GetTournamentDataAsync(tournamentId);

        result.Should().ContainSingle();
        var row = result.First();
        row.Username.Should().Be("Unknown");
        row.Name.Should().Be("Unknown");
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

    #region GetAdvancedDataAsync Tests

    [Test]
    public async Task GetAdvancedDataAsync_WithEmptyTypes_ReturnsEmptyData()
    {
        var tournamentId = Ulid.NewUlid();

        var result = await _repository.GetAdvancedDataAsync(tournamentId, new List<string>());

        result.Users.Should().BeEmpty();
        result.Materials.Should().BeEmpty();
        result.Cuerdas.Should().BeEmpty();
        result.Tournament.Should().BeEmpty();
        result.Pedidos.Should().BeEmpty();
        result.PedidoLineas.Should().BeEmpty();
    }

    [Test]
    public async Task GetAdvancedDataAsync_WithUsersType_ReturnsUsers()
    {
        var tournamentId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();

        var user = new User
        {
            Id = userId,
            Username = "excel_user",
            Email = "excel@test.com",
            PasswordHash = "hash",
            Name = "Excel User",
            Phone = "+34123456789",
            TournamentId = tournamentId,
            IsDeleted = false
        };
        _userContext.Users.Add(user);
        await _userContext.SaveChangesAsync();

        var result = await _repository.GetAdvancedDataAsync(tournamentId, new List<string> { "users" });

        result.Users.Should().ContainSingle();
        var userDto = result.Users.First();
        userDto.Username.Should().Be("excel_user");
        userDto.Email.Should().Be("excel@test.com");
        userDto.Name.Should().Be("Excel User");
        userDto.Phone.Should().Be("+34123456789");
        userDto.TournamentId.Should().Be(tournamentId.ToString());
    }

    [Test]
    public async Task GetAdvancedDataAsync_WithMaterialsType_ReturnsMaterials()
    {
        var tournamentId = Ulid.NewUlid();

        var material = new Material
        {
            TournamentId = tournamentId,
            Marca = "Wilson",
            Modelo = "Pro Staff 97",
            Stock = 10,
            Precio = 199.99,
            Type = MaterialType.Overgrip,
            IsDeleted = false
        };
        _materialsContext.Materiales.Add(material);
        await _materialsContext.SaveChangesAsync();

        var result = await _repository.GetAdvancedDataAsync(tournamentId, new List<string> { "materials" });

        result.Materials.Should().ContainSingle();
        var materialDto = result.Materials.First();
        materialDto.Marca.Should().Be("Wilson");
        materialDto.Modelo.Should().Be("Pro Staff 97");
        materialDto.Stock.Should().Be(10);
        materialDto.Precio.Should().Be(199.99);
        materialDto.Type.Should().Be("Overgrip");
    }

    [Test]
    public async Task GetAdvancedDataAsync_WithCuerdasType_ReturnsCuerdas()
    {
        var tournamentId = Ulid.NewUlid();

        var cuerda = new Cuerda
        {
            TournamentId = tournamentId,
            Marca = "Babolat",
            Modelo = "Pure Strike",
            Stock = 20,
            Precio = 12.50,
            StringFormat = StringFormat.Reel,
            StringsType = StringsType.NaturalGut,
            IsDeleted = false
        };
        _materialsContext.Cuerdas.Add(cuerda);
        await _materialsContext.SaveChangesAsync();

        var result = await _repository.GetAdvancedDataAsync(tournamentId, new List<string> { "cuerdas" });

        result.Cuerdas.Should().ContainSingle();
        var cuerdasDto = result.Cuerdas.First();
        cuerdasDto.Marca.Should().Be("Babolat");
        cuerdasDto.Modelo.Should().Be("Pure Strike");
        cuerdasDto.Stock.Should().Be(20);
        cuerdasDto.Precio.Should().Be(12.50);
        cuerdasDto.StringFormat.Should().Be("Reel");
        cuerdasDto.StringsType.Should().Be("NaturalGut");
    }

    [Test]
    public async Task GetAdvancedDataAsync_WithTournamentType_ReturnsTournament()
    {
        var tournamentId = Ulid.NewUlid();
        var ownerId = Ulid.NewUlid();
        var supervisor1 = Ulid.NewUlid();
        var supervisor2 = Ulid.NewUlid();
        var worker1 = Ulid.NewUlid();

        var tournament = new Tournaments
        {
            Id = tournamentId,
            Owner = ownerId,
            Title = "My Tournament",
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(30),
            Logotype = "image.jpg",
            WorkersList = new List<Ulid> { worker1 },
            SupervisorList = new List<Ulid> { supervisor1, supervisor2 },
            IsDeleted = false
        };
        _talleresContext.Partidos.Add(tournament);
        await _talleresContext.SaveChangesAsync();

        var result = await _repository.GetAdvancedDataAsync(tournamentId, new List<string> { "tournament" });

        result.Tournament.Should().ContainSingle();
        var tournamentDto = result.Tournament.First();
        tournamentDto.Title.Should().Be("My Tournament");
        tournamentDto.Owner.Should().Be(ownerId.ToString());
        tournamentDto.Logotype.Should().Be("image.jpg");
        tournamentDto.WorkersList.Should().Contain(worker1.ToString());
        tournamentDto.SupervisorList.Should().Contain(supervisor1.ToString());
        tournamentDto.SupervisorList.Should().Contain(supervisor2.ToString());
    }

    [Test]
    public async Task GetAdvancedDataAsync_WithPedidosType_ReturnsPedidosAndLineas()
    {
        var tournamentId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var assignedTo = Ulid.NewUlid();

        var pedido = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = playerId,
            AssignedTo = assignedTo,
            Machine = "Machine1",
            Comments = "Test comments",
            Price = 150.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT
        };
        _pedidosContext.Pedidos.Add(pedido);
        await _pedidosContext.SaveChangesAsync();

        var result = await _repository.GetAdvancedDataAsync(tournamentId, new List<string> { "pedidos" });

        result.Pedidos.Should().ContainSingle();
        var pedidoDto = result.Pedidos.First();
        pedidoDto.PlayerId.Should().Be(playerId.ToString());
        pedidoDto.AssignedTo.Should().Be(assignedTo.ToString());
        pedidoDto.Machine.Should().Be("Machine1");
        pedidoDto.Comments.Should().Be("Test comments");
        pedidoDto.Price.Should().Be(150.0);
        pedidoDto.PayStatus.Should().Be("PENDING_PAYMENT");
    }

    [Test]
    public async Task GetAdvancedDataAsync_WithPedidosAndLineas_ReturnsBothCorrectly()
    {
        var tournamentId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var assignedTo = Ulid.NewUlid();

        var pedido = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = playerId,
            AssignedTo = assignedTo,
            Machine = "Machine1",
            Comments = "Test",
            Price = 100.0,
            PayStatus = PaymentStatus.PAID
        };
        _pedidosContext.Pedidos.Add(pedido);
        await _pedidosContext.SaveChangesAsync();

        var stringSetup = new StringSetup
        {
            StringV = "Babolat",
            TensionV = 50,
            PreStetchV = 2,
            StringH = "Wilson",
            TensionH = 48,
            PreStetchH = (short)1.5
        };

        var linea = new PedidoLinea
        {
            PedidoId = pedido.Id,
            RaquetModel = "Pro Staff",
            Nudos = 16,
            DateString = DateTime.UtcNow,
            Logotype = true,
            Color = "Red",
            StringSetup = stringSetup,
            Status = Status.COMPLETED
        };
        _pedidosContext.PedidoLineas.Add(linea);
        await _pedidosContext.SaveChangesAsync();

        var result = await _repository.GetAdvancedDataAsync(tournamentId, new List<string> { "pedidos" });

        result.Pedidos.Should().ContainSingle();
        result.PedidoLineas.Should().ContainSingle();

        var lineaDto = result.PedidoLineas.First();
        lineaDto.RaquetModel.Should().Be("Pro Staff");
        lineaDto.Nudos.Should().Be(16);
        lineaDto.StringV.Should().Be("Babolat");
        lineaDto.TensionV.Should().Be(50);
        lineaDto.PreStetchV.Should().Be(2);
        lineaDto.StringH.Should().Be("Wilson");
        lineaDto.TensionH.Should().Be(48);
        lineaDto.PreStetchH.Should().Be((short)1.5);
        lineaDto.Status.Should().Be("COMPLETED");
    }

    [Test]
    public async Task GetAdvancedDataAsync_WithPedidoLineasNullStringSetup_ReturnsSafeDefaults()
    {
        var tournamentId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var assignedTo = Ulid.NewUlid();

        var pedido = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = playerId,
            AssignedTo = assignedTo,
            Machine = "Machine1",
            Price = 100.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT
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
            StringSetup = null!,
            Status = Status.IN_PROGRESS
        };
        _pedidosContext.PedidoLineas.Add(linea);
        await _pedidosContext.SaveChangesAsync();

        var result = await _repository.GetAdvancedDataAsync(tournamentId, new List<string> { "pedidos" });

        result.PedidoLineas.Should().ContainSingle();
        var lineaDto = result.PedidoLineas.First();
        lineaDto.StringV.Should().Be("");
        lineaDto.TensionV.Should().Be(0);
        lineaDto.PreStetchV.Should().Be(0);
        lineaDto.StringH.Should().Be("");
        lineaDto.TensionH.Should().Be(0);
        lineaDto.PreStetchH.Should().Be(0);
    }

    [Test]
    public async Task GetAdvancedDataAsync_WithAllTypes_ReturnsAllData()
    {
        var tournamentId = Ulid.NewUlid();
        var ownerId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();

        // Add user
        var user = new User
        {
            Id = userId,
            Username = "user1",
            Email = "user1@test.com",
            PasswordHash = "hash",
            TournamentId = tournamentId,
            IsDeleted = false
        };
        _userContext.Users.Add(user);
        await _userContext.SaveChangesAsync();

        // Add material
        var material = new Material
        {
            TournamentId = tournamentId,
            Marca = "Wilson",
            Modelo = "Pro",
            Stock = 5,
            Precio = 100,
            Type = MaterialType.Grip,
            IsDeleted = false
        };
        _materialsContext.Materiales.Add(material);
        await _materialsContext.SaveChangesAsync();

        // Add cuerda
        var cuerda = new Cuerda
        {
            TournamentId = tournamentId,
            Marca = "Babolat",
            Modelo = "Hybrid",
            Stock = 10,
            Precio = 15,
            StringFormat = StringFormat.Reel,
            StringsType = StringsType.Hybrid,
            IsDeleted = false
        };
        _materialsContext.Cuerdas.Add(cuerda);
        await _materialsContext.SaveChangesAsync();

        // Add tournament
        var tournament = new Tournaments
        {
            Id = tournamentId,
            Owner = ownerId,
            Title = "Complete Tournament",
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(7),
            SupervisorList = new List<Ulid> { userId },
            IsDeleted = false
        };
        _talleresContext.Partidos.Add(tournament);
        await _talleresContext.SaveChangesAsync();

        // Add pedido
        var pedido = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = playerId,
            AssignedTo = ownerId,
            Price = 100,
            PayStatus = PaymentStatus.PAID
        };
        _pedidosContext.Pedidos.Add(pedido);
        await _pedidosContext.SaveChangesAsync();

        var result = await _repository.GetAdvancedDataAsync(tournamentId,
            new List<string> { "users", "materials", "cuerdas", "tournament", "pedidos" });

        result.Users.Should().ContainSingle();
        result.Materials.Should().ContainSingle();
        result.Cuerdas.Should().ContainSingle();
        result.Tournament.Should().ContainSingle();
        result.Pedidos.Should().ContainSingle();
    }

    [Test]
    public async Task GetAdvancedDataAsync_WithSpecificTypes_OnlyReturnsRequested()
    {
        var tournamentId = Ulid.NewUlid();
        var ownerId = Ulid.NewUlid();

        // Add material
        var material = new Material
        {
            TournamentId = tournamentId,
            Marca = "Wilson",
            Modelo = "Pro",
            Stock = 5,
            Precio = 100,
            Type = MaterialType.Grip,
            IsDeleted = false
        };
        _materialsContext.Materiales.Add(material);

        // Add cuerda
        var cuerda = new Cuerda
        {
            TournamentId = tournamentId,
            Marca = "Babolat",
            Modelo = "Hybrid",
            Stock = 10,
            Precio = 15,
            StringFormat = StringFormat.Reel,
            StringsType = StringsType.Hybrid,
            IsDeleted = false
        };
        _materialsContext.Cuerdas.Add(cuerda);
        await _materialsContext.SaveChangesAsync();

        var result = await _repository.GetAdvancedDataAsync(tournamentId, new List<string> { "materials" });

        result.Materials.Should().ContainSingle();
        result.Cuerdas.Should().BeEmpty();
        result.Users.Should().BeEmpty();
        result.Tournament.Should().BeEmpty();
        result.Pedidos.Should().BeEmpty();
    }

    [Test]
    public async Task GetAdvancedDataAsync_WithNonExistentTournamentInTournamentType_ReturnsEmptyTournamentList()
    {
        var tournamentId = Ulid.NewUlid();

        var result = await _repository.GetAdvancedDataAsync(tournamentId, new List<string> { "tournament" });

        result.Tournament.Should().BeEmpty();
    }

    [Test]
    public async Task GetAdvancedDataAsync_WithMultiplePedidos_ReturnsAll()
    {
        var tournamentId = Ulid.NewUlid();
        var player1 = Ulid.NewUlid();
        var player2 = Ulid.NewUlid();

        var pedido1 = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = player1,
            AssignedTo = Ulid.NewUlid(),
            Price = 50,
            PayStatus = PaymentStatus.PENDING_PAYMENT
        };
        var pedido2 = new Pedidos
        {
            TournamentId = tournamentId,
            PlayerId = player2,
            AssignedTo = Ulid.NewUlid(),
            Price = 75,
            PayStatus = PaymentStatus.PAID
        };
        _pedidosContext.Pedidos.AddRange(pedido1, pedido2);
        await _pedidosContext.SaveChangesAsync();

        var result = await _repository.GetAdvancedDataAsync(tournamentId, new List<string> { "pedidos" });

        result.Pedidos.Should().HaveCount(2);
        result.Pedidos.Select(p => p.Price).Should().Contain(new[] { 50.0, 75.0 });
    }

    #endregion
}
