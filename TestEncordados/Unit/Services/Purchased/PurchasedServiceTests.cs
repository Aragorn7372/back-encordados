using BackEncordados.Common.Dto;
using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.keys;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Common.Service.Email;
using BackEncordados.Common.Service.WhatsApp;
using BackEncordados.Common.SignalR;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Errors;
using BackEncordados.Purchased.Model;
using BackEncordados.Purchased.Repository;
using BackEncordados.Purchased.Service;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TestEncordados.Unit.Fixtures;
using PurchasedServiceType = BackEncordados.Purchased.Service.PurchasedService;
using IPuchasedRepositoryType = BackEncordados.Purchased.Repository.IPuchasedRepository;
using IUserRepositoryType = BackEncordados.Usuarios.Repository.IUserRepository;
using ICacheServiceType = BackEncordados.Common.Service.Cache.ICacheService;
using ICloudinaryServiceType = BackEncordados.Common.Service.Cloudinary.ICloudinaryService;
using IEmailServiceType = BackEncordados.Common.Service.Email.IEmailService;
using IWhatsAppServiceType = BackEncordados.Common.Service.WhatsApp.IWhatsAppService;

namespace TestEncordados.Unit.Services.Purchased;

/// <summary>
/// Test double for IClientProxy that does nothing, allowing SendAsync to be called safely.
/// </summary>
public class NoOpClientProxy : IClientProxy
{
    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class PurchasedServiceTests
{
    private readonly Mock<IPuchasedRepositoryType> _mockRepo;
    private readonly Mock<IUserRepositoryType> _mockUserRepo;
    private readonly Mock<ICacheServiceType> _mockCache;
    private readonly Mock<ICloudinaryServiceType> _mockCloudinary;
    private readonly Mock<IEmailServiceType> _mockEmail;
    private readonly Mock<IWhatsAppServiceType> _mockWhatsApp;
    private readonly Mock<ILogger<PurchasedServiceType>> _mockLogger;
    private readonly Mock<IHubContext<SignalHub>> _mockSignal;
    private readonly PurchasedServiceType _service;

    public PurchasedServiceTests()
    {
        _mockRepo = new Mock<IPuchasedRepositoryType>();
        _mockUserRepo = new Mock<IUserRepositoryType>();
        _mockCache = CacheServiceBuilder.Create();
        _mockCloudinary = CloudinaryServiceBuilder.Create();
        _mockEmail = new Mock<IEmailServiceType>();
        _mockWhatsApp = new Mock<IWhatsAppServiceType>();
        _mockSignal = new Mock<IHubContext<SignalHub>>();
        _mockLogger = new Mock<ILogger<PurchasedServiceType>>();

        SetupSignalHubMock();

        _service = new PurchasedServiceType(
            _mockRepo.Object,
            _mockUserRepo.Object,
            _mockLogger.Object,
            _mockCache.Object,
            _mockCloudinary.Object,
            _mockEmail.Object,
            _mockWhatsApp.Object,
            _mockSignal.Object);
    }

    [SetUp]
    public void ResetMocks()
    {
        // IMPORTANT: Clear the static cache store FIRST before resetting mocks
        // This ensures a clean slate for each test
        CacheServiceBuilder.Clear();
        
        // Reset all mocks
        _mockRepo.Reset();
        _mockUserRepo.Reset();
        _mockEmail.Reset();
        _mockWhatsApp.Reset();
        _mockLogger.Reset();
        _mockCache.Reset();
    }

    private void SetupSignalHubMock()
    {
        // Use a NoOp test double instead of a mock, since SendAsync is an extension method
        // that can't be mocked with Moq
        var noOpClientProxy = new NoOpClientProxy();

        var mockClients = new Mock<IHubClients>();
        mockClients
            .Setup(c => c.Groups(It.IsAny<IReadOnlyList<string>>()))
            .Returns(noOpClientProxy);

        _mockSignal
            .Setup(s => s.Clients)
            .Returns(mockClients.Object);
    }

    private static FilterPurchasedDto CreateFilter(string search = "") =>
        new(IsEncorder: null, IsUser: null, UserId: null, TournamentId: null, Search: search, Page: 0, Size: 10, SortBy: "createdAt", Direction: "desc");

    private static User CreateUser(string username = "testuser", string role = User.UserRoles.USER, double bonos = 0)
    {
        return new User
        {
            Id = Ulid.NewUlid(),
            Username = username,
            Name = "Test User",
            Email = $"{username}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = role,
            Bonos = bonos,
            IsDeleted = false
        };
    }

    private static PurchasedResponseDto CreatePurchasedResponseDto()
    {
        return new PurchasedResponseDto(
            Id: Ulid.NewUlid(),
            TournamentId: Ulid.NewUlid(),
            Player: new UserResponseDto("player1", "http://img.com/player.png", "Player One", 0),
            Encorder: new UserResponseDto("encorder1", "http://img.com/encorder.png", "Encorder One", 0),
            Machine: "Machine-1",
            Price: 50.0,
            Comments: "Test",
            PayStatus: PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas: new List<PedidoLineaResponseDto>(),
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );
    }

    [Test]
    public async Task FindAllAsync_WithResults_ReturnsPagedResponse()
    {
        var filter = CreateFilter();
        var purchased = PedidosBuilder.Create();
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);

        _mockRepo.Setup(r => r.FindAllAsync(filter)).ReturnsAsync((new List<Pedidos> { purchased }, 1));

        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.AssignedTo)).ReturnsAsync(encorder);

        var result = await _service.FindAllAsync(filter);

        result.Content.Should().HaveCount(1);
        result.TotalElements.Should().Be(1);
    }

    [Test]
    public async Task FindAllAsync_WithMissingPlayer_SkipsItem()
    {
        var filter = CreateFilter();
        var purchased = PedidosBuilder.Create();

        _mockRepo.Setup(r => r.FindAllAsync(filter)).ReturnsAsync((new List<Pedidos> { purchased }, 1));
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync((User?)null);

        var result = await _service.FindAllAsync(filter);

        result.Content.Should().BeEmpty();
    }

    [Test]
    public async Task FindByIdAsync_Cached_ReturnsCached()
    {
        var id = Ulid.NewUlid();
        var cached = CreatePurchasedResponseDto();

        _mockCache.Setup(c => c.GetAsync<PurchasedResponseDto>(CacheKeys.PurchasedCacheKey + id))
            .ReturnsAsync(cached);

        var result = await _service.FindByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(cached.Id);
    }

    [Test]
    public async Task FindByIdAsync_NotCached_FetchesFromRepo()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);

        _mockCache.Setup(c => c.GetAsync<PurchasedResponseDto>(CacheKeys.PurchasedCacheKey + id))
            .ReturnsAsync((PurchasedResponseDto?)null);
        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.AssignedTo)).ReturnsAsync(encorder);

        var result = await _service.FindByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        _mockRepo.Verify(r => r.FindByIdAsync(id), Times.Once);
    }

    [Test]
    public async Task FindByIdAsync_NotFound_ReturnsNotFoundError()
    {
        var id = Ulid.NewUlid();

        _mockCache.Setup(c => c.GetAsync<PurchasedResponseDto>(CacheKeys.PurchasedCacheKey + id))
            .ReturnsAsync((PurchasedResponseDto?)null);
        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((Pedidos?)null);

        var result = await _service.FindByIdAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    [Test]
    public async Task UpdatePurchasedAsync_NotFound_ReturnsNotFoundError()
    {
        var id = Ulid.NewUlid();
        var dto = new PurchasedPatchDto { Machine = "NewMachine" };

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync((Pedidos?)null);

        var result = await _service.UpdatePurchasedAsync(id, dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    [Test]
    public async Task UpdatePurchasedAsync_UpdatesComments_ReturnsSuccess()
    {
        var id = Ulid.NewUlid();
        var existing = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);
        var patch = new PurchasedPatchDto { Comments = "Updated comment" };

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdatePurchasedAsync(It.IsAny<Pedidos>(), id))
            .ReturnsAsync((Pedidos p, Ulid _) => { 
                p.PlayerId = existing.PlayerId; 
                p.AssignedTo = existing.AssignedTo; 
                p.Comments = "Updated comment"; 
                return p; 
            });
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + existing.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + existing.AssignedTo))
            .ReturnsAsync(encorderDto);

        var result = await _service.UpdatePurchasedAsync(id, patch);
        
        result.IsSuccess.Should().BeTrue();
        result.Value.Comments.Should().Be("Updated comment");
    }

    [Test]
    public async Task UpdatePurchasedAsync_UpdatesMachine_ReturnsSuccess()
    {
        var id = Ulid.NewUlid();
        var existing = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);
        var patch = new PurchasedPatchDto { Machine = "Machine-2" };

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdatePurchasedAsync(It.IsAny<Pedidos>(), id))
            .ReturnsAsync((Pedidos p, Ulid _) => { 
                p.PlayerId = existing.PlayerId; 
                p.AssignedTo = existing.AssignedTo; 
                p.Machine = "Machine-2"; 
                return p; 
            });
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + existing.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + existing.AssignedTo))
            .ReturnsAsync(encorderDto);

        var result = await _service.UpdatePurchasedAsync(id, patch);

        result.IsSuccess.Should().BeTrue();
        result.Value.Machine.Should().Be("Machine-2");
    }

    [Test]
    public async Task UpdatePurchasedAsync_ChangesPaymentStatus_ReturnsSuccess()
    {
        var id = Ulid.NewUlid();
        var existing = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);
        var patch = new PurchasedPatchDto { PayStatus = PaymentStatus.PAID.ToString() };

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdatePurchasedAsync(It.IsAny<Pedidos>(), id))
            .ReturnsAsync((Pedidos p, Ulid _) => { 
                p.PlayerId = existing.PlayerId; 
                p.AssignedTo = existing.AssignedTo; 
                p.PayStatus = PaymentStatus.PAID; 
                return p; 
            });
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + existing.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + existing.AssignedTo))
            .ReturnsAsync(encorderDto);

        var result = await _service.UpdatePurchasedAsync(id, patch);

        result.IsSuccess.Should().BeTrue();
        result.Value.PayStatus.Should().Be(PaymentStatus.PAID.ToString());
    }

    [Test]
    public async Task UpdatePurchasedAsync_UpdatesCache_AfterSuccess()
    {
        var id = Ulid.NewUlid();
        var existing = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);
        var patch = new PurchasedPatchDto { Comments = "Updated" };

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdatePurchasedAsync(It.IsAny<Pedidos>(), id))
            .ReturnsAsync((Pedidos p, Ulid _) => { 
                p.PlayerId = existing.PlayerId; 
                p.AssignedTo = existing.AssignedTo; 
                p.Comments = "Updated"; 
                return p; 
            });
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + existing.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + existing.AssignedTo))
            .ReturnsAsync(encorderDto);

        await _service.UpdatePurchasedAsync(id, patch);

        _mockCache.Verify(c => c.SetAsync(
            It.Is<string>(k => k.StartsWith(CacheKeys.PurchasedCacheKey)),
            It.IsAny<PurchasedResponseDto>(),
            It.IsAny<TimeSpan>()), Times.Once);
    }

    [Test]
    public async Task CancelPurchasedAsync_NotFound_ReturnsNotFoundError()
    {
        var id = Ulid.NewUlid();

        _mockRepo.Setup(r => r.CancelPurchasedAsync(id)).ReturnsAsync((Pedidos?)null);

        var result = await _service.CancelPurchasedAsync(id, false, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    [Test]
    public async Task CancelPurchasedAsync_UnauthorizedUser_ReturnsUnauthorizedError()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var otherUserId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.CancelPurchasedAsync(id)).ReturnsAsync(purchased);

        var result = await _service.CancelPurchasedAsync(id, true, otherUserId.ToString());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
    }

    [Test]
    public async Task ChangePaymentStatusPurchasedAsync_InvalidStatus_ReturnsError()
    {
        var id = Ulid.NewUlid();

        var result = await _service.ChangePaymentStatusPurchasedAsync(id, "INVALID_STATUS");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<InvalidStatusError>();
    }

    [Test]
    public async Task ChangePaymentStatusPurchasedAsync_Valid_ReturnsSuccess()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);

        _mockRepo.Setup(r => r.ChangeStatusPurchasedAsync(id, "PAID")).ReturnsAsync((Pedidos?)purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.AssignedTo)).ReturnsAsync(encorder);

        var result = await _service.ChangePaymentStatusPurchasedAsync(id, "PAID");

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UpdateLineaAsync_NotFound_ReturnsNotFoundError()
    {
        var lineaId = Ulid.NewUlid();
        var dto = new PedidoLineaPatchDto { Nudos = 3 };

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync((PedidoLinea?)null);

        var result = await _service.UpdateLineaAsync(lineaId, dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    [Test]
    public async Task CancelLineaAsync_NotFound_ReturnsNotFoundError()
    {
        var lineaId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync((PedidoLinea?)null);

        var result = await _service.CancelLineaAsync(lineaId, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    [Test]
    public async Task ChangeLineaStatusAsync_InvalidStatus_ReturnsError()
    {
        var lineaId = Ulid.NewUlid();

        var result = await _service.ChangeLineaStatusAsync(lineaId, "INVALID_STATUS");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<InvalidStatusError>();
    }

    [Test]
    public async Task ChangeLineaStatusAsync_NotFound_ReturnsNotFoundError()
    {
        var lineaId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync((PedidoLinea?)null);

        var result = await _service.ChangeLineaStatusAsync(lineaId, "COMPLETED");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    [Test]
    public async Task ChangeAllLineasStatusAsync_InvalidStatus_ReturnsError()
    {
        var purchasedId = Ulid.NewUlid();

        var result = await _service.ChangeAllLineasStatusAsync(purchasedId, "INVALID_STATUS");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<InvalidStatusError>();
    }

    [Test]
    public async Task ChangeAllLineasStatusAsync_NotFound_ReturnsNotFoundError()
    {
        var purchasedId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.FindByIdAsync(purchasedId)).ReturnsAsync((Pedidos?)null);

        var result = await _service.ChangeAllLineasStatusAsync(purchasedId, "COMPLETED");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    #region CreatePurchasedAsync Tests

    [Test]
    public async Task CreatePurchasedAsync_PlayerNotFound_ReturnsUserNotFoundError()
    {
        var request = new PurchasedRequestDto
        {
            PlayerName = "nonexistent_player",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = "Test",
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "nonexistent_player")).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync("nonexistent_player")).ReturnsAsync((User?)null);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task CreatePurchasedAsync_EncorderNotFound_ReturnsUserNotFoundError()
    {
        var player = CreateUser("player1");
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "nonexistent_encorder",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = "Test",
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "nonexistent_encorder")).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync("nonexistent_encorder")).ReturnsAsync((User?)null);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task CreatePurchasedAsync_SufficientBonos_SetsPaidStatusAndDeductsBonos()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 100); // 100 bonos
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var price = 50.0;
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = price
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.PayStatus.Should().Be(PaymentStatus.PAID.ToString());
        _mockUserRepo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.Bonos == 50)), Times.Once);
    }

    [Test]
    public async Task CreatePurchasedAsync_InsufficientBonos_SetsPendingAndAddsComment()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 20); // 20 bonos < 50 price
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var price = 50.0;
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = price
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.PayStatus.Should().Be(PaymentStatus.PENDING_PAYMENT.ToString());
        result.Value.Comments.Should().Contain("Falta por pagar: 30");
    }

    [Test]
    public async Task CreatePurchasedAsync_PartialBonos_DeductsAndAddsComment()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 30); // 30 bonos < 50 price
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var price = 50.0;
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = "Original comment",
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = price
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Comments.Should().Contain("Original comment | Falta por pagar: 20");
    }

    [Test]
    public async Task CreatePurchasedAsync_CacheSetsCorrectly()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsSuccess.Should().BeTrue();
        _mockCache.Verify(c => c.SetAsync(
            It.Is<string>(k => k.StartsWith(CacheKeys.PurchasedCacheKey)),
            It.IsAny<PurchasedResponseDto>(),
            It.IsAny<TimeSpan>()), Times.Once);
    }

    [Test]
    public async Task CreatePurchasedAsync_PlayerFromCache_UsesCachedUser()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync("player1")).ReturnsAsync(player);

        await _service.CreatePurchasedAsync(request);

        _mockUserRepo.Verify(r => r.FindByUsernameAsync("player1"), Times.AtLeastOnce);
    }

    [Test]
    public async Task CreatePurchasedAsync_EncorderFromCache_UsesCachedUser()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync("player1")).ReturnsAsync(player);

        await _service.CreatePurchasedAsync(request);

        _mockUserRepo.Verify(r => r.FindByUsernameAsync("encorder1"), Times.Never);
    }

    [Test]
    public async Task CreatePurchasedAsync_RepositoryCalledWithCorrectEntity()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var tournamentId = Ulid.NewUlid();
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = tournamentId,
            Machine = "Machine-1",
            Comments = "Test",
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        await _service.CreatePurchasedAsync(request);

        _mockRepo.Verify(r => r.CreatePurchasedAsync(It.Is<Pedidos>(p =>
            p.PlayerId == player.Id &&
            p.AssignedTo == encorder.Id &&
            p.TournamentId == tournamentId &&
            p.Machine == "Machine-1"
        )), Times.Once);
    }

    [Test]
    public async Task CreatePurchasedAsync_PaidStatus_SendsEmailNotification()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync("player1")).ReturnsAsync(player);

        await _service.CreatePurchasedAsync(request);

        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Once);
    }

    [Test]
    public async Task CreatePurchasedAsync_PendingStatus_NoEmailSent()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 10); // Insufficient bonos
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);

        await _service.CreatePurchasedAsync(request);

        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
    }

    [Test]
    public async Task CreatePurchasedAsync_RetrySucceedsOnFirstAttempt()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsSuccess.Should().BeTrue();
        _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Test]
    public async Task CreatePurchasedAsync_DTOContainsCorrectData()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var tournamentId = Ulid.NewUlid();
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = tournamentId,
            Machine = "Machine-1",
            Comments = "Test comment",
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 150.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _service.CreatePurchasedAsync(request);

        result.Value.TournamentId.Should().Be(tournamentId);
        result.Value.Machine.Should().Be("Machine-1");
        result.Value.Price.Should().Be(150.0);
    }

    #endregion

    #region CancelLineaAsync Additional Tests

    [Test]
    public async Task CancelLineaAsync_UserCanCancelPending_ReturnsSuccess()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Status = Status.PENDING;
        var pedido = PedidosBuilder.Create(id: pedidoId);
        linea.Pedido = pedido;

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.CANCELED)).ReturnsAsync(linea);

        var result = await _service.CancelLineaAsync(lineaId, userId.ToString(), User.UserRoles.USER);

        result.IsSuccess.Should().BeTrue();
        _mockRepo.Verify(r => r.ChangeLineaStatusAsync(lineaId, Status.CANCELED), Times.Once);
    }

    [Test]
    public async Task CancelLineaAsync_UserCannotCancelCompleted_ReturnsError()
    {
        var lineaId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();
        var linea = PedidoLineaBuilder.Create(id: lineaId);
        linea.Status = Status.COMPLETED;

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);

        var result = await _service.CancelLineaAsync(lineaId, userId.ToString(), User.UserRoles.USER);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<InvalidStatusError>();
    }

    [Test]
    public async Task CancelLineaAsync_UserCannotCancelDelivered_ReturnsError()
    {
        var lineaId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();
        var linea = PedidoLineaBuilder.Create(id: lineaId);
        linea.Status = Status.DELIVERED_TOpLAYER;

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);

        var result = await _service.CancelLineaAsync(lineaId, userId.ToString(), User.UserRoles.USER);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<InvalidStatusError>();
    }

    [Test]
    public async Task CancelLineaAsync_AdminCanCancelCompleted_ReturnsSuccess()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Status = Status.COMPLETED;
        var pedido = PedidosBuilder.Create(id: pedidoId);
        linea.Pedido = pedido;

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.CANCELED)).ReturnsAsync(linea);

        var result = await _service.CancelLineaAsync(lineaId, userId.ToString(), User.UserRoles.ADMIN);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CancelLineaAsync_SendsWhatsAppNotification_WhenPhoneExists()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var userId = Ulid.NewUlid();
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Status = Status.PENDING;
        var pedido = PedidosBuilder.Create(id: pedidoId);
        pedido.PlayerId = playerId;
        linea.Pedido = pedido;
        var player = CreateUser("player1", User.UserRoles.USER, 0);
        player.Id = playerId;
        player.Phone = "+34123456789";

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.CANCELED)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync(pedido);
        _mockUserRepo.Setup(r => r.FindByIdAsync(playerId)).ReturnsAsync(player);

        await _service.CancelLineaAsync(lineaId, userId.ToString(), User.UserRoles.ADMIN);

        _mockWhatsApp.Verify(w => w.SendLineaCanceledMessageAsync(
            "+34123456789", "Test User", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Additional Phase 2 Tests

    // ===== FindAllAsync Additional Tests =====
    [Test]
    public async Task FindAllAsync_EmptyResults_ReturnsEmptyContent()
    {
        var filter = CreateFilter();

        _mockRepo.Setup(r => r.FindAllAsync(filter)).ReturnsAsync((new List<Pedidos>(), 0));

        var result = await _service.FindAllAsync(filter);

        result.Content.Should().BeEmpty();
        result.TotalElements.Should().Be(0);
    }

    [Test]
    public async Task FindAllAsync_WithMissingEncorder_SkipsItem()
    {
        var filter = CreateFilter();
        var purchased = PedidosBuilder.Create();

        _mockRepo.Setup(r => r.FindAllAsync(filter)).ReturnsAsync((new List<Pedidos> { purchased }, 1));
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync(player);
        
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.AssignedTo)).ReturnsAsync((User?)null);

        var result = await _service.FindAllAsync(filter);

        result.Content.Should().BeEmpty();
    }

    [Test]
    public async Task FindAllAsync_MultipleResults_ReturnsPaged()
    {
        var filter = CreateFilter();
        var purchased1 = PedidosBuilder.Create();
        var purchased2 = PedidosBuilder.Create();
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);

        _mockRepo.Setup(r => r.FindAllAsync(filter)).ReturnsAsync((new List<Pedidos> { purchased1, purchased2 }, 2));

        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + It.IsAny<Ulid>()))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(It.IsAny<Ulid>()))
            .ReturnsAsync((Ulid id) => id == purchased1.PlayerId || id == purchased2.PlayerId ? player : encorder);

        var result = await _service.FindAllAsync(filter);

        result.Content.Should().HaveCount(2);
        result.TotalElements.Should().Be(2);
    }

    [Test]
    public async Task FindAllAsync_UsesCache_ForUserData()
    {
        var filter = CreateFilter();
        var purchased = PedidosBuilder.Create();
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);

        _mockRepo.Setup(r => r.FindAllAsync(filter)).ReturnsAsync((new List<Pedidos> { purchased }, 1));
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync(encorderDto);

        var result = await _service.FindAllAsync(filter);

        result.Content.Should().HaveCount(1);
        _mockUserRepo.Verify(r => r.FindByIdAsync(It.IsAny<Ulid>()), Times.Never);
    }

    // ===== CancelPurchasedAsync Additional Tests =====
    [Test]
    public async Task CancelPurchasedAsync_ValidCancel_ReturnsSuccess()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);

        _mockRepo.Setup(r => r.CancelPurchasedAsync(id)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync(encorderDto);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(player.Username)).ReturnsAsync(player);

        var result = await _service.CancelPurchasedAsync(id, false, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CancelPurchasedAsync_UserOwner_CanCancel()
    {
        var id = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        purchased.PlayerId = playerId;
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);

        _mockRepo.Setup(r => r.CancelPurchasedAsync(id)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync(encorderDto);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(player.Username)).ReturnsAsync(player);

        var result = await _service.CancelPurchasedAsync(id, true, playerId.ToString());

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CancelPurchasedAsync_UserNotOwner_CannotCancel()
    {
        var id = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var otherUserId = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        purchased.PlayerId = playerId;

        _mockRepo.Setup(r => r.CancelPurchasedAsync(id)).ReturnsAsync(purchased);

        var result = await _service.CancelPurchasedAsync(id, true, otherUserId.ToString());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
    }

    [Test]
    public async Task CancelPurchasedAsync_ClearsCacheAfterCancel()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);

        _mockRepo.Setup(r => r.CancelPurchasedAsync(id)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync(encorderDto);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(player.Username)).ReturnsAsync(player);

        await _service.CancelPurchasedAsync(id, false, null);

        _mockCache.Verify(c => c.SetAsync(
            It.Is<string>(k => k.StartsWith(CacheKeys.PurchasedCacheKey)),
            It.IsAny<PurchasedResponseDto>(),
            It.IsAny<TimeSpan>()), Times.Once);
    }

    [Test]
    public async Task CancelPurchasedAsync_SendsCancelEmailAsync()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        player.Email = "player1@test.com";
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);

        _mockRepo.Setup(r => r.CancelPurchasedAsync(id)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync(encorderDto);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(player.Username)).ReturnsAsync(player);

        await _service.CancelPurchasedAsync(id, false, null);

        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Once);
    }

    // ===== ChangePaymentStatusPurchasedAsync Additional Tests =====
    [Test]
    public async Task ChangePaymentStatusPurchasedAsync_ToPaid_SendsEmailNotification()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id, payStatus: PaymentStatus.PAID);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        player.Email = "player1@test.com";
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);

        _mockRepo.Setup(r => r.ChangeStatusPurchasedAsync(id, "PAID")).ReturnsAsync((Pedidos?)purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.AssignedTo)).ReturnsAsync(encorder);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync("player1")).ReturnsAsync(player);

        var result = await _service.ChangePaymentStatusPurchasedAsync(id, "PAID");

        result.IsSuccess.Should().BeTrue();
        result.Value.PayStatus.Should().Be(PaymentStatus.PAID.ToString());
        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.Is<EmailMessage>(m => m.To == player.Email && m.Subject == "Pago confirmado")), Times.Once);
    }

    [Test]
    public async Task ChangePaymentStatusPurchasedAsync_ToPending_ReturnsSuccess()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);

        _mockRepo.Setup(r => r.ChangeStatusPurchasedAsync(id, PaymentStatus.PENDING_PAYMENT.ToString()))
            .ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync(encorderDto);

        var result = await _service.ChangePaymentStatusPurchasedAsync(id, PaymentStatus.PENDING_PAYMENT.ToString());

        result.IsSuccess.Should().BeTrue();
    }

    // ===== UpdateLineaAsync Additional Tests =====
    [Test]
    public async Task UpdateLineaAsync_UpdatesLinea_ReturnsSuccess()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var pedido = PedidosBuilder.Create(id: pedidoId);
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Pedido = pedido;
        var patch = new PedidoLineaPatchDto { Nudos = 50 };

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.UpdateLineaAsync(It.IsAny<PedidoLinea>(), lineaId))
            .ReturnsAsync((PedidoLinea p, Ulid _) => { 
                p.Nudos = 50; 
                p.Pedido = pedido; 
                return p; 
            });

        var result = await _service.UpdateLineaAsync(lineaId, patch);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nudos.Should().Be(50);
    }

    [Test]
    public async Task UpdateLineaAsync_NotFound_ReturnsError()
    {
        var lineaId = Ulid.NewUlid();
        var patch = new PedidoLineaPatchDto { Nudos = 50 };

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync((PedidoLinea?)null);

        var result = await _service.UpdateLineaAsync(lineaId, patch);

        result.IsFailure.Should().BeTrue();
    }

    // ===== ChangeLineaStatusAsync Additional Tests =====
    [Test]
    public async Task ChangeLineaStatusAsync_ToCompleted_ReturnsSuccess()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var pedido = PedidosBuilder.Create(id: pedidoId, playerId: playerId);
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Pedido = pedido;

        var player = CreateUser("player1", User.UserRoles.USER, 0);
        player.Id = playerId;
        player.Email = "player1@test.com";

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.COMPLETED))
            .ReturnsAsync((Ulid _, Status _) => { 
                linea.Status = Status.COMPLETED; 
                return linea; 
            });
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync(pedido);
        _mockUserRepo.Setup(r => r.FindByIdAsync(playerId)).ReturnsAsync(player);

        var result = await _service.ChangeLineaStatusAsync(lineaId, Status.COMPLETED.ToString());

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(Status.COMPLETED);
    }

    [Test]
    public async Task ChangeLineaStatusAsync_ToDelivered_ReturnsSuccess()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var pedido = PedidosBuilder.Create(id: pedidoId, playerId: playerId);
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Pedido = pedido;

        var player = CreateUser("player1", User.UserRoles.USER, 0);
        player.Id = playerId;
        player.Email = "player1@test.com";

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.DELIVERED_TOpLAYER))
            .ReturnsAsync((Ulid _, Status _) => { 
                linea.Status = Status.DELIVERED_TOpLAYER; 
                return linea; 
            });
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync(pedido);
        _mockUserRepo.Setup(r => r.FindByIdAsync(playerId)).ReturnsAsync(player);

        var result = await _service.ChangeLineaStatusAsync(lineaId, Status.DELIVERED_TOpLAYER.ToString());

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(Status.DELIVERED_TOpLAYER);
    }

    #endregion

    #region NEW Unit Tests for PurchasedService

    // ===== FindByIdAsync New Tests =====
    [Test]
    public async Task FindByIdAsync_PlayerNotFound_ReturnsUserNotFoundError()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);

        _mockCache.Setup(c => c.GetAsync<PurchasedResponseDto>(CacheKeys.PurchasedCacheKey + id))
            .ReturnsAsync((PurchasedResponseDto?)null);
        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync((User?)null);

        var result = await _service.FindByIdAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task FindByIdAsync_EncorderNotFound_ReturnsUserNotFoundError()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);

        _mockCache.Setup(c => c.GetAsync<PurchasedResponseDto>(CacheKeys.PurchasedCacheKey + id))
            .ReturnsAsync((PurchasedResponseDto?)null);
        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(purchased);
        
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync(player);

        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.AssignedTo)).ReturnsAsync((User?)null);

        var result = await _service.FindByIdAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    // ===== CreatePurchasedAsync New Tests =====
    [Test]
    public async Task CreatePurchasedAsync_ConcurrencyError_ReturnsConcurrencyError()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ThrowsAsync(new DbUpdateConcurrencyException());
        _mockUserRepo.Setup(r => r.FindByIdAsync(player.Id)).ReturnsAsync(player);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConcurrencyError>();
        _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Exactly(3));
    }

    [Test]
    public async Task CreatePurchasedAsync_ConcurrencyReloadNull_ReturnsUserNotFoundError()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ThrowsAsync(new DbUpdateConcurrencyException());
        _mockUserRepo.Setup(r => r.FindByIdAsync(player.Id)).ReturnsAsync((User?)null);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
        _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Test]
    public async Task CreatePurchasedAsync_ValidPaidUserWithBadEmail_SkipsEmailNotification()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        player.Email = "invalid-email";
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync("player1")).ReturnsAsync(player);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsSuccess.Should().BeTrue();
        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
    }

    // ===== UpdatePurchasedAsync New Tests =====
    [Test]
    public async Task UpdatePurchasedAsync_RepositoryUpdateFails_ReturnsNotFoundError()
    {
        var id = Ulid.NewUlid();
        var existing = PedidosBuilder.Create(id: id);
        var patch = new PurchasedPatchDto { Machine = "NewMachine" };

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdatePurchasedAsync(It.IsAny<Pedidos>(), id)).ReturnsAsync((Pedidos?)null);

        var result = await _service.UpdatePurchasedAsync(id, patch);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    [Test]
    public async Task UpdatePurchasedAsync_PlayerNotFound_ReturnsUserNotFoundError()
    {
        var id = Ulid.NewUlid();
        var existing = PedidosBuilder.Create(id: id);
        var patch = new PurchasedPatchDto { Machine = "NewMachine" };
        var updated = PedidosBuilder.Create(id: id);

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdatePurchasedAsync(It.IsAny<Pedidos>(), id)).ReturnsAsync(updated);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + updated.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(updated.PlayerId)).ReturnsAsync((User?)null);

        var result = await _service.UpdatePurchasedAsync(id, patch);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task UpdatePurchasedAsync_EncorderNotFound_ReturnsUserNotFoundError()
    {
        var id = Ulid.NewUlid();
        var existing = PedidosBuilder.Create(id: id);
        var patch = new PurchasedPatchDto { Machine = "NewMachine" };
        var updated = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdatePurchasedAsync(It.IsAny<Pedidos>(), id)).ReturnsAsync(updated);
        
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + updated.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(updated.PlayerId)).ReturnsAsync(player);

        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + updated.AssignedTo))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(updated.AssignedTo)).ReturnsAsync((User?)null);

        var result = await _service.UpdatePurchasedAsync(id, patch);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    // ===== CancelPurchasedAsync New Tests =====
    [Test]
    public async Task CancelPurchasedAsync_PlayerNotFound_ReturnsUserNotFoundError()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);

        _mockRepo.Setup(r => r.CancelPurchasedAsync(id)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync((User?)null);

        var result = await _service.CancelPurchasedAsync(id, false, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task CancelPurchasedAsync_EncorderNotFound_ReturnsUserNotFoundError()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1");

        _mockRepo.Setup(r => r.CancelPurchasedAsync(id)).ReturnsAsync(purchased);
        
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync(player);

        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.AssignedTo)).ReturnsAsync((User?)null);

        var result = await _service.CancelPurchasedAsync(id, false, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task CancelPurchasedAsync_NoWhatsAppSent_WhenPhoneIsEmpty()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        player.Phone = "";
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);

        _mockRepo.Setup(r => r.CancelPurchasedAsync(id)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync(encorderDto);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(player.Username)).ReturnsAsync(player);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync(player);

        var result = await _service.CancelPurchasedAsync(id, false, null);

        result.IsSuccess.Should().BeTrue();
        _mockWhatsApp.Verify(w => w.SendPedidoCanceledMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    // ===== ChangePaymentStatusPurchasedAsync New Tests =====
    [Test]
    public async Task ChangePaymentStatusPurchasedAsync_RepositoryChangeFails_ReturnsNotFoundError()
    {
        var id = Ulid.NewUlid();
        _mockRepo.Setup(r => r.ChangeStatusPurchasedAsync(id, "PAID")).ReturnsAsync((Pedidos?)null);

        var result = await _service.ChangePaymentStatusPurchasedAsync(id, "PAID");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    [Test]
    public async Task ChangePaymentStatusPurchasedAsync_PlayerNotFound_ReturnsUserNotFoundError()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);

        _mockRepo.Setup(r => r.ChangeStatusPurchasedAsync(id, "PAID")).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync((User?)null);

        var result = await _service.ChangePaymentStatusPurchasedAsync(id, "PAID");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task ChangePaymentStatusPurchasedAsync_EncorderNotFound_ReturnsUserNotFoundError()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1");

        _mockRepo.Setup(r => r.ChangeStatusPurchasedAsync(id, "PAID")).ReturnsAsync(purchased);
        
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync(player);

        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.AssignedTo)).ReturnsAsync((User?)null);

        var result = await _service.ChangePaymentStatusPurchasedAsync(id, "PAID");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    // ===== UpdateLineaAsync & CancelLineaAsync New Tests =====
    [Test]
    public async Task UpdateLineaAsync_RepositoryUpdateFails_ReturnsNotFoundError()
    {
        var lineaId = Ulid.NewUlid();
        var linea = PedidoLineaBuilder.Create(id: lineaId);
        var patch = new PedidoLineaPatchDto { Nudos = 50 };

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.UpdateLineaAsync(It.IsAny<PedidoLinea>(), lineaId)).ReturnsAsync((PedidoLinea?)null);

        var result = await _service.UpdateLineaAsync(lineaId, patch);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    [Test]
    public async Task CancelLineaAsync_RepositoryChangeFails_ReturnsNotFoundError()
    {
        var lineaId = Ulid.NewUlid();
        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(PedidoLineaBuilder.Create(id: lineaId));
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.CANCELED)).ReturnsAsync((PedidoLinea?)null);

        var result = await _service.CancelLineaAsync(lineaId, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    // ===== ChangeLineaStatusAsync New Tests =====
    [Test]
    public async Task ChangeLineaStatusAsync_RepositoryChangeFails_ReturnsNotFoundError()
    {
        var lineaId = Ulid.NewUlid();
        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(PedidoLineaBuilder.Create(id: lineaId));
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.COMPLETED)).ReturnsAsync((PedidoLinea?)null);

        var result = await _service.ChangeLineaStatusAsync(lineaId, "COMPLETED");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    [Test]
    public async Task ChangeLineaStatusAsync_Completed_SendsEmailAndWhatsApp()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var pedido = PedidosBuilder.Create(id: pedidoId, playerId: playerId);
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Pedido = pedido;

        var player = CreateUser("player1", User.UserRoles.USER, 0);
        player.Id = playerId;
        player.Email = "player1@test.com";
        player.Phone = "+34123456789";

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.COMPLETED)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync(pedido);
        _mockUserRepo.Setup(r => r.FindByIdAsync(playerId)).ReturnsAsync(player);

        var result = await _service.ChangeLineaStatusAsync(lineaId, "COMPLETED");

        result.IsSuccess.Should().BeTrue();
        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.Is<EmailMessage>(m => m.To == player.Email && m.Subject == "Línea completada")), Times.Once);
        _mockWhatsApp.Verify(w => w.SendLineaCompletedMessageAsync(player.Phone, player.Name, linea.RaquetModel, pedidoId.ToString()), Times.Once);
    }

    [Test]
    public async Task ChangeLineaStatusAsync_Delivered_SendsEmailOnly()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var pedido = PedidosBuilder.Create(id: pedidoId, playerId: playerId);
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Pedido = pedido;

        var player = CreateUser("player1", User.UserRoles.USER, 0);
        player.Id = playerId;
        player.Email = "player1@test.com";
        player.Phone = "+34123456789";

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.DELIVERED_TOpLAYER)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync(pedido);
        _mockUserRepo.Setup(r => r.FindByIdAsync(playerId)).ReturnsAsync(player);

        var result = await _service.ChangeLineaStatusAsync(lineaId, "DELIVERED_TOpLAYER");

        result.IsSuccess.Should().BeTrue();
        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.Is<EmailMessage>(m => m.To == player.Email && m.Subject == "Línea entregada")), Times.Once);
        _mockWhatsApp.Verify(w => w.SendLineaCompletedMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ChangeLineaStatusAsync_Canceled_SendsWhatsAppOnly()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var pedido = PedidosBuilder.Create(id: pedidoId, playerId: playerId);
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Pedido = pedido;

        var player = CreateUser("player1", User.UserRoles.USER, 0);
        player.Id = playerId;
        player.Email = "player1@test.com";
        player.Phone = "+34123456789";

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.CANCELED)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync(pedido);
        _mockUserRepo.Setup(r => r.FindByIdAsync(playerId)).ReturnsAsync(player);

        var result = await _service.ChangeLineaStatusAsync(lineaId, "CANCELED");

        result.IsSuccess.Should().BeTrue();
        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
        _mockWhatsApp.Verify(w => w.SendLineaCanceledMessageAsync(player.Phone, player.Name, linea.RaquetModel, pedidoId.ToString()), Times.Once);
    }

    // ===== ChangeAllLineasStatusAsync New Tests =====
    [Test]
    public async Task ChangeAllLineasStatusAsync_EmptyLineas_ReturnsNotFoundError()
    {
        var purchasedId = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: purchasedId);
        purchased.Lineas = null;

        _mockRepo.Setup(r => r.FindByIdAsync(purchasedId)).ReturnsAsync(purchased);

        var result = await _service.ChangeAllLineasStatusAsync(purchasedId, "COMPLETED");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PurchasedNotFoundError>();
    }

    [Test]
    public async Task ChangeAllLineasStatusAsync_NoLinesToUpdate_ReturnsResponseDtoWithoutUpdate()
    {
        var purchasedId = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: purchasedId);
        var linea = PedidoLineaBuilder.Create(pedidoId: purchasedId, status: Status.COMPLETED);
        purchased.Lineas = new List<PedidoLinea> { linea };

        var player = CreateUser("player1");
        var playerDto = new UserResponseDto(player.Username, "", player.Name, 0);
        var encorder = CreateUser("encorder1");
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, 0);

        _mockRepo.Setup(r => r.FindByIdAsync(purchasedId)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId)).ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo)).ReturnsAsync(encorderDto);

        var result = await _service.ChangeAllLineasStatusAsync(purchasedId, "CANCELED");

        result.IsSuccess.Should().BeTrue();
        result.Value.Lineas[0].Status.Should().Be(Status.COMPLETED);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Test]
    public async Task ChangeAllLineasStatusAsync_UpdatesLinesAndSavesChanges_ReturnsSuccess()
    {
        var purchasedId = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: purchasedId);
        var linea = PedidoLineaBuilder.Create(pedidoId: purchasedId, status: Status.PENDING);
        purchased.Lineas = new List<PedidoLinea> { linea };

        var player = CreateUser("player1");
        var playerDto = new UserResponseDto(player.Username, "", player.Name, 0);
        var encorder = CreateUser("encorder1");
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, 0);

        _mockRepo.Setup(r => r.FindByIdAsync(purchasedId)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId)).ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo)).ReturnsAsync(encorderDto);

        var result = await _service.ChangeAllLineasStatusAsync(purchasedId, "COMPLETED");

        result.IsSuccess.Should().BeTrue();
        result.Value.Lineas[0].Status.Should().Be(Status.COMPLETED);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task ChangeAllLineasStatusAsync_PlayerNotFound_ReturnsUserNotFoundError()
    {
        var purchasedId = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: purchasedId);
        var linea = PedidoLineaBuilder.Create(pedidoId: purchasedId, status: Status.PENDING);
        purchased.Lineas = new List<PedidoLinea> { linea };

        _mockRepo.Setup(r => r.FindByIdAsync(purchasedId)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId)).ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync((User?)null);

        var result = await _service.ChangeAllLineasStatusAsync(purchasedId, "COMPLETED");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task ChangeAllLineasStatusAsync_EncorderNotFound_ReturnsUserNotFoundError()
    {
        var purchasedId = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: purchasedId);
        var linea = PedidoLineaBuilder.Create(pedidoId: purchasedId, status: Status.PENDING);
        purchased.Lineas = new List<PedidoLinea> { linea };
        var player = CreateUser("player1");
        var playerDto = new UserResponseDto(player.Username, "", player.Name, 0);

        _mockRepo.Setup(r => r.FindByIdAsync(purchasedId)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId)).ReturnsAsync(playerDto);
        
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo)).ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.AssignedTo)).ReturnsAsync((User?)null);

        var result = await _service.ChangeAllLineasStatusAsync(purchasedId, "COMPLETED");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task FindByIdAsync_PlayerIsDeleted_ReturnsUserNotFoundError()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        player.IsDeleted = true;

        _mockCache.Setup(c => c.GetAsync<PurchasedResponseDto>(CacheKeys.PurchasedCacheKey + id))
            .ReturnsAsync((PurchasedResponseDto?)null);
        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync(player);

        var result = await _service.FindByIdAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task CreatePurchasedAsync_ZeroBonos_DoesNotAppendFaltaComment()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 0); // 0 bonos
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = "Initial comments",
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Comments.Should().Be("Initial comments"); // Comments should remain untouched
    }

    [Test]
    public async Task CreatePurchasedAsync_ConcurrencyResolvedOnSecondAttempt_ReturnsSuccess()
    {
        var player1 = CreateUser("player1", User.UserRoles.USER, 100);
        var player2 = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player1);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);

        // First attempt throws concurrency exception, second succeeds
        _mockUserRepo.SetupSequence(r => r.UpdateAsync(It.IsAny<User>()))
            .ThrowsAsync(new DbUpdateConcurrencyException())
            .ReturnsAsync(player2);

        // FindByIdAsync to reload player for the retry
        _mockUserRepo.Setup(r => r.FindByIdAsync(player1.Id)).ReturnsAsync(player2);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync("player1")).ReturnsAsync(player1);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsSuccess.Should().BeTrue();
        _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Exactly(2));
    }

    [Test]
    public async Task CreatePurchasedAsync_PaidStatusWithDeletedPlayerForEmail_SkipsEmailNotification()
    {
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var request = new PurchasedRequestDto
        {
            PlayerName = "player1",
            AssignedToName = "encorder1",
            TournamentId = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = null,
            PayStatus = PaymentStatus.PENDING_PAYMENT.ToString(),
            Lineas = new List<PedidoLineaRequestDto>(),
            Price = 50.0
        };

        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "player1")).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + "encorder1")).ReturnsAsync(encorder);
        _mockRepo.Setup(r => r.CreatePurchasedAsync(It.IsAny<Pedidos>())).ReturnsAsync((Pedidos p) => p);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        // Player is deleted/not found when checking email
        var deletedPlayer = CreateUser("player1");
        deletedPlayer.IsDeleted = true;
        _mockUserRepo.Setup(r => r.FindByUsernameAsync("player1")).ReturnsAsync(deletedPlayer);

        var result = await _service.CreatePurchasedAsync(request);

        result.IsSuccess.Should().BeTrue();
        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
    }

    [Test]
    public async Task CancelPurchasedAsync_UserRoleWithInvalidIdUserUlid_SkipsOwnerValidation()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);

        _mockRepo.Setup(r => r.CancelPurchasedAsync(id)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync(encorderDto);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(player.Username)).ReturnsAsync(player);

        // isUser is true, but idUser is an invalid ULID string. Skip validation and proceed.
        var result = await _service.CancelPurchasedAsync(id, true, "invalid-ulid");

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CancelPurchasedAsync_PlayerNotFoundAtNotificationTime_SkipsWhatsApp()
    {
        var id = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: id);
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var playerDto = new UserResponseDto(player.Username, "", player.Name, (int)player.Bonos);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, (int)encorder.Bonos);

        _mockRepo.Setup(r => r.CancelPurchasedAsync(id)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId))
            .ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo))
            .ReturnsAsync(encorderDto);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(player.Username)).ReturnsAsync(player);

        // Player not found when trying to send WhatsApp
        _mockUserRepo.Setup(r => r.FindByIdAsync(purchased.PlayerId)).ReturnsAsync((User?)null);

        var result = await _service.CancelPurchasedAsync(id, false, null);

        result.IsSuccess.Should().BeTrue();
        _mockWhatsApp.Verify(w => w.SendPedidoCanceledMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task CancelLineaAsync_OrderNotFoundForWhatsApp_SkipsWhatsApp()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Status = Status.PENDING;
        linea.Pedido = PedidosBuilder.Create(id: pedidoId);

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.CANCELED)).ReturnsAsync(linea);
        
        // Order not found
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync((Pedidos?)null);

        var result = await _service.CancelLineaAsync(lineaId, null, null);

        result.IsSuccess.Should().BeTrue();
        _mockWhatsApp.Verify(w => w.SendLineaCanceledMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task CancelLineaAsync_PlayerNotFoundForWhatsApp_SkipsWhatsApp()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Status = Status.PENDING;
        var pedido = PedidosBuilder.Create(id: pedidoId);
        pedido.PlayerId = playerId;
        linea.Pedido = pedido;

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.CANCELED)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync(pedido);
        
        // Player not found
        _mockUserRepo.Setup(r => r.FindByIdAsync(playerId)).ReturnsAsync((User?)null);

        var result = await _service.CancelLineaAsync(lineaId, null, null);

        result.IsSuccess.Should().BeTrue();
        _mockWhatsApp.Verify(w => w.SendLineaCanceledMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task CancelLineaAsync_PlayerPhoneEmptyForWhatsApp_SkipsWhatsApp()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Status = Status.PENDING;
        var pedido = PedidosBuilder.Create(id: pedidoId);
        pedido.PlayerId = playerId;
        linea.Pedido = pedido;
        var player = CreateUser("player1");
        player.Phone = ""; // Empty phone number

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.CANCELED)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync(pedido);
        _mockUserRepo.Setup(r => r.FindByIdAsync(playerId)).ReturnsAsync(player);

        var result = await _service.CancelLineaAsync(lineaId, null, null);

        result.IsSuccess.Should().BeTrue();
        _mockWhatsApp.Verify(w => w.SendLineaCanceledMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ChangeLineaStatusAsync_OrderNotFound_SkipsNotifications()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var pedido = PedidosBuilder.Create(id: pedidoId);
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Pedido = pedido;

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.COMPLETED)).ReturnsAsync(linea);
        
        // Order not found
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync((Pedidos?)null);

        var result = await _service.ChangeLineaStatusAsync(lineaId, "COMPLETED");

        result.IsSuccess.Should().BeTrue();
        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
        _mockWhatsApp.Verify(w => w.SendLineaCompletedMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ChangeLineaStatusAsync_PlayerNotFound_SkipsNotifications()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var pedido = PedidosBuilder.Create(id: pedidoId, playerId: playerId);
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Pedido = pedido;

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.COMPLETED)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync(pedido);
        
        // Player not found
        _mockUserRepo.Setup(r => r.FindByIdAsync(playerId)).ReturnsAsync((User?)null);

        var result = await _service.ChangeLineaStatusAsync(lineaId, "COMPLETED");

        result.IsSuccess.Should().BeTrue();
        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
        _mockWhatsApp.Verify(w => w.SendLineaCompletedMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ChangeLineaStatusAsync_PlayerEmailInvalid_SkipsEmailOnly()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var pedido = PedidosBuilder.Create(id: pedidoId, playerId: playerId);
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Pedido = pedido;

        var player = CreateUser("player1");
        player.Email = "invalidemail"; // Invalid email
        player.Phone = "+34123456789";

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.COMPLETED)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync(pedido);
        _mockUserRepo.Setup(r => r.FindByIdAsync(playerId)).ReturnsAsync(player);

        var result = await _service.ChangeLineaStatusAsync(lineaId, "COMPLETED");

        result.IsSuccess.Should().BeTrue();
        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
        _mockWhatsApp.Verify(w => w.SendLineaCompletedMessageAsync(player.Phone, player.Name, linea.RaquetModel, pedidoId.ToString()), Times.Once);
    }

    [Test]
    public async Task ChangeLineaStatusAsync_PlayerPhoneEmpty_SkipsWhatsAppOnly()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var pedido = PedidosBuilder.Create(id: pedidoId, playerId: playerId);
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Pedido = pedido;

        var player = CreateUser("player1");
        player.Email = "player1@test.com";
        player.Phone = ""; // Empty phone

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.COMPLETED)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync(pedido);
        _mockUserRepo.Setup(r => r.FindByIdAsync(playerId)).ReturnsAsync(player);

        var result = await _service.ChangeLineaStatusAsync(lineaId, "COMPLETED");

        result.IsSuccess.Should().BeTrue();
        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.Is<EmailMessage>(m => m.To == player.Email)), Times.Once);
        _mockWhatsApp.Verify(w => w.SendLineaCompletedMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ChangeLineaStatusAsync_StatusPending_SkipsNotifications()
    {
        var lineaId = Ulid.NewUlid();
        var pedidoId = Ulid.NewUlid();
        var playerId = Ulid.NewUlid();
        var pedido = PedidosBuilder.Create(id: pedidoId, playerId: playerId);
        var linea = PedidoLineaBuilder.Create(id: lineaId, pedidoId: pedidoId);
        linea.Pedido = pedido;

        var player = CreateUser("player1");
        player.Email = "player1@test.com";
        player.Phone = "+34123456789";

        _mockRepo.Setup(r => r.FindLineaByIdAsync(lineaId)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.ChangeLineaStatusAsync(lineaId, Status.PENDING)).ReturnsAsync(linea);
        _mockRepo.Setup(r => r.FindByIdAsync(pedidoId)).ReturnsAsync(pedido);
        _mockUserRepo.Setup(r => r.FindByIdAsync(playerId)).ReturnsAsync(player);

        var result = await _service.ChangeLineaStatusAsync(lineaId, "PENDING");

        result.IsSuccess.Should().BeTrue();
        _mockEmail.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
        _mockWhatsApp.Verify(w => w.SendLineaCompletedMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockWhatsApp.Verify(w => w.SendLineaCanceledMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ChangeAllLineasStatusAsync_ToCompletedWithCanceledLine_NoLinesToUpdate()
    {
        var purchasedId = Ulid.NewUlid();
        var purchased = PedidosBuilder.Create(id: purchasedId);
        var linea = PedidoLineaBuilder.Create(pedidoId: purchasedId, status: Status.CANCELED);
        purchased.Lineas = new List<PedidoLinea> { linea };

        var player = CreateUser("player1");
        var playerDto = new UserResponseDto(player.Username, "", player.Name, 0);
        var encorder = CreateUser("encorder1");
        var encorderDto = new UserResponseDto(encorder.Username, "", encorder.Name, 0);

        _mockRepo.Setup(r => r.FindByIdAsync(purchasedId)).ReturnsAsync(purchased);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.PlayerId)).ReturnsAsync(playerDto);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + purchased.AssignedTo)).ReturnsAsync(encorderDto);

        // Change all lines to COMPLETED, but the line is CANCELED, so it shouldn't update it
        var result = await _service.ChangeAllLineasStatusAsync(purchasedId, "COMPLETED");

        result.IsSuccess.Should().BeTrue();
        result.Value.Lineas[0].Status.Should().Be(Status.CANCELED);
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    #endregion
}