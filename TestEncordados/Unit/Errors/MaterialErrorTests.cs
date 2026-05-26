using BackEncordados.Materials.Errors;
using FluentAssertions;

namespace TestEncordados.Unit.Errors;

public class MaterialErrorTests
{
    [Test]
    public void MaterialError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Material error message";
        var error = new MaterialError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void MaterialConflictError_InheritsFromMaterialError()
    {
        var error = new MaterialConflictError("Conflict");

        error.Should().BeAssignableTo<MaterialError>();
    }

    [Test]
    public void MaterialConflictError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Material already exists";
        var error = new MaterialConflictError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void MaterialNotFoundError_InheritsFromMaterialError()
    {
        var error = new MaterialNotFoundError();

        error.Should().BeAssignableTo<MaterialError>();
    }

    [Test]
    public void MaterialNotFoundError_HasDefaultMessage()
    {
        var error = new MaterialNotFoundError();

        error.Error.Should().Be("Material not found");
    }

    [Test]
    public void MaterialNotFoundError_WithCustomMessage_OverridesDefault()
    {
        const string customMessage = "Material with id 123 not found";
        var error = new MaterialNotFoundError(customMessage);

        error.Error.Should().Be(customMessage);
    }

    [Test]
    public void MaterialValidationError_InheritsFromMaterialError()
    {
        var error = new MaterialValidationError("Validation");

        error.Should().BeAssignableTo<MaterialError>();
    }

    [Test]
    public void MaterialValidationError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Invalid material data";
        var error = new MaterialValidationError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }
}