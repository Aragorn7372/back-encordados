using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Validator.Strings;
using BackEncordados.Talleres.Model;
using BackEncordados.Talleres.Repository;
using FluentAssertions;
using Moq;

namespace TestEncordados.Unit.Validators.Cuerdas;

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

    private static CuerdaRequestDto ValidDto(Ulid tournamentId) => new()
    {
        Marca = "Babolat",
        TournamentId = tournamentId,
        Modelo = "Pro Tour",
        Stock = 10,
        Precio = 25.99,
        Calibre = 1.25,
        StringFormat = "Reel",
        StringsType = "Polyester"
    };

    private static CuerdaRequestDto DtoWithStringFormat(string format, Ulid tournamentId) => new()
    {
        Marca = "Babolat",
        TournamentId = tournamentId,
        Modelo = "Pro Tour",
        Stock = 10,
        Precio = 25.99,
        Calibre = 1.25,
        StringFormat = format,
        StringsType = "Polyester"
    };

    private static CuerdaRequestDto DtoWithStringsType(string stringsType, Ulid tournamentId) => new()
    {
        Marca = "Babolat",
        TournamentId = tournamentId,
        Modelo = "Pro Tour",
        Stock = 10,
        Precio = 25.99,
        Calibre = 1.25,
        StringFormat = "Reel",
        StringsType = stringsType
    };

    private static CuerdaRequestDto DtoWithTournamentId(Ulid tournamentId) => new()
    {
        Marca = "Babolat",
        TournamentId = tournamentId,
        Modelo = "Pro Tour",
        Stock = 10,
        Precio = 25.99,
        Calibre = 1.25,
        StringFormat = "Reel",
        StringsType = "Polyester"
    };

    [Test]
    public async Task Validate_AllValidWithExistingTournament_ReturnsNoErrors()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);

        var dto = ValidDto(tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task Validate_TournamentIdIsEmpty_ReturnsError()
    {
        SetupValidator(null);
        var dto = new CuerdaRequestDto
        {
            Marca = "Babolat",
            TournamentId = default,
            Modelo = "Pro Tour",
            Stock = 10,
            Precio = 25.99,
            Calibre = 1.25,
            StringFormat = "Reel",
            StringsType = "Polyester"
        };

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CuerdaRequestDto.TournamentId));
    }

    [Test]
    public async Task Validate_TournamentDoesNotExist_ReturnsError()
    {
        SetupValidator(null);
        var dto = DtoWithTournamentId(Ulid.NewUlid());

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

        var dto = DtoWithTournamentId(tournamentId);

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
        var dto = new CuerdaRequestDto
        {
            Marca = "Babolat",
            TournamentId = tournamentId,
            Modelo = "Pro Tour",
            Stock = 10,
            Precio = 25.99,
            Calibre = 1.25,
            StringFormat = "",
            StringsType = "Polyester"
        };

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CuerdaRequestDto.StringFormat));
    }

    [Test]
    public async Task Validate_StringFormatIsInvalid_ReturnsError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = DtoWithStringFormat("InvalidFormat", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CuerdaRequestDto.StringFormat) &&
            e.ErrorMessage.Contains("inválido"));
    }

    [Test]
    public async Task Validate_StringsTypeIsEmpty_ReturnsError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = new CuerdaRequestDto
        {
            Marca = "Babolat",
            TournamentId = tournamentId,
            Modelo = "Pro Tour",
            Stock = 10,
            Precio = 25.99,
            Calibre = 1.25,
            StringFormat = "Reel",
            StringsType = ""
        };

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CuerdaRequestDto.StringsType));
    }

    [Test]
    public async Task Validate_StringsTypeIsInvalid_ReturnsError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = DtoWithStringsType("InvalidType", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CuerdaRequestDto.StringsType) &&
            e.ErrorMessage.Contains("inválido"));
    }

    [Test]
    public async Task Validate_AllValidStringFormatValues_ReturnsNoErrors()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);

        var validFormats = new[] { "Reel", "Set" };

        foreach (var format in validFormats)
        {
            var dto = DtoWithStringFormat(format, tournamentId);
            var result = await _validator.ValidateAsync(dto);

            result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaRequestDto.StringFormat));
        }
    }

    [Test]
    public async Task Validate_AllValidStringsTypeValues_ReturnsNoErrors()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);

        var validTypes = new[] { "Polyester", "SyntheticGut", "NaturalGut", "Multifilament", "Hybrid" };

        foreach (var type in validTypes)
        {
            var dto = DtoWithStringsType(type, tournamentId);
            var result = await _validator.ValidateAsync(dto);

            result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaRequestDto.StringsType));
        }
    }

    [Test]
    public async Task Validate_StringFormatIsCaseInsensitive_ReturnsNoError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = DtoWithStringFormat("REEL", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaRequestDto.StringFormat));
    }

    [Test]
    public async Task Validate_StringsTypeIsCaseInsensitive_ReturnsNoError()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = new Tournaments { Id = tournamentId, IsDeleted = false };
        SetupValidator(tournament);
        var dto = DtoWithStringsType("POLYESTER", tournamentId);

        var result = await _validator.ValidateAsync(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaRequestDto.StringsType));
    }
}