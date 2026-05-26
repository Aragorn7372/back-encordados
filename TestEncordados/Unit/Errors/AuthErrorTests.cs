using BackEncordados.Common.Errors;
using BackEncordados.Usuarios.Errors;
using FluentAssertions;

namespace TestEncordados.Unit.Errors;

public class AuthErrorTests
{
    [Test]
    public void AuthError_InheritsFromDomainErrors()
    {
        var error = new AuthError("test");

        error.Should().BeAssignableTo<DomainErrors>();
    }

    [Test]
    public void AuthError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Authentication failed";
        var error = new AuthError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void UnauthorizedError_InheritsFromAuthError()
    {
        var error = new UnauthorizedError("Unauthorized");

        error.Should().BeAssignableTo<AuthError>();
    }

    [Test]
    public void UnauthorizedError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Invalid credentials";
        var error = new UnauthorizedError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void ConflictError_InheritsFromAuthError()
    {
        var error = new ConflictError("Conflict");

        error.Should().BeAssignableTo<AuthError>();
    }

    [Test]
    public void ConflictError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Username already exists";
        var error = new ConflictError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void UserNotFoundError_InheritsFromAuthError()
    {
        var error = new UserNotFoundError("User not found");

        error.Should().BeAssignableTo<AuthError>();
    }

    [Test]
    public void UserNotFoundError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "User with id 123 not found";
        var error = new UserNotFoundError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void ValidationError_InheritsFromAuthError()
    {
        var error = new ValidationError("Validation");

        error.Should().BeAssignableTo<AuthError>();
    }

    [Test]
    public void ValidationError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Invalid email format";
        var error = new ValidationError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void PasswordChangeExpiredTimeout_InheritsFromAuthError()
    {
        var error = new PasswordChangeExpiredTimeout();

        error.Should().BeAssignableTo<AuthError>();
    }

    [Test]
    public void PasswordChangeExpiredTimeout_HasDefaultMessage()
    {
        var error = new PasswordChangeExpiredTimeout();

        error.Error.Should().Be("el para cambiar la contraseña expiró o no se ha encontrado el usuario vuelva a intentarlo en otro momento o vuelva a solicitar el cambio de contraseña");
    }

    [Test]
    public void PasswordChangeExpiredTimeout_WithCustomMessage_OverridesDefault()
    {
        const string customMessage = "Password change timeout";
        var error = new PasswordChangeExpiredTimeout(customMessage);

        error.Error.Should().Be(customMessage);
    }
}