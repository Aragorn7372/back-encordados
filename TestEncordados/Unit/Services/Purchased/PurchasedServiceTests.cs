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
    public async Task UpdatePurchasedAsync_Valid_ReturnsSuccess()
    {
        var id = Ulid.NewUlid();
        var existing = PedidosBuilder.Create(id: id);
        var dto = new PurchasedPatchDto { Machine = "NewMachine", Comments = "Updated" };
        var updated = (Pedidos)PedidosBuilder.Create(id: id);
        updated.Comments = "Updated";
        var player = CreateUser("player1", User.UserRoles.USER, 100);
        var encorder = CreateUser("encorder1", User.UserRoles.ENCORDER, 0);

        _mockRepo.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdatePurchasedAsync(It.IsAny<Pedidos>(), id)).ReturnsAsync(updated);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + updated.PlayerId))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(updated.PlayerId)).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + updated.AssignedTo))
            .ReturnsAsync((UserResponseDto?)null);
        _mockUserRepo.Setup(r => r.FindByIdAsync(updated.AssignedTo)).ReturnsAsync(encorder);

        var result = await _service.UpdatePurchasedAsync(id, dto);

        result.IsSuccess.Should().BeTrue();
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
}