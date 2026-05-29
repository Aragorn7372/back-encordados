using BackEncordados.Common.Database.Config;
using BackEncordados.Common.Errors;
using BackEncordados.Excel.Archive;
using BackEncordados.Excel.Dto;
using BackEncordados.Excel.Repository;
using BackEncordados.Excel.Service;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Model;
using BackEncordados.Materials.Service.Cuerdas;
using BackEncordados.Materials.Service.Materials;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using BackEncordados.Purchased.Service;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Error;
using BackEncordados.Talleres.Model;
using BackEncordados.Talleres.Service;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Service.CrudService;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TestEncordados.Unit.Excel.Service;

public class ExcelServiceTests
{
    private Mock<IExcelRepository> _mockRepo = null!;
    private Mock<IExcelArchiveManager> _mockArchive = null!;
    private Mock<IUserService> _mockUserService = null!;
    private Mock<IPurchasedService> _mockPurchasedService = null!;
    private Mock<ITournamentService> _mockTournamentService = null!;
    private Mock<IMaterialsService> _mockMaterialsService = null!;
    private Mock<ICuerdasService> _mockCuerdasService = null!;
    private ExcelService _service = null!;

    private static readonly Ulid UserId = Ulid.NewUlid();
    private static readonly Ulid TournamentId = Ulid.NewUlid();
    private const string TournamentName = "Test Tournament";
    private static readonly byte[] SampleExcelBytes = [0x50, 0x4B, 0x03, 0x04];
    private static readonly Pedidos SamplePedido = new()
    {
        TournamentId = TournamentId,
        PlayerId = UserId,
        AssignedTo = Ulid.NewUlid(),
        Price = 50.0,
        Machine = "Machine1"
    };
    private static readonly List<Pedidos> SamplePedidos = [SamplePedido];
    private static readonly Dictionary<Ulid, (string Username, string Name)> SampleUsersLookup = new()
    {
        [UserId] = ("player1", "Player One")
    };
    private static readonly Tournaments SampleTournament = new()
    {
        Id = TournamentId,
        Owner = UserId,
        Title = TournamentName,
        StartTournament = DateTime.UtcNow,
        EndTournament = DateTime.UtcNow.AddDays(7)
    };

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IExcelRepository>();
        _mockArchive = new Mock<IExcelArchiveManager>();
        _mockUserService = new Mock<IUserService>();
        _mockPurchasedService = new Mock<IPurchasedService>();
        _mockTournamentService = new Mock<ITournamentService>();
        _mockMaterialsService = new Mock<IMaterialsService>();
        _mockCuerdasService = new Mock<ICuerdasService>();

        _service = new ExcelService(
            _mockRepo.Object,
            _mockArchive.Object,
            _mockUserService.Object,
            _mockTournamentService.Object,
            _mockMaterialsService.Object,
            _mockCuerdasService.Object,
            _mockPurchasedService.Object,
            NullLogger<ExcelService>.Instance
        );
    }

    [TearDown]
    public void TearDown()
    {
    }

    #region ExportTournamentAsync

    [Test]
    public async Task ExportTournamentAsync_WhenSupervisor_ReturnsExcelBytes()
    {
        _mockRepo.Setup(r => r.IsUserSupervisorOfTournamentAsync(UserId, TournamentId))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.GetPedidosByTournamentAsync(TournamentId))
            .ReturnsAsync(SamplePedidos);
        _mockRepo.Setup(r => r.GetUsersByIdsAsync(It.IsAny<List<Ulid>>()))
            .ReturnsAsync(SampleUsersLookup);
        _mockRepo.Setup(r => r.GetTournamentByIdAsync(TournamentId))
            .ReturnsAsync(SampleTournament);
        _mockArchive.Setup(a => a.CreateExcelAsync(It.IsAny<IEnumerable<TournamentExcelRowDto>>(), TournamentName))
            .ReturnsAsync(SampleExcelBytes);

        var result = await _service.ExportTournamentAsync(UserId, TournamentId);

        result.Should().BeSameAs(SampleExcelBytes);
    }

    [Test]
    public async Task ExportTournamentAsync_WhenNotSupervisor_ThrowsUnauthorizedAccessException()
    {
        _mockRepo.Setup(r => r.IsUserSupervisorOfTournamentAsync(UserId, TournamentId))
            .ReturnsAsync(false);

        var act = () => _service.ExportTournamentAsync(UserId, TournamentId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("No tienes permisos para exportar este torneo");
    }

    [Test]
    public async Task ExportTournamentAsync_WhenTournamentNotInDb_UsesFallbackName()
    {
        var unknownId = Ulid.NewUlid();
        _mockRepo.Setup(r => r.IsUserSupervisorOfTournamentAsync(UserId, unknownId))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.GetPedidosByTournamentAsync(unknownId))
            .ReturnsAsync(new List<Pedidos>());
        _mockRepo.Setup(r => r.GetUsersByIdsAsync(It.IsAny<List<Ulid>>()))
            .ReturnsAsync(new Dictionary<Ulid, (string, string)>());
        _mockRepo.Setup(r => r.GetTournamentByIdAsync(unknownId))
            .ReturnsAsync((Tournaments?)null);
        _mockArchive.Setup(a => a.CreateExcelAsync(It.IsAny<IEnumerable<TournamentExcelRowDto>>(), $"Torneo {unknownId}"))
            .ReturnsAsync(SampleExcelBytes);

        var result = await _service.ExportTournamentAsync(UserId, unknownId);

        result.Should().BeSameAs(SampleExcelBytes);
    }

    #endregion

    #region ExportAdvancedAsync

    [Test]
    public async Task ExportAdvancedAsync_WhenAdmin_BypassesOwnerCheck()
    {
        _mockRepo.Setup(r => r.GetUsersByTournamentAsync(TournamentId))
            .ReturnsAsync(new List<User>());
        _mockRepo.Setup(r => r.GetMaterialsByTournamentAsync(TournamentId))
            .ReturnsAsync(new List<Material>());
        _mockRepo.Setup(r => r.GetCuerdasByTournamentAsync(TournamentId))
            .ReturnsAsync(new List<Cuerdas>());
        _mockRepo.Setup(r => r.GetTournamentByIdAsync(TournamentId))
            .ReturnsAsync(SampleTournament);
        _mockRepo.Setup(r => r.GetPedidosByTournamentAsync(TournamentId))
            .ReturnsAsync(new List<Pedidos>());
        _mockRepo.Setup(r => r.GetPedidoLineasByPedidoIdsAsync(It.IsAny<List<Ulid>>()))
            .ReturnsAsync(new List<PedidoLinea>());
        _mockArchive.Setup(a => a.CreateAdvancedExcelAsync(
                It.IsAny<ExcelAdvancedDataDto>(), It.IsAny<List<string>>(), TournamentName))
            .ReturnsAsync(SampleExcelBytes);

        var result = await _service.ExportAdvancedAsync(UserId, TournamentId, [], "ADMIN");

        result.Should().BeSameAs(SampleExcelBytes);
        _mockRepo.Verify(r => r.IsUserOwnerOfTournamentAsync(It.IsAny<Ulid>(), It.IsAny<Ulid>()), Times.Never);
    }

    [Test]
    public async Task ExportAdvancedAsync_WhenOwner_ReturnsExcelBytes()
    {
        _mockRepo.Setup(r => r.IsUserOwnerOfTournamentAsync(UserId, TournamentId))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.GetUsersByTournamentAsync(TournamentId))
            .ReturnsAsync(new List<User>());
        _mockRepo.Setup(r => r.GetMaterialsByTournamentAsync(TournamentId))
            .ReturnsAsync(new List<Material>());
        _mockRepo.Setup(r => r.GetCuerdasByTournamentAsync(TournamentId))
            .ReturnsAsync(new List<Cuerdas>());
        _mockRepo.Setup(r => r.GetTournamentByIdAsync(TournamentId))
            .ReturnsAsync(SampleTournament);
        _mockRepo.Setup(r => r.GetPedidosByTournamentAsync(TournamentId))
            .ReturnsAsync(new List<Pedidos>());
        _mockRepo.Setup(r => r.GetPedidoLineasByPedidoIdsAsync(It.IsAny<List<Ulid>>()))
            .ReturnsAsync(new List<PedidoLinea>());
        _mockArchive.Setup(a => a.CreateAdvancedExcelAsync(
                It.IsAny<ExcelAdvancedDataDto>(), It.IsAny<List<string>>(), TournamentName))
            .ReturnsAsync(SampleExcelBytes);

        var result = await _service.ExportAdvancedAsync(UserId, TournamentId, [], "OWNER");

        result.Should().BeSameAs(SampleExcelBytes);
    }

    [Test]
    public async Task ExportAdvancedAsync_WhenNotAuthorized_ThrowsUnauthorizedAccessException()
    {
        var act = () => _service.ExportAdvancedAsync(UserId, TournamentId, [], "USER");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("No tienes permisos para exportar este torneo");
    }

    [Test]
    public async Task ExportAdvancedAsync_WithInvalidTypes_FiltersToValidOnes()
    {
        _mockRepo.Setup(r => r.IsUserOwnerOfTournamentAsync(UserId, TournamentId))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.GetUsersByTournamentAsync(TournamentId))
            .ReturnsAsync(new List<User>());
        _mockRepo.Setup(r => r.GetPedidosByTournamentAsync(TournamentId))
            .ReturnsAsync(new List<Pedidos>());
        _mockRepo.Setup(r => r.GetPedidoLineasByPedidoIdsAsync(It.IsAny<List<Ulid>>()))
            .ReturnsAsync(new List<PedidoLinea>());
        _mockRepo.Setup(r => r.GetTournamentByIdAsync(TournamentId))
            .ReturnsAsync(SampleTournament);
        _mockArchive.Setup(a => a.CreateAdvancedExcelAsync(
                It.IsAny<ExcelAdvancedDataDto>(),
                It.Is<List<string>>(types => types.Count == 2 && types.Contains("users") && types.Contains("pedidos")),
                TournamentName))
            .ReturnsAsync(SampleExcelBytes);

        await _service.ExportAdvancedAsync(UserId, TournamentId, ["users", "invalid_type", "pedidos"], "OWNER");

        _mockRepo.Verify(r => r.GetUsersByTournamentAsync(TournamentId), Times.Once);
        _mockRepo.Verify(r => r.GetPedidosByTournamentAsync(TournamentId), Times.Once);
        _mockRepo.Verify(r => r.GetMaterialsByTournamentAsync(It.IsAny<Ulid>()), Times.Never);
        _mockRepo.Verify(r => r.GetCuerdasByTournamentAsync(It.IsAny<Ulid>()), Times.Never);
    }

    #endregion

    #region ImportAsync

    [Test]
    public async Task ImportAsync_WhenAdmin_ImportsAllTypes()
    {
        var data = new ExcelAdvancedDataDto
        {
            Users = [new ExcelUsersDto { Username = "newuser", Name = "New", Email = "new@test.com" }]
        };
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(data);
        _mockUserService.Setup(u => u.CreateContacto(It.IsAny<ContactoPostRequestDto>()))
            .ReturnsAsync(default(Result<UserResponseDto, global::BackEncordados.Usuarios.Errors.AuthError>));

        var result = await _service.ImportAsync(UserId, TournamentId, ["users"], "ADMIN", Stream.Null);

        result.UsersCreated.Should().Be(1);
        _mockUserService.Verify(u => u.CreateContacto(It.Is<ContactoPostRequestDto>(
            c => c.Name == "New" && c.TournamentId == TournamentId)), Times.Once);
    }

    [Test]
    public async Task ImportAsync_WhenNotAuthorized_ThrowsUnauthorizedAccessException()
    {
        var act = () => _service.ImportAsync(UserId, TournamentId, ["users"], "USER", Stream.Null);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("No tienes permisos para importar a este torneo");
    }

    [Test]
    public async Task ImportAsync_WithSpecificTypes_OnlyImportsThose()
    {
        var data = new ExcelAdvancedDataDto
        {
            Users = [new ExcelUsersDto { Username = "u1" }],
            Pedidos = [new ExcelPedidosDto { Machine = "M1" }]
        };
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(data);
        _mockUserService.Setup(u => u.CreateContacto(It.IsAny<ContactoPostRequestDto>()))
            .ReturnsAsync(default(Result<UserResponseDto, global::BackEncordados.Usuarios.Errors.AuthError>));

        await _service.ImportAsync(UserId, TournamentId, ["cuerdas"], "ADMIN", Stream.Null);

        _mockUserService.Verify(u => u.CreateContacto(It.IsAny<ContactoPostRequestDto>()), Times.Never);
    }

    [Test]
    public async Task ImportAsync_WhenServiceThrows_AddsToErrors()
    {
        var data = new ExcelAdvancedDataDto
        {
            Users = [new ExcelUsersDto { Username = "failuser" }]
        };
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(data);
        _mockUserService.Setup(u => u.CreateContacto(It.IsAny<ContactoPostRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("DB failure"));

        var result = await _service.ImportAsync(UserId, TournamentId, ["users"], "ADMIN", Stream.Null);

        result.UsersCreated.Should().Be(0);
        result.Errors.Should().Contain(e => e.Contains("failuser") && e.Contains("DB failure"));
    }

    [Test]
    public async Task ImportAsync_WithPedidosWithoutId_CreatesPedido()
    {
        var data = new ExcelAdvancedDataDto
        {
            Pedidos = [new ExcelPedidosDto { Id = "", PlayerId = "Player1", Machine = "Alpha" }],
            PedidoLineas = [new ExcelPedidoLineasDto { PedidoId = "", RaquetModel = "Pro Staff" }]
        };
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(data);
        _mockPurchasedService.Setup(p => p.CreatePurchasedAsync(It.IsAny<PurchasedRequestDto>()))
            .ReturnsAsync(default(Result<PurchasedResponseDto, DomainErrors>));

        var result = await _service.ImportAsync(UserId, TournamentId, ["pedidos"], "ADMIN", Stream.Null);

        result.PedidosCreated.Should().Be(1);
        _mockPurchasedService.Verify(p => p.CreatePurchasedAsync(It.Is<PurchasedRequestDto>(
            r => r.Machine == "Alpha" && r.TournamentId == TournamentId)), Times.Once);
    }

    #endregion

    #region ImportAsync - additional coverage

    [Test]
    public async Task ImportAsync_WhenOwner_ImportsAllTypes()
    {
        _mockRepo.Setup(r => r.IsUserOwnerOfTournamentAsync(UserId, TournamentId))
            .ReturnsAsync(true);
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Users = [new ExcelUsersDto { Username = "newuser", Name = "New", Email = "new@test.com" }]
        });
        _mockUserService.Setup(u => u.CreateContacto(It.IsAny<ContactoPostRequestDto>()))
            .ReturnsAsync(default(Result<UserResponseDto, AuthError>));

        var result = await _service.ImportAsync(UserId, TournamentId, ["users"], "OWNER", Stream.Null);

        result.UsersCreated.Should().Be(1);
        _mockUserService.Verify(u => u.CreateContacto(It.IsAny<ContactoPostRequestDto>()), Times.Once);
    }

    [Test]
    public async Task ImportAsync_WithUsers_UpdatesExistingUser()
    {
        var existingUserId = Ulid.NewUlid();
        var data = new ExcelAdvancedDataDto
        {
            Users = [new ExcelUsersDto { Id = existingUserId.ToString(), Username = "existing", Name = "Updated" }]
        };
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(data);

        var result = await _service.ImportAsync(UserId, TournamentId, ["users"], "ADMIN", Stream.Null);

        result.UsersUpdated.Should().Be(1);
        _mockUserService.Verify(u => u.PatchUserAsync(existingUserId,
            It.Is<UserRequestDto>(r => r.Name == "Updated")), Times.Once);
    }

    [Test]
    public async Task ImportAsync_WithMaterials_CreatesMaterial()
    {
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Materials = [new ExcelMaterialsDto { Marca = "BrandA", Modelo = "ModelA", Stock = 5, Precio = 10.99 }]
        });
        _mockMaterialsService.Setup(m => m.CreateAsync(It.IsAny<MaterialRequestDto>()))
            .ReturnsAsync(default(Result<MaterialResponseDto, MaterialError>));

        var result = await _service.ImportAsync(UserId, TournamentId, ["materials"], "ADMIN", Stream.Null);

        result.MaterialsCreated.Should().Be(1);
        _mockMaterialsService.Verify(m => m.CreateAsync(It.Is<MaterialRequestDto>(
            r => r.Marca == "BrandA" && r.Modelo == "ModelA" && r.TournamentId == TournamentId)), Times.Once);
    }

    [Test]
    public async Task ImportAsync_WithMaterials_UpdatesMaterial()
    {
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Materials = [new ExcelMaterialsDto { Id = 42, Marca = "BrandB", Modelo = "ModelB" }]
        });
        _mockMaterialsService.Setup(m => m.UpdateAsync(42, It.IsAny<MaterialPatchDto>()))
            .ReturnsAsync(default(Result<MaterialResponseDto, MaterialError>));

        var result = await _service.ImportAsync(UserId, TournamentId, ["materials"], "ADMIN", Stream.Null);

        result.MaterialsUpdated.Should().Be(1);
        _mockMaterialsService.Verify(m => m.UpdateAsync(42, It.Is<MaterialPatchDto>(
            p => p.Marca == "BrandB" && p.Modelo == "ModelB")), Times.Once);
    }

    [Test]
    public async Task ImportAsync_WithMaterials_ErrorLogged()
    {
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Materials = [new ExcelMaterialsDto { Marca = "Fail", Modelo = "Broken" }]
        });
        _mockMaterialsService.Setup(m => m.CreateAsync(It.IsAny<MaterialRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var result = await _service.ImportAsync(UserId, TournamentId, ["materials"], "ADMIN", Stream.Null);

        result.MaterialsCreated.Should().Be(0);
        result.Errors.Should().Contain(e => e.Contains("Fail") && e.Contains("DB error"));
    }

    [Test]
    public async Task ImportAsync_WithCuerdas_CreatesCuerda()
    {
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Cuerdas = [new ExcelCuerdasDto { Marca = "StringX", Modelo = "ModelX", Stock = 10, Precio = 15.50, Calibre = 1.25 }]
        });
        _mockCuerdasService.Setup(c => c.CreateAsync(It.IsAny<CuerdaRequestDto>()))
            .ReturnsAsync(default(Result<CuerdaResponseDto, CuerdaError>));

        var result = await _service.ImportAsync(UserId, TournamentId, ["cuerdas"], "ADMIN", Stream.Null);

        result.CuerdasCreated.Should().Be(1);
        _mockCuerdasService.Verify(c => c.CreateAsync(It.Is<CuerdaRequestDto>(
            r => r.Marca == "StringX" && r.Modelo == "ModelX")), Times.Once);
    }

    [Test]
    public async Task ImportAsync_WithCuerdas_UpdatesCuerda()
    {
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Cuerdas = [new ExcelCuerdasDto { Id = 99, Marca = "StringY", Modelo = "ModelY", Calibre = 1.25 }]
        });
        _mockCuerdasService.Setup(c => c.UpdateAsync(99, It.IsAny<CuerdaPatchDto>()))
            .ReturnsAsync(default(Result<CuerdaResponseDto, CuerdaError>));

        var result = await _service.ImportAsync(UserId, TournamentId, ["cuerdas"], "ADMIN", Stream.Null);

        result.CuerdasUpdated.Should().Be(1);
        _mockCuerdasService.Verify(c => c.UpdateAsync(99, It.Is<CuerdaPatchDto>(
            p => p.Marca == "StringY" && p.Modelo == "ModelY")), Times.Once);
    }

    [Test]
    public async Task ImportAsync_WithCuerdas_Throws_ReturnsError()
    {
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Cuerdas = [new ExcelCuerdasDto { Marca = "FailCuerda", Modelo = "Broken", Calibre = 1.25 }]
        });
        _mockCuerdasService.Setup(c => c.CreateAsync(It.IsAny<CuerdaRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("cuerda error"));

        var result = await _service.ImportAsync(UserId, TournamentId, ["cuerdas"], "ADMIN", Stream.Null);

        result.CuerdasCreated.Should().Be(0);
        result.Errors.Should().Contain(e => e.Contains("FailCuerda") && e.Contains("cuerda error"));
    }

    [Test]
    public async Task ImportAsync_WithTournament_CreatesTournament()
    {
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Tournament = [new ExcelTournamentDto { Title = "New Tournament", StartTournament = DateTime.UtcNow, EndTournament = DateTime.UtcNow.AddDays(3) }]
        });
        _mockTournamentService.Setup(t => t.OwnerCreateTournament(It.IsAny<TournamentRequestDto>(), UserId))
            .ReturnsAsync(default(Result<TournamentResponseDetailsDto, DomainErrors>));

        var result = await _service.ImportAsync(UserId, TournamentId, ["tournament"], "ADMIN", Stream.Null);

        result.TournamentsCreated.Should().Be(1);
        _mockTournamentService.Verify(t => t.OwnerCreateTournament(It.Is<TournamentRequestDto>(
            r => r.Name == "New Tournament"), UserId), Times.Once);
    }

    [Test]
    public async Task ImportAsync_WithTournament_UpdatesTournament()
    {
        var tournamentUlid = Ulid.NewUlid();
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Tournament = [new ExcelTournamentDto { Id = tournamentUlid.ToString(), Title = "Updated Title" }]
        });
        _mockTournamentService.Setup(t => t.UpdateTournament(tournamentUlid, It.IsAny<TournamentPatchDto>()))
            .ReturnsAsync(default(Result<TournamentResponseDto, TournamentsErrors>));

        var result = await _service.ImportAsync(UserId, TournamentId, ["tournament"], "ADMIN", Stream.Null);

        result.TournamentsUpdated.Should().Be(1);
        _mockTournamentService.Verify(t => t.UpdateTournament(tournamentUlid,
            It.Is<TournamentPatchDto>(p => p.Name == "Updated Title")), Times.Once);
    }

    [Test]
    public async Task ImportAsync_WithPedidos_UpdatesPedido()
    {
        var pedidoUlid = Ulid.NewUlid();
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Pedidos = [new ExcelPedidosDto { Id = pedidoUlid.ToString(), Machine = "M2", PlayerId = "Player2" }]
        });
        _mockPurchasedService.Setup(p => p.FindByIdAsync(pedidoUlid))
            .ReturnsAsync(Result.Success<PurchasedResponseDto, DomainErrors>(new PurchasedResponseDto(
                pedidoUlid, TournamentId, null!, null!, "", "", "", DateTime.UtcNow, DateTime.UtcNow, 0, [])));
        _mockPurchasedService.Setup(p => p.UpdatePurchasedAsync(pedidoUlid, It.IsAny<PurchasedPatchDto>()))
            .ReturnsAsync(default(Result<PurchasedResponseDto, DomainErrors>));

        var result = await _service.ImportAsync(UserId, TournamentId, ["pedidos"], "ADMIN", Stream.Null);

        result.PedidosUpdated.Should().Be(1);
        _mockPurchasedService.Verify(p => p.FindByIdAsync(pedidoUlid), Times.Once);
        _mockPurchasedService.Verify(p => p.UpdatePurchasedAsync(pedidoUlid,
            It.Is<PurchasedPatchDto>(dto => dto.Machine == "M2")), Times.Once);
    }

    [Test]
    public async Task ImportAsync_WithPedidos_UpdatesPedidoWithLineas()
    {
        var pedidoUlid = Ulid.NewUlid();
        var lineaUlid = Ulid.NewUlid();
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Pedidos = [new ExcelPedidosDto { Id = pedidoUlid.ToString(), Machine = "M2", PlayerId = "Player2" }],
            PedidoLineas = [new ExcelPedidoLineasDto { Id = lineaUlid.ToString(), PedidoId = pedidoUlid.ToString(), RaquetModel = "Pro Staff" }]
        });
        _mockPurchasedService.Setup(p => p.FindByIdAsync(pedidoUlid))
            .ReturnsAsync(Result.Success<PurchasedResponseDto, DomainErrors>(new PurchasedResponseDto(
                pedidoUlid, TournamentId, null!, null!, "", "", "", DateTime.UtcNow, DateTime.UtcNow, 0, [])));
        _mockPurchasedService.Setup(p => p.UpdatePurchasedAsync(pedidoUlid, It.IsAny<PurchasedPatchDto>()))
            .ReturnsAsync(default(Result<PurchasedResponseDto, DomainErrors>));
        _mockPurchasedService.Setup(p => p.UpdateLineaAsync(lineaUlid, It.IsAny<PedidoLineaPatchDto>()))
            .ReturnsAsync(default(Result<PedidoLineaResponseDto, DomainErrors>));

        var result = await _service.ImportAsync(UserId, TournamentId, ["pedidos"], "ADMIN", Stream.Null);

        result.PedidosUpdated.Should().Be(1);
        result.PedidosLineasUpdated.Should().Be(1);
        _mockPurchasedService.Verify(p => p.UpdateLineaAsync(lineaUlid,
            It.Is<PedidoLineaPatchDto>(dto => dto.RaquetModel == "Pro Staff")), Times.Once);
    }

    [Test]
    public async Task ImportAsync_WithPedidos_WhenFindByIdFails_CreatesPedido()
    {
        var pedidoUlid = Ulid.NewUlid();
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Pedidos = [new ExcelPedidosDto { Id = pedidoUlid.ToString(), Machine = "M3", PlayerId = "Player3" }]
        });
        _mockPurchasedService.Setup(p => p.FindByIdAsync(pedidoUlid))
            .ReturnsAsync(Result.Failure<PurchasedResponseDto, DomainErrors>(new DomainErrors("not found")));
        _mockPurchasedService.Setup(p => p.CreatePurchasedAsync(It.IsAny<PurchasedRequestDto>()))
            .ReturnsAsync(default(Result<PurchasedResponseDto, DomainErrors>));

        var result = await _service.ImportAsync(UserId, TournamentId, ["pedidos"], "ADMIN", Stream.Null);

        result.PedidosCreated.Should().Be(1);
        _mockPurchasedService.Verify(p => p.FindByIdAsync(pedidoUlid), Times.Once);
        _mockPurchasedService.Verify(p => p.UpdatePurchasedAsync(It.IsAny<Ulid>(), It.IsAny<PurchasedPatchDto>()), Times.Never);
        _mockPurchasedService.Verify(p => p.CreatePurchasedAsync(It.Is<PurchasedRequestDto>(
            r => r.Machine == "M3")), Times.Once);
    }

    [Test]
    public async Task ImportAsync_WithPedidos_Throws_ReturnsError()
    {
        var pedidoUlid = Ulid.NewUlid();
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Pedidos = [new ExcelPedidosDto { Id = pedidoUlid.ToString(), Machine = "M4", PlayerId = "Player4" }]
        });
        _mockPurchasedService.Setup(p => p.FindByIdAsync(pedidoUlid))
            .ReturnsAsync(Result.Success<PurchasedResponseDto, DomainErrors>(new PurchasedResponseDto(
                pedidoUlid, TournamentId, null!, null!, "", "", "", DateTime.UtcNow, DateTime.UtcNow, 0, [])));
        _mockPurchasedService.Setup(p => p.UpdatePurchasedAsync(pedidoUlid, It.IsAny<PurchasedPatchDto>()))
            .ThrowsAsync(new InvalidOperationException("update error"));

        var result = await _service.ImportAsync(UserId, TournamentId, ["pedidos"], "ADMIN", Stream.Null);

        result.PedidosUpdated.Should().Be(0);
        result.Errors.Should().Contain(e => e.Contains("update error"));
    }

    [Test]
    public async Task ImportAsync_WithTournament_Throws_ReturnsError()
    {
        _mockArchive.Setup(a => a.ReadExcelAsync(It.IsAny<Stream>())).ReturnsAsync(new ExcelAdvancedDataDto
        {
            Tournament = [new ExcelTournamentDto { Title = "FailTourney" }]
        });
        _mockTournamentService.Setup(t => t.OwnerCreateTournament(It.IsAny<TournamentRequestDto>(), UserId))
            .ThrowsAsync(new InvalidOperationException("create error"));

        var result = await _service.ImportAsync(UserId, TournamentId, ["tournament"], "ADMIN", Stream.Null);

        result.TournamentsCreated.Should().Be(0);
        result.Errors.Should().Contain(e => e.Contains("FailTourney") && e.Contains("create error"));
    }

    #endregion
}
