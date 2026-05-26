using BackEncordados.Materials.Errors;
using FluentAssertions;

namespace TestEncordados.Unit.Errors;

public class CuerdaErrorTests
{
    [Test]
    public void CuerdaError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Cuerda error message";
        var error = new CuerdaError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void ConflictError_InheritsFromCuerdaError()
    {
        var error = new ConflictError("Conflict");

        error.Should().BeAssignableTo<CuerdaError>();
    }

    [Test]
    public void ConflictError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Cuerda already exists";
        var error = new ConflictError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void CuerdaNotFoundError_InheritsFromCuerdaError()
    {
        var error = new CuerdaNotFoundError();

        error.Should().BeAssignableTo<CuerdaError>();
    }

    [Test]
    public void CierraNotFoundError_HasDefaultMessage()
    {
        var error = new CuerdaNotFoundError();

        error.Error.Should().Be("Cuerda not found");
    }

    [Test]
    public void CierraNotFoundError_WithCustomMessage_OverridesDefault()
    {
        const string customMessage = "Cuerda with id 123 not found";
        var error = new CuerdaNotFoundError(customMessage);

        error.Error.Should().Be(customMessage);
    }

    [Test]
    public void ValidationError_InheritsFromCuerdaError()
    {
        var error = new ValidationError("Validation");

        error.Should().BeAssignableTo<CuerdaError>();
    }

    [Test]
    public void ValidationError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Invalid cuerda data";
        var error = new ValidationError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }
}