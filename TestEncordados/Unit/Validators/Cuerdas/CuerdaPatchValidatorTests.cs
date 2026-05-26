using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Validator.Strings;
using FluentAssertions;

namespace TestEncordados.Unit.Validators.Cuerdas;

public class CuerdaPatchValidatorTests
{
    private CuerdaPatchValidator CreateValidator() => new();

    private static CuerdaPatchDto ValidDto() => new()
    {
        Marca = "Babolat",
        Modelo = "Pro Tour",
        Stock = 10,
        Precio = 25.99,
        Calibre = 1.25,
        StringFormat = "Reel",
        StringsType = "Polyester"
    };

    private static CuerdaPatchDto DtoWithStock(int stock) => new()
    {
        Stock = stock
    };

    private static CuerdaPatchDto DtoWithPrecio(double precio) => new()
    {
        Precio = precio
    };

    private static CuerdaPatchDto DtoWithStringFormat(string? format) => new()
    {
        StringFormat = format ?? ""
    };

    private static CuerdaPatchDto DtoWithStringsType(string? type) => new()
    {
        StringsType = type ?? ""
    };

    [Test]
    public void Validate_AllOptionalFieldsNull_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = new CuerdaPatchDto();

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_StockIsZero_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithStock(0);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaPatchDto.Stock));
    }

    [Test]
    public void Validate_StockIsNegative_ValidationNotTriggered_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithStock(-1);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaPatchDto.Stock));
    }

    [Test]
    public void Validate_PrecioIsPositive_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithPrecio(25.99);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaPatchDto.Precio));
    }

    [Test]
    public void Validate_PrecioIsZero_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithPrecio(0);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CuerdaPatchDto.Precio) &&
            e.ErrorMessage.Contains("mayor"));
    }

    [Test]
    public void Validate_PrecioIsNegative_ValidationNotTriggered_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithPrecio(-10.0);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaPatchDto.Precio));
    }

    [Test]
    public void Validate_StringFormatIsNull_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringFormat(null);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaPatchDto.StringFormat));
    }

    [Test]
    public void Validate_StringFormatIsEmpty_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringFormat("");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaPatchDto.StringFormat));
    }

    [Test]
    public void Validate_StringFormatIsValid_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringFormat("Reel");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaPatchDto.StringFormat));
    }

    [Test]
    public void Validate_StringFormatIsInvalid_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringFormat("InvalidFormat");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CuerdaPatchDto.StringFormat) &&
            e.ErrorMessage.Contains("inválido"));
    }

    [Test]
    public void Validate_StringsTypeIsNull_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringsType(null);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaPatchDto.StringsType));
    }

    [Test]
    public void Validate_StringsTypeIsEmpty_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringsType("");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaPatchDto.StringsType));
    }

    [Test]
    public void Validate_StringsTypeIsValid_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringsType("Polyester");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaPatchDto.StringsType));
    }

    [Test]
    public void Validate_StringsTypeIsInvalid_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringsType("InvalidType");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CuerdaPatchDto.StringsType) &&
            e.ErrorMessage.Contains("inválido"));
    }

    [Test]
    public void Validate_StringFormatIsCaseInsensitive_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringFormat("REEL");

        var result = validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaPatchDto.StringFormat));
    }

    [Test]
    public void Validate_StringsTypeIsCaseInsensitive_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringsType("POLYESTER");

        var result = validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CuerdaPatchDto.StringsType));
    }
}