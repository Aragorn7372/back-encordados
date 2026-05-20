using BackEncordados.Common.Errors;
using BackEncordados.Purchased.Errors;
using FluentAssertions;

namespace TestEncordados.Unit.Errors;

public class PurchasedErrorsTests
{
    [Test]
    public void PurchasedErrors_InheritsFromDomainErrors()
    {
        var error = new PurchasedErrors("test");

        error.Should().BeAssignableTo<DomainErrors>();
    }

    [Test]
    public void PurchasedErrors_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Purchased error message";
        var error = new PurchasedErrors(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void ConflictError_InheritsFromPurchasedErrors()
    {
        var error = new ConflictError("Conflict");

        error.Should().BeAssignableTo<PurchasedErrors>();
    }

    [Test]
    public void ConflictError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Purchased conflict message";
        var error = new ConflictError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void PurchasedNotFoundError_InheritsFromPurchasedErrors()
    {
        var error = new PurchasedNotFoundError();

        error.Should().BeAssignableTo<PurchasedErrors>();
    }

    [Test]
    public void PurchasedNotFoundError_HasDefaultMessage()
    {
        var error = new PurchasedNotFoundError();

        error.Error.Should().Be("Purchased not found");
    }

    [Test]
    public void PurchasedNotFoundError_WithCustomMessage_OverridesDefault()
    {
        const string customMessage = "Purchased with id 123 not found";
        var error = new PurchasedNotFoundError(customMessage);

        error.Error.Should().Be(customMessage);
    }

    [Test]
    public void ValidationError_InheritsFromPurchasedErrors()
    {
        var error = new ValidationError("Validation");

        error.Should().BeAssignableTo<PurchasedErrors>();
    }

    [Test]
    public void ValidationError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Invalid purchased data";
        var error = new ValidationError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void InvalidStatusError_InheritsFromPurchasedErrors()
    {
        var error = new InvalidStatusError("Invalid status");

        error.Should().BeAssignableTo<PurchasedErrors>();
    }

    [Test]
    public void InvalidStatusError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Cannot change from pending to cancelled";
        var error = new InvalidStatusError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void ConcurrencyError_InheritsFromPurchasedErrors()
    {
        var error = new ConcurrencyError();

        error.Should().BeAssignableTo<PurchasedErrors>();
    }

    [Test]
    public void ConcurrencyError_HasDefaultMessage()
    {
        var error = new ConcurrencyError();

        error.Error.Should().Be("El usuario fue modificado por otra operación. Intente de nuevo.");
    }

    [Test]
    public void ConcurrencyError_WithCustomMessage_OverridesDefault()
    {
        const string customMessage = "Record was modified";
        var error = new ConcurrencyError(customMessage);

        error.Error.Should().Be(customMessage);
    }
}