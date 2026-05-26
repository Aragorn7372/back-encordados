using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Validator.Materials;
using BackEncordados.Talleres.Model;
using BackEncordados.Talleres.Repository;
using FluentAssertions;
using Moq;

namespace TestEncordados.Unit.Validators.Materials;

public class MaterialRequestValidatorTests
{
    private Mock<ITournamentRepository> _mockRepository = null!;
    private MaterialRequestValidator _validator = null!;

    private void SetupValidator(Tournaments? tournamentToReturn)
    {
        _mockRepository = new Mock<ITournamentRepository>();
        _mockRepository
            .Setup(r => r.FindByIdAsync(It.IsAny<Ulid>()))
            .ReturnsAsync(tournamentToReturn);
        _validator = new MaterialRequestValidator(_mockRepository.Object);
    }

    private static MaterialRequestDto CreateValidDto(Ulid tournamentId) => new()
    {
        Marca = "Head",
        TournamentId = tournamentId,
        Modelo = "Extreme",
        Stock = 50,
        Precio = 35.99,
        Type = "Grip"
    };

    private static MaterialRequestDto CreateDtoWithType(string type, Ulid tournamentId) => new()
    {
        Marca = "Head",
        TournamentId = tournamentId,
        Modelo = "Extreme",
        Stock = 50,
        Precio = 35.99,
        Type = type
    };

    private static MaterialRequestDto CreateDtoWithTournamentId(Ulid tournamentId) => new()
    {
        Marca = "Head",
        TournamentId = tournamentId,
        Modelo = "Extreme",
        Stock = 50,
        Precio = 35.99,
        Type = "Grip"
    };

    private static MaterialRequestDto CreateDtoWithTournamentIdNull() => new()
    {
        Marca = "Head",
        TournamentId = default,
        Modelo = "Extreme",
        Stock = 50,
        Precio = 35.99,
        Type = "Grip"
    };

    private static MaterialRequestDto CreateDtoWithTypeValid(Ulid tournamentId) => new()
    {
        Marca = "Head",
        TournamentId = tournamentId,
        Modelo = "Extreme",
        Stock = 50,
        Precio = 35.99,
        Type = "Grip"
    };

    [Test]
    public async Task Validate_AllValidWithExistingTournament_ReturnsNoErrors()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);

        var dto = CreateValidDto(tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task Validate_TournamentIdIsNull_ReturnsError()
    {
        SetupValidator(null);
        var dto = CreateDtoWithTournamentIdNull();

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(MaterialRequestDto.TournamentId));
    }

    [Test]
    public async Task Validate_TournamentDoesNotExist_ReturnsError()
    {
        SetupValidator(null);
        var dto = CreateDtoWithTournamentId(Ulid.NewUlid());

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(MaterialRequestDto.TournamentId) &&
            e.ErrorMessage.Contains("torneo"));
    }

    [Test]
    public async Task Validate_TournamentIsDeleted_ReturnsError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = true };
        SetupValidator(tournament);

        var dto = CreateDtoWithTournamentId(tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(MaterialRequestDto.TournamentId) &&
            e.ErrorMessage.Contains("cancelado"));
    }

    [Test]
    public async Task Validate_TypeIsEmpty_ReturnsError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = CreateDtoWithType("", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(MaterialRequestDto.Type));
    }

    [Test]
    public async Task Validate_TypeIsInvalid_ReturnsError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = CreateDtoWithType("InvalidType", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(MaterialRequestDto.Type) &&
            e.ErrorMessage.Contains("inválido"));
    }

    [Test]
    public async Task Validate_TypeIsValid_ReturnsNoError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = CreateDtoWithType("Grip", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(MaterialRequestDto.Type));
    }

    [Test]
    public async Task Validate_AllValidEnumTypes_ReturnsNoErrors()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);

        var validTypes = new[] { "Grip", "Overgrip", "LeadTape", "Silicone", "Otro" };

        foreach (var type in validTypes)
        {
            var dto = CreateDtoWithType(type, tournamentId);

            var result = await _validator.ValidateAsync(dto);

            result.Errors.Should().NotContain(e => e.PropertyName == nameof(MaterialRequestDto.Type));
        }
    }

    [Test]
    public async Task Validate_TypeIsCaseInsensitive_ReturnsNoError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = CreateDtoWithType("GRIP", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(MaterialRequestDto.Type));
    }
}