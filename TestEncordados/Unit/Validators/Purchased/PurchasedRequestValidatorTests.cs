using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.keys;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using BackEncordados.Purchased.Validator;
using BackEncordados.Talleres.Model;
using BackEncordados.Talleres.Repository;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using FluentAssertions;
using FluentValidation;
using Moq;
using TestEncordados.Unit.Fixtures;
using PurchasedRequestValidatorType = BackEncordados.Purchased.Validator.PurchasedRequestValidator;
using ITournamentRepositoryType = BackEncordados.Talleres.Repository.ITournamentRepository;
using IUserRepositoryType = BackEncordados.Usuarios.Repository.IUserRepository;
using ICacheServiceType = BackEncordados.Common.Service.Cache.ICacheService;

namespace TestEncordados.Unit.Validators.Purchased;

public class PurchasedRequestValidatorTests
{
    private readonly Mock<ITournamentRepositoryType> _mockTournamentRepo;
    private readonly Mock<IUserRepositoryType> _mockUserRepo;
    private readonly Mock<ICacheServiceType> _mockCache;
    private readonly PurchasedRequestValidatorType _validator;

    public PurchasedRequestValidatorTests()
    {
        _mockTournamentRepo = new Mock<ITournamentRepositoryType>();
        _mockUserRepo = new Mock<IUserRepositoryType>();
        _mockCache = CacheServiceBuilder.Create();
        _validator = new PurchasedRequestValidatorType(
            _mockTournamentRepo.Object,
            _mockUserRepo.Object,
            _mockCache.Object);
    }

    private static PurchasedRequestDto CreateValidDto(string playerName = "player1", string encorderName = "encorder1") => new()
    {
        TournamentId = Ulid.NewUlid(),
        PlayerName = playerName,
        AssignedToName = encorderName,
        Machine = "Machine-1",
        Comments = "Test comment",
        PayStatus = "PENDING_PAYMENT",
        Price = 50.0,
        Lineas = new List<PedidoLineaRequestDto>
        {
            new()
            {
                RaquetModel = "Wilson Pro Staff",
                Nudos = 4,
                DateString = DateTime.UtcNow.AddDays(1),
                Logotype = true,
                Color = "Negro",
                StringSetup = new StringSetupDto
                {
                    StringV = "Synthetic Gut",
                    TensionV = 20.0,
                    PreStetchV = 10,
                    StringH = "Natural Gut",
                    TensionH = 18.0,
                    PreStetchH = 5
                }
            }
        }
    };

    [Test]
    public async Task Validate_ValidDto_ReturnsSuccess()
    {
        var dto = CreateValidDto();
        var tournament = TournamentBuilder.Create(id: dto.TournamentId);
        var player = UserBuilder.StandardUser();
        var encorder = UserBuilder.EncorderUser();

        _mockTournamentRepo.Setup(r => r.FindByIdAsync(dto.TournamentId)).ReturnsAsync(tournament);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.PlayerName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.PlayerName)).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.AssignedToName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.AssignedToName)).ReturnsAsync(encorder);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task Validate_NullTournamentId_ReturnsError()
    {
        var dto = CreateValidDto();
        var field = nameof(dto.TournamentId);

        _mockTournamentRepo.Setup(r => r.FindByIdAsync(It.IsAny<Ulid>())).ReturnsAsync((Tournaments?)null);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains(field));
    }

    [Test]
    public async Task Validate_DeletedTournament_ReturnsError()
    {
        var dto = CreateValidDto();
        var tournament = TournamentBuilder.DeletedTournament(dto.TournamentId);

        _mockTournamentRepo.Setup(r => r.FindByIdAsync(dto.TournamentId)).ReturnsAsync(tournament);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("torneo"));
    }

    [Test]
    public async Task Validate_EmptyPlayerName_ReturnsError()
    {
        var dto = CreateValidDto(playerName: "");

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("PlayerName"));
    }

    [Test]
    public async Task Validate_NonExistentPlayer_ReturnsError()
    {
        var dto = CreateValidDto();

        _mockTournamentRepo.Setup(r => r.FindByIdAsync(dto.TournamentId)).ReturnsAsync(TournamentBuilder.Create(id: dto.TournamentId));
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.PlayerName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.PlayerName)).ReturnsAsync((User?)null);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("jugador"));
    }

    [Test]
    public async Task Validate_DeletedPlayer_ReturnsError()
    {
        var dto = CreateValidDto();
        var player = UserBuilder.DeletedUser();

        _mockTournamentRepo.Setup(r => r.FindByIdAsync(dto.TournamentId)).ReturnsAsync(TournamentBuilder.Create(id: dto.TournamentId));
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.PlayerName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.PlayerName)).ReturnsAsync(player);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task Validate_PlayerNotUserRole_ReturnsError()
    {
        var dto = CreateValidDto();
        var player = UserBuilder.EncorderUser();

        _mockTournamentRepo.Setup(r => r.FindByIdAsync(dto.TournamentId)).ReturnsAsync(TournamentBuilder.Create(id: dto.TournamentId));
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.PlayerName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.PlayerName)).ReturnsAsync(player);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("jugador"));
    }

    [Test]
    public async Task Validate_EmptyAssignedToName_ReturnsError()
    {
        var dto = CreateValidDto(encorderName: "");

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("AssignedToName"));
    }

    [Test]
    public async Task Validate_NonExistentEncorder_ReturnsError()
    {
        var dto = CreateValidDto();

        _mockTournamentRepo.Setup(r => r.FindByIdAsync(dto.TournamentId)).ReturnsAsync(TournamentBuilder.Create(id: dto.TournamentId));
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.PlayerName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.PlayerName)).ReturnsAsync(UserBuilder.StandardUser());
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.AssignedToName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.AssignedToName)).ReturnsAsync((User?)null);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("encordador"));
    }

    [Test]
    public async Task Validate_StandardUserAsEncorder_ReturnsError()
    {
        var dto = CreateValidDto();
        var standardUser = UserBuilder.StandardUser();

        _mockTournamentRepo.Setup(r => r.FindByIdAsync(dto.TournamentId)).ReturnsAsync(TournamentBuilder.Create(id: dto.TournamentId));
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.PlayerName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.PlayerName)).ReturnsAsync(UserBuilder.StandardUser());
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.AssignedToName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.AssignedToName)).ReturnsAsync(standardUser);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task Validate_OwnerAsEncorder_ReturnsSuccess()
    {
        var dto = CreateValidDto();
        var tournament = TournamentBuilder.Create(id: dto.TournamentId);
        var player = UserBuilder.StandardUser();
        var owner = UserBuilder.OwnerUser();

        _mockTournamentRepo.Setup(r => r.FindByIdAsync(dto.TournamentId)).ReturnsAsync(tournament);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.PlayerName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.PlayerName)).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.AssignedToName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.AssignedToName)).ReturnsAsync(owner);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task Validate_InvalidPayStatus_ReturnsError()
    {
        var dto = CreateValidDto();
        dto = new PurchasedRequestDto
        {
            TournamentId = dto.TournamentId,
            PlayerName = dto.PlayerName,
            AssignedToName = dto.AssignedToName,
            Machine = dto.Machine,
            Comments = dto.Comments,
            PayStatus = "INVALID_STATUS",
            Price = dto.Price,
            Lineas = dto.Lineas
        };

        _mockTournamentRepo.Setup(r => r.FindByIdAsync(dto.TournamentId)).ReturnsAsync(TournamentBuilder.Create(id: dto.TournamentId));
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.PlayerName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.PlayerName)).ReturnsAsync(UserBuilder.StandardUser());
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.AssignedToName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.AssignedToName)).ReturnsAsync(UserBuilder.EncorderUser());

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("PayStatus"));
    }

    [Test]
    public async Task Validate_EmptyLineas_ReturnsError()
    {
        var dto = new PurchasedRequestDto
        {
            TournamentId = Ulid.NewUlid(),
            PlayerName = "player1",
            AssignedToName = "encorder1",
            Machine = "Machine-1",
            Comments = "",
            PayStatus = "PENDING_PAYMENT",
            Price = 50.0,
            Lineas = new List<PedidoLineaRequestDto>()
        };

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Lineas"));
    }

    [Test]
    public async Task Validate_CachedPlayer_ReturnsSuccess()
    {
        var dto = CreateValidDto();
        var tournament = TournamentBuilder.Create(id: dto.TournamentId);
        var player = UserBuilder.StandardUser();
        var encorder = UserBuilder.EncorderUser();

        _mockTournamentRepo.Setup(r => r.FindByIdAsync(dto.TournamentId)).ReturnsAsync(tournament);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.PlayerName)).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.AssignedToName)).ReturnsAsync(encorder);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ValidPayStatuses_ReturnsSuccess()
    {
        var dto = CreateValidDto();
        var tournament = TournamentBuilder.Create(id: dto.TournamentId);
        var player = UserBuilder.StandardUser();
        var encorder = UserBuilder.EncorderUser();

        _mockTournamentRepo.Setup(r => r.FindByIdAsync(dto.TournamentId)).ReturnsAsync(tournament);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.PlayerName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.PlayerName)).ReturnsAsync(player);
        _mockCache.Setup(c => c.GetAsync<User>(CacheKeys.UserKey + dto.AssignedToName)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.AssignedToName)).ReturnsAsync(encorder);

        foreach (var status in new[] { "PENDING_PAYMENT", "PAID", "CANCELED", "FINNISH_TOURNAMENT" })
        {
            dto = new PurchasedRequestDto
            {
                TournamentId = dto.TournamentId,
                PlayerName = dto.PlayerName,
                AssignedToName = dto.AssignedToName,
                Machine = dto.Machine,
                Comments = dto.Comments,
                PayStatus = status,
                Price = dto.Price,
                Lineas = dto.Lineas
            };

            var result = await _validator.ValidateAsync(dto);

            result.IsValid.Should().BeTrue();
        }
    }
}