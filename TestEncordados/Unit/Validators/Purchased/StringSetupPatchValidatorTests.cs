using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Validator;
using FluentAssertions;

namespace TestEncordados.Unit.Validators.Purchased;

public class StringSetupPatchValidatorTests
{
    private StringSetupPatchValidator CreateValidator() => new();

    private static StringSetupDto CreateValidDto() => new()
    {
        StringV = "Synthetic Gut",
        TensionV = 20.0,
        PreStetchV = 10,
        StringH = "Natural Gut",
        TensionH = 18.0,
        PreStetchH = 5
    };

    private static StringSetupDto DtoWithStringV(string stringV) => new()
    {
        StringV = stringV,
        TensionV = 20.0,
        PreStetchV = 10
    };

    private static StringSetupDto DtoWithTensionV(double tensionV) => new()
    {
        StringV = "Synthetic Gut",
        TensionV = tensionV,
        PreStetchV = 10
    };

    private static StringSetupDto DtoWithPreStetchV(short preStetchV) => new()
    {
        StringV = "Synthetic Gut",
        TensionV = 20.0,
        PreStetchV = preStetchV
    };

    private static StringSetupDto DtoWithStringH(string stringH) => new()
    {
        StringV = "Synthetic Gut",
        TensionV = 20.0,
        PreStetchV = 10,
        StringH = stringH
    };

    private static StringSetupDto DtoWithTensionH(double tensionH) => new()
    {
        StringV = "Synthetic Gut",
        TensionV = 20.0,
        PreStetchV = 10,
        TensionH = tensionH
    };

    private static StringSetupDto DtoWithPreStetchH(short preStetchH) => new()
    {
        StringV = "Synthetic Gut",
        TensionV = 20.0,
        PreStetchV = 10,
        PreStetchH = preStetchH
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
    public void Validate_StringVIsEmpty_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringV("");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StringSetupDto.StringV));
    }

    [Test]
    public void Validate_StringVExceedsMaxLength_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringV(new string('a', 101));

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StringSetupDto.StringV) &&
            e.ErrorMessage.Contains("100"));
    }

    [Test]
    public void Validate_TensionVBelowMin_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithTensionV(4.9);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StringSetupDto.TensionV) &&
            e.ErrorMessage.Contains("5"));
    }

    [Test]
    public void Validate_TensionVAboveMax_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithTensionV(40.1);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StringSetupDto.TensionV) &&
            e.ErrorMessage.Contains("40"));
    }

    [Test]
    public void Validate_TensionVAtMinBoundary_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithTensionV(5.0);

        var result = validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(StringSetupDto.TensionV));
    }

    [Test]
    public void Validate_TensionVAtMaxBoundary_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithTensionV(40.0);

        var result = validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(StringSetupDto.TensionV));
    }

    [Test]
    public void Validate_PreStetchVBelowMin_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithPreStetchV(-1);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StringSetupDto.PreStetchV) &&
            e.ErrorMessage.Contains("0"));
    }

    [Test]
    public void Validate_PreStetchVAboveMax_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithPreStetchV(21);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StringSetupDto.PreStetchV) &&
            e.ErrorMessage.Contains("20"));
    }

    [Test]
    public void Validate_PreStetchVAtMinBoundary_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithPreStetchV(0);

        var result = validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(StringSetupDto.PreStetchV));
    }

    [Test]
    public void Validate_PreStetchVAtMaxBoundary_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithPreStetchV(20);

        var result = validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(StringSetupDto.PreStetchV));
    }

    [Test]
    public void Validate_StringHExceedsMaxLength_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithStringH(new string('a', 101));

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StringSetupDto.StringH) &&
            e.ErrorMessage.Contains("100"));
    }

    [Test]
    public void Validate_TensionHIsZero_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithTensionH(0);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(StringSetupDto.TensionH));
    }

    [Test]
    public void Validate_TensionHBelowMin_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithTensionH(4.9);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StringSetupDto.TensionH) &&
            e.ErrorMessage.Contains("5"));
    }

    [Test]
    public void Validate_TensionHAboveMax_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithTensionH(40.1);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StringSetupDto.TensionH) &&
            e.ErrorMessage.Contains("40"));
    }

    [Test]
    public void Validate_PreStetchHIsZero_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithPreStetchH(0);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(StringSetupDto.PreStetchH));
    }

    [Test]
    public void Validate_PreStetchHBelowMin_ValidationNotTriggered_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithPreStetchH(-1);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(StringSetupDto.PreStetchH));
    }

    [Test]
    public void Validate_PreStetchHAboveMax_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithPreStetchH(21);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StringSetupDto.PreStetchH) &&
            e.ErrorMessage.Contains("20"));
    }
}