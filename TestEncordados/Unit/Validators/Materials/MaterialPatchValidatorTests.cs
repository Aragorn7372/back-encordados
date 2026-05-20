using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Validator.Materials;
using FluentAssertions;

namespace TestEncordados.Unit.Validators.Materials;

public class MaterialPatchValidatorTests
{
    private MaterialPatchValidator CreateValidator() => new();

    private static MaterialPatchDto CreateDto(int stock, double precio, string? type = null)
    {
        return new MaterialPatchDto
        {
            Stock = stock,
            Precio = precio,
            Type = type ?? ""
        };
    }

    [Test]
    public void Validate_OnlyStockProvided_PassesValidation()
    {
        var validator = CreateValidator();
        var dto = CreateDto(stock: 0, precio: 35.99);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_OnlyPrecioProvided_PassesValidation()
    {
        var validator = CreateValidator();
        var dto = CreateDto(stock: 10, precio: 25.0);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_OnlyTypeProvided_PassesValidation()
    {
        var validator = CreateValidator();
        var dto = CreateDto(stock: 10, precio: 25.0, type: "Grip");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_TypeIsEmpty_PassesValidation()
    {
        var validator = CreateValidator();
        var dto = CreateDto(stock: 10, precio: 25.0, type: "");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_TypeIsValid_PassesValidation()
    {
        var validator = CreateValidator();
        var dto = CreateDto(stock: 10, precio: 25.0, type: "Grip");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(MaterialPatchDto.Type));
    }

    [Test]
    public void Validate_TypeIsInvalid_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = CreateDto(stock: 10, precio: 25.0, type: "InvalidType");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(MaterialPatchDto.Type) &&
            e.ErrorMessage.Contains("inválido"));
    }

    [Test]
    public void Validate_TypeIsCaseInsensitive_PassesValidation()
    {
        var validator = CreateValidator();
        var dto = CreateDto(stock: 10, precio: 25.0, type: "GRIP");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(MaterialPatchDto.Type));
    }

    [Test]
    public void Validate_AllValidEnumTypes_PassValidation()
    {
        var validator = CreateValidator();
        var validTypes = new[] { "Grip", "Overgrip", "LeadTape", "Silicone", "Otro" };

        foreach (var type in validTypes)
        {
            var dto = CreateDto(stock: 10, precio: 25.0, type: type);
            var result = validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }
    }

    [Test]
    public void Validate_InvalidTypeAndValidStock_PassesValidation()
    {
        var validator = CreateValidator();
        var dto = new MaterialPatchDto
        {
            Stock = 10,
            Precio = 25.0,
            Type = "InvalidType"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(MaterialPatchDto.Type));
    }

    [Test]
    public void Validate_ValidTypeAndValidPrecio_PassesValidation()
    {
        var validator = CreateValidator();
        var dto = new MaterialPatchDto
        {
            Stock = 10,
            Precio = 25.0,
            Type = "Grip"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }
}