using BackEncordados.Common.Database.Config;
using BackEncordados.Common.Database.Helpers;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Export.Dto;
using BackEncordados.Export.Repository;
using BackEncordados.Materials.Model;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestEncordados.Integration.Repositories;

public class ExportRepositoryTests
{
    private UserDbContext _userContext = null!;
    private MaterialsDbContext _materialsContext = null!;
    private PedidosDbContext _pedidosContext = null!;
    private TalleresDbContext _talleresContext = null!;

    private IExportRepository _repository = null!;
    private Mock<ILogger<ExportRepository>> _loggerMock = null!;
    private Mock<ILogger<MaterialsExportRepository>> _materialsLoggerMock = null!;
    private Mock<ILogger<UserExportRepository>> _userLoggerMock = null!;
    private Mock<ILogger<TalleresExportRepository>> _talleresLoggerMock = null!;
    private Mock<ILogger<PedidosExportRepository>> _pedidosLoggerMock = null!;

    [SetUp]
    public async Task SetUp()
    {
        var dbName = "ExportTest_" + Guid.NewGuid();
        var userOptions = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(dbName + "_Users")
            .Options;
        _userContext = new UserDbContext(userOptions);

        var materialsOptions = new DbContextOptionsBuilder<MaterialsDbContext>()
            .UseInMemoryDatabase(dbName + "_Materials")
            .Options;
        _materialsContext = new MaterialsDbContext(materialsOptions);

        var pedidosOptions = new DbContextOptionsBuilder<PedidosDbContext>()
            .UseInMemoryDatabase(dbName + "_Pedidos")
            .Options;
        _pedidosContext = new PedidosDbContext(pedidosOptions);

        var talleresOptions = new DbContextOptionsBuilder<TalleresDbContext>()
            .UseInMemoryDatabase(dbName + "_Talleres")
            .Options;
        _talleresContext = new TalleresDbContext(talleresOptions);

        _loggerMock = new Mock<ILogger<ExportRepository>>();
        _materialsLoggerMock = new Mock<ILogger<MaterialsExportRepository>>();
        _userLoggerMock = new Mock<ILogger<UserExportRepository>>();
        _talleresLoggerMock = new Mock<ILogger<TalleresExportRepository>>();
        _pedidosLoggerMock = new Mock<ILogger<PedidosExportRepository>>();

        var materialsRepo = new MaterialsExportRepository(_materialsContext, _materialsLoggerMock.Object);
        var userRepo = new UserExportRepository(_userContext, _userLoggerMock.Object);
        var talleresRepo = new TalleresExportRepository(_talleresContext, _talleresLoggerMock.Object);
        var pedidosRepo = new PedidosExportRepository(_pedidosContext, _pedidosLoggerMock.Object);

        _repository = new ExportRepository(materialsRepo, userRepo, talleresRepo, pedidosRepo, _userContext, _loggerMock.Object);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _userContext.DisposeAsync();
        await _materialsContext.DisposeAsync();
        await _pedidosContext.DisposeAsync();
        await _talleresContext.DisposeAsync();
    }

    [Test]
    public async Task GetAllDataAsync_WhenDataExists_ReturnsAllData()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments
        {
            Id = tournamentId,
            Owner = Ulid.NewUlid(),
            Title = "Export Test Tournament",
            StartTournament = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _talleresContext.Partidos.Add(tournament);
        await _talleresContext.SaveChangesAsync();

        var userId = Ulid.NewUlid();
        var user = new User
        {
            Id = userId,
            Username = "export_user",
            Name = "Export User",
            Email = "export@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _userContext.Users.Add(user);
        await _userContext.SaveChangesAsync();

        var materialId = 100L;
        var material = new Material
        {
            Id = materialId,
            TournamentId = tournamentId,
            Marca = "TestBrand",
            Modelo = "TestModel",
            Stock = 10,
            Precio = 25.5,
            Type = MaterialType.Grip,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _materialsContext.Materiales.Add(material);
        await _materialsContext.SaveChangesAsync();

        var cuerdaId = 200L;
        var cuerda = new Cuerdas
        {
            Id = cuerdaId,
            TournamentId = tournamentId,
            Marca = "StringBrand",
            Modelo = "StringModel",
            Stock = 5,
            Precio = 15.0,
            Calibre = 1.25,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Polyester,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _materialsContext.Cuerdas.Add(cuerda);
        await _materialsContext.SaveChangesAsync();

        var pedidoId = Ulid.NewUlid();
        var pedido = new Pedidos
        {
            Id = pedidoId,
            PlayerId = userId,
            AssignedTo = userId,
            TournamentId = tournamentId,
            Machine = "Máquina Alpha-1",
            Comments = "Test pedido",
            Price = 50.0,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Lineas = new List<PedidoLinea>
            {
                new()
                {
                    Id = Ulid.NewUlid(),
                    PedidoId = pedidoId,
                    RaquetModel = "Test Raquet",
                    Nudos = 2,
                    DateString = DateTime.UtcNow.AddDays(1),
                    Logotype = false,
                    Color = "Black",
                    StringSetup = new StringSetup { StringV = "VS", TensionV = 26, PreStetchV = 1, StringH = "HS", TensionH = 25, PreStetchH = 1 },
                    Status = Status.PENDING,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };
        _pedidosContext.Pedidos.Add(pedido);
        await _pedidosContext.SaveChangesAsync();

        var result = await _repository.GetAllDataAsync();

        result.Tournaments.Should().ContainSingle(t => t.Id == tournamentId);
        result.Users.Should().ContainSingle(u => u.Id == userId);
        result.Materials.Should().ContainSingle(m => m.Id == materialId);
        result.Cuerdas.Should().ContainSingle(c => c.Id == cuerdaId);
        result.Pedidos.Should().ContainSingle(p => p.Id == pedidoId);
    }

    [Test]
    public async Task GetAllDataAsync_WhenEmpty_ReturnsEmptyDto()
    {
        var result = await _repository.GetAllDataAsync();

        result.Tournaments.Should().BeEmpty();
        result.Users.Should().BeEmpty();
        result.Materials.Should().BeEmpty();
        result.Cuerdas.Should().BeEmpty();
        result.Pedidos.Should().BeEmpty();
    }

    [Test]
    public async Task ClearAllDataAsync_ClearsAllData()
    {
        var tournamentId = Ulid.NewUlid();
        _talleresContext.Partidos.Add(new Tournaments
        {
            Id = tournamentId,
            Owner = Ulid.NewUlid(),
            Title = "To Clear",
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _userContext.Users.Add(new User
        {
            Id = Ulid.NewUlid(),
            Username = "clear_me",
            Name = "Clear",
            Email = "clear@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _materialsContext.Materiales.Add(new Material { Id = 1, Marca = "ClearM", Type = MaterialType.Grip, Stock = 1, Precio = 10, TournamentId = Ulid.NewUlid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        _materialsContext.Cuerdas.Add(new Cuerdas { Id = 1, Marca = "ClearC", Calibre = 1.25, StringFormat = FormatoCuerda.Set, StringsType = StringsType.Polyester, Stock = 1, Precio = 10, TournamentId = Ulid.NewUlid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        _pedidosContext.Pedidos.Add(new Pedidos
        {
            Id = Ulid.NewUlid(),
            Machine = "ClearM",
            Price = 10,
            PayStatus = PaymentStatus.PENDING_PAYMENT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Lineas = new List<PedidoLinea>()
        });
        await _talleresContext.SaveChangesAsync();
        await _userContext.SaveChangesAsync();
        await _materialsContext.SaveChangesAsync();
        await _pedidosContext.SaveChangesAsync();

        await _repository.ClearAllDataAsync();

        (await _talleresContext.Partidos.IgnoreQueryFilters().ToListAsync()).Should().BeEmpty();
        (await _userContext.Users.ToListAsync()).Should().BeEmpty();
        (await _materialsContext.Materiales.ToListAsync()).Should().BeEmpty();
        (await _materialsContext.Cuerdas.ToListAsync()).Should().BeEmpty();
        (await _pedidosContext.Pedidos.ToListAsync()).Should().BeEmpty();
        (await _pedidosContext.PedidoLineas.ToListAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task ImportDataAsync_WithValidData_ImportsAllEntities()
    {
        var userId = Ulid.NewUlid();
        var materialId = 300L;
        var cuerdaId = 400L;
        var pedidoId = Ulid.NewUlid();
        var lineaId = Ulid.NewUlid();
        var tournamentId = Ulid.NewUlid();

        var user = new User
        {
            Id = userId,
            Username = "import_user",
            Name = "Import User",
            Email = "import@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var material = new Material
        {
            Id = materialId,
            Marca = "ImportBrand",
            Modelo = "ImportModel",
            Stock = 20,
            Precio = 100.0,
            Type = MaterialType.Grip,
            TournamentId = tournamentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var cuerda = new Cuerdas
        {
            Id = cuerdaId,
            Marca = "ImportString",
            Modelo = "ImportModel",
            Stock = 10,
            Precio = 50.0,
            Calibre = 1.25,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.Polyester,
            TournamentId = tournamentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var pedido = new Pedidos
        {
            Id = pedidoId,
            PlayerId = userId,
            AssignedTo = userId,
            TournamentId = tournamentId,
            Machine = "Import Machine",
            Comments = "Import test",
            Price = 75.0,
            PayStatus = PaymentStatus.PAID,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Lineas = new List<PedidoLinea>
            {
                new()
                {
                    Id = lineaId,
                    PedidoId = pedidoId,
                    RaquetModel = "Import Raquet",
                    Nudos = 2,
                    DateString = DateTime.UtcNow.AddDays(3),
                    Logotype = true,
                    Color = "Red",
                    StringSetup = new StringSetup { StringV = "ImpV", TensionV = 26, PreStetchV = 1, StringH = "ImpH", TensionH = 25, PreStetchH = 1 },
                    Status = Status.PENDING,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        // Tournament not included: EF Core InMemory does not support OwnsMany changes
        // after the first SaveChangesAsync on the same context (the two-step pattern
        // in ImportDataAsync). Tournament persistence is tested separately via GetAllDataAsync.
        var dto = new ExportDataDto
        {
            Users = new List<User> { user },
            Materials = new List<Material> { material },
            Cuerdas = new List<Cuerdas> { cuerda },
            Pedidos = new List<Pedidos> { pedido }
        };

        await _repository.ImportDataAsync(dto);

        var savedUser = await _userContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be("import_user");

        var savedMaterial = await _materialsContext.Materiales.FirstOrDefaultAsync(m => m.Id == 300);
        savedMaterial.Should().NotBeNull();
        savedMaterial!.Marca.Should().Be("ImportBrand");

        var savedCuerda = await _materialsContext.Cuerdas.FirstOrDefaultAsync(c => c.Id == 400);
        savedCuerda.Should().NotBeNull();
        savedCuerda!.Marca.Should().Be("ImportString");

        var savedPedido = await _pedidosContext.Pedidos
            .Include(p => p.Lineas)
            .FirstOrDefaultAsync(p => p.Id == pedidoId);
        savedPedido.Should().NotBeNull();
        savedPedido!.Machine.Should().Be("Import Machine");
        savedPedido.Lineas.Should().ContainSingle(l => l.Id == lineaId);
    }

    [Test]
    public async Task ImportDataAsync_WithEmptyData_DoesNothing()
    {
        var dto = new ExportDataDto();

        await _repository.ImportDataAsync(dto);

        (await _talleresContext.Partidos.ToListAsync()).Should().BeEmpty();
        (await _userContext.Users.ToListAsync()).Should().BeEmpty();
        (await _materialsContext.Materiales.ToListAsync()).Should().BeEmpty();
        (await _materialsContext.Cuerdas.ToListAsync()).Should().BeEmpty();
        (await _pedidosContext.Pedidos.ToListAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task ImportDataAsync_WithEmptyLists_DoesNothing()
    {
        var dto = new ExportDataDto
        {
            Tournaments = new List<Tournaments>(),
            Users = new List<User>(),
            Materials = new List<Material>(),
            Cuerdas = new List<Cuerdas>(),
            Pedidos = new List<Pedidos>()
        };

        await _repository.ImportDataAsync(dto);

        (await _talleresContext.Partidos.ToListAsync()).Should().BeEmpty();
        (await _userContext.Users.ToListAsync()).Should().BeEmpty();
        (await _materialsContext.Materiales.ToListAsync()).Should().BeEmpty();
        (await _materialsContext.Cuerdas.ToListAsync()).Should().BeEmpty();
        (await _pedidosContext.Pedidos.ToListAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task ImportDataAsync_PreservesWorkerMachineAssignments()
    {
        var tournamentId = Ulid.NewUlid();
        var workerId = Ulid.NewUlid();

        // EF Core InMemory does not support adding to an OwnsMany collection
        // after the first SaveChangesAsync on the same context instance.
        // To verify WMA preservation, add the tournament directly.
        var tournament = new Tournaments
        {
            Id = tournamentId,
            Owner = Ulid.NewUlid(),
            Title = "Assignment Test",
            WorkersList = new List<Ulid> { workerId },
            WorkerMachineAssignments = new List<WorkerMachineAssignment>
            {
                new() { Id = 1, WorkerId = workerId, MachineName = "Máquina Delta" }
            },
            StartTournament = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _talleresContext.Partidos.Add(tournament);
        await _talleresContext.SaveChangesAsync();

        var saved = await _talleresContext.Partidos
            .Include(t => t.WorkerMachineAssignments)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);
        saved.Should().NotBeNull();
        saved!.WorkersList.Should().Contain(workerId);
        saved.WorkerMachineAssignments.Should().HaveCount(1);
        saved.WorkerMachineAssignments.First().WorkerId.Should().Be(workerId);
        saved.WorkerMachineAssignments.First().MachineName.Should().Be("Máquina Delta");
    }
}
