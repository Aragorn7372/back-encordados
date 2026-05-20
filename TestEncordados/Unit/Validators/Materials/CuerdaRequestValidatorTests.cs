using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Validator.Strings;
using BackEncordados.Talleres.Model;
using BackEncordados.Talleres.Repository;
using FluentAssertions;
using Moq;

namespace TestEncordados.Unit.Validators.Materials;

public class CuerdaRequestValidatorTests
{
    private Mock<ITournamentRepository> _mockRepository = null!;
    private CuerdaRequestValidator _validator = null!;

    private void SetupValidator(Tournaments? tournamentToReturn)
    {
        _mockRepository = new Mock<ITournamentRepository>();
        _mockRepository
            .Setup(r => r.FindByIdAsync(It.IsAny<Ulid>()))
            .ReturnsAsync(tournamentToReturn);
        _validator = new CuerdaRequestValidator(_mockRepository.Object);
    }

    private static CuerdaRequestDto CreateValidDto(Ulid tournamentId) => new()
    {
        Marca = "Wilson",
        TournamentId = tournamentId,
        Modelo = "Sensation",
        Stock = 10,
        Precio = 25.99,
        StringFormat = "Reel",
        StringsType = "Polyester"
    };

    private static CuerdaRequestDto CreateDtoWithStringFormat(string stringFormat, Ulid tournamentId) => new()
    {
        Marca = "Wilson",
        TournamentId = tournamentId,
        Modelo = "Sensation",
        Stock = 10,
        Precio = 25.99,
        StringFormat = stringFormat,
        StringsType = "Polyester"
    };

    private static CuerdaRequestDto CreateDtoWithStringsType(string stringsType, Ulid tournamentId) => new()
    {
        Marca = "Wilson",
        TournamentId = tournamentId,
        Modelo = "Sensation",
        Stock = 10,
        Precio = 25.99,
        StringFormat = "Reel",
        StringsType = stringsType
    };

    private static CuerdaRequestDto CreateDtoWithTournamentId(Ulid tournamentId) => new()
    {
        Marca = "Wilson",
        TournamentId = tournamentId,
        Modelo = "Sensation",
        Stock = 10,
        Precio = 25.99,
        StringFormat = "Reel",
        StringsType = "Polyester"
    };

    private static CuerdaRequestDto CreateDtoWithTournamentIdNull() => new()
    {
        Marca = "Wilson",
        TournamentId = default,
        Modelo = "Sensation",
        Stock = 10,
        Precio = 25.99,
        StringFormat = "Reel",
        StringsType = "Polyester"
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
            e.PropertyName == nameof(CuerdaRequestDto.TournamentId));
    }

    [Test]
    public async Task Validate_TournamentDoesNotExist_ReturnsError()
    {
        SetupValidator(null);
        var dto = CreateDtoWithTournamentId(Ulid.NewUlid());

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CuerdaRequestDto.TournamentId) &&
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
            e.PropertyName == nameof(CuerdaRequestDto.TournamentId) &&
            e.ErrorMessage.Contains("cancelado"));
    }

    [Test]
    public async Task Validate_StringFormatIsEmpty_ReturnsError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = CreateDtoWithStringFormat("", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CuerdaRequestDto.StringFormat));
    }

    [Test]
    public async Task Validate_StringFormatIsInvalid_ReturnsError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = CreateDtoWithStringFormat("InvalidFormat", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CuerdaRequestDto.StringFormat) &&
            e.ErrorMessage.Contains("inválido"));
    }

    [TestCase("Reel")]
    [TestCase("Set")]
    public async Task Validate_StringFormatIsValid_ReturnsNoError(string stringFormat)
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = CreateDtoWithStringFormat(stringFormat, tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaRequestDto.StringFormat));
    }

    [Test]
    public async Task Validate_StringsTypeIsEmpty_ReturnsError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = CreateDtoWithStringsType("", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CuerdaRequestDto.StringsType));
    }

    [Test]
    public async Task Validate_StringsTypeIsInvalid_ReturnsError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = CreateDtoWithStringsType("InvalidType", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CuerdaRequestDto.StringsType) &&
            e.ErrorMessage.Contains("inválido"));
    }

    [TestCase("Polyester")]
    [TestCase("Multifilament")]
    [TestCase("SyntheticGut")]
    [TestCase("NaturalGut")]
    [TestCase("Hybrid")]
    public async Task Validate_StringsTypeIsValid_ReturnsNoError(string stringsType)
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = CreateDtoWithStringsType(stringsType, tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaRequestDto.StringsType));
    }

    [Test]
    public async Task Validate_StringFormatIsCaseInsensitive_ReturnsNoError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = CreateDtoWithStringFormat("REEL", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaRequestDto.StringFormat));
    }

    [Test]
    public async Task Validate_StringsTypeIsCaseInsensitive_ReturnsNoError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = CreateDtoWithStringsType("POLYESTER", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaRequestDto.StringsType));
    }
}