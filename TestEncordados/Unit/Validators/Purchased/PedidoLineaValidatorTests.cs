using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Validator;
using FluentAssertions;

namespace TestEncordados.Unit.Validators.Purchased;

public class PedidoLineaValidatorTests
{
    private PedidoLineaRequestValidator CreateValidator() => new();

    private static PedidoLineaRequestDto CreateValidDto() => new()
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
    };

    private static PedidoLineaRequestDto CreateDtoWithRaquetModel(string raquetModel) => new()
    {
        RaquetModel = raquetModel,
        Nudos = 4,
        DateString = DateTime.UtcNow.AddDays(1),
        Logotype = true,
        Color = "Negro",
        StringSetup = new StringSetupDto
        {
            StringV = "Synthetic Gut",
            TensionV = 20.0,
            PreStetchV = 10
        }
    };

    private static PedidoLineaRequestDto CreateDtoWithNudos(byte nudos) => new()
    {
        RaquetModel = "Wilson Pro Staff",
        Nudos = nudos,
        DateString = DateTime.UtcNow.AddDays(1),
        Logotype = true,
        Color = "Negro",
        StringSetup = new StringSetupDto
        {
            StringV = "Synthetic Gut",
            TensionV = 20.0,
            PreStetchV = 10
        }
    };

    private static PedidoLineaRequestDto CreateDtoWithDateString(DateTime dateString) => new()
    {
        RaquetModel = "Wilson Pro Staff",
        Nudos = 4,
        DateString = dateString,
        Logotype = true,
        Color = "Negro",
        StringSetup = new StringSetupDto
        {
            StringV = "Synthetic Gut",
            TensionV = 20.0,
            PreStetchV = 10
        }
    };

    private static PedidoLineaRequestDto CreateDtoWithColor(string color) => new()
    {
        RaquetModel = "Wilson Pro Staff",
        Nudos = 4,
        DateString = DateTime.UtcNow.AddDays(1),
        Logotype = true,
        Color = color,
        StringSetup = new StringSetupDto
        {
            StringV = "Synthetic Gut",
            TensionV = 20.0,
            PreStetchV = 10
        }
    };

    private static PedidoLineaRequestDto CreateDtoWithStringSetup(StringSetupDto? stringSetup) => new()
    {
        RaquetModel = "Wilson Pro Staff",
        Nudos = 4,
        DateString = DateTime.UtcNow.AddDays(1),
        Logotype = true,
        Color = "Negro",
        StringSetup = stringSetup
    };

    [Test]
    public void Validate_AllValidFields_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = CreateValidDto();

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_RaquetModelIsEmpty_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = CreateDtoWithRaquetModel("");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PedidoLineaRequestDto.RaquetModel));
    }

    [Test]
    public void Validate_RaquetModelExceedsMaxLength_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = CreateDtoWithRaquetModel(new string('a', 201));

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PedidoLineaRequestDto.RaquetModel) &&
            e.ErrorMessage.Contains("200"));
    }

    [TestCase((byte)0)]
    [TestCase((byte)1)]
    [TestCase((byte)3)]
    [TestCase((byte)5)]
    public void Validate_NudosAreInvalid_ReturnsError(byte nudos)
    {
        var validator = CreateValidator();
        var dto = CreateDtoWithNudos(nudos);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PedidoLineaRequestDto.Nudos) &&
            e.ErrorMessage.Contains("nudos"));
    }

    [TestCase((byte)2)]
    [TestCase((byte)4)]
    public void Validate_NudosAreValid_ReturnsNoError(byte nudos)
    {
        var validator = CreateValidator();
        var dto = CreateDtoWithNudos(nudos);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_DateStringIsDefault_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = CreateDtoWithDateString(default);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PedidoLineaRequestDto.DateString));
    }

    [Test]
    public void Validate_ColorExceedsMaxLength_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = CreateDtoWithColor(new string('a', 101));

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PedidoLineaRequestDto.Color) &&
            e.ErrorMessage.Contains("100"));
    }

    [Test]
    public void Validate_StringSetupIsNull_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = CreateDtoWithStringSetup(null);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PedidoLineaRequestDto.StringSetup));
    }

    [Test]
    public void Validate_StringSetupIsInvalid_ReturnsErrors()
    {
        var validator = CreateValidator();
        var dto = CreateDtoWithStringSetup(new StringSetupDto
        {
            StringV = "",
            TensionV = 50.0,
            PreStetchV = 25
        });

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}