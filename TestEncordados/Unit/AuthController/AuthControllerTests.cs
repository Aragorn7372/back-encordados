using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Service.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TestEncordados.Unit.AuthController;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly BackEncordados.Usuarios.Controller.AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _controller = new BackEncordados.Usuarios.Controller.AuthController(
            _mockAuthService.Object,
            NullLogger<BackEncordados.Usuarios.Controller.AuthController>.Instance
        );
    }

    private static RegisterDto CreateRegisterDto() => new()
    {
        Username = "testuser",
        Email = "test@example.com",
        Password = "Password123!"
    };

    private static LoginDto CreateLoginDto() => new()
    {
        Username = "testuser",
        Password = "Password123!"
    };

    private static ChangePasswordRequestDto CreateChangePasswordDto() => new()
    {
        NewPassword = "NewPassword123!",
        ConfirmPassword = "NewPassword123!"
    };

    private static AuthResponseDto CreateAuthResponse() => new(
        "jwt_token",
        new UserDto(Ulid.NewUlid(), "testuser", "test@example.com", "USER", DateTime.UtcNow)
    );

    #region SignUp Tests

    [Test]
    public async Task SignUp_ValidDto_ReturnsCreatedResult()
    {
        var dto = CreateRegisterDto();
        var response = CreateAuthResponse();

        _mockAuthService
            .Setup(s => s.SignUpAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<AuthResponseDto, AuthError>(response));

        var result = await _controller.SignUp(dto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().Be(response);
    }

    [Test]
    public async Task SignUp_ValidationError_ReturnsBadRequest()
    {
        var dto = CreateRegisterDto();
        var error = new ValidationError("El nombre de usuario es obligatorio");

        _mockAuthService
            .Setup(s => s.SignUpAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<AuthResponseDto, AuthError>(error));

        var result = await _controller.SignUp(dto);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task SignUp_ConflictError_ReturnsConflict()
    {
        var dto = CreateRegisterDto();
        var error = new ConflictError("El usuario ya existe");

        _mockAuthService
            .Setup(s => s.SignUpAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<AuthResponseDto, AuthError>(error));

        var result = await _controller.SignUp(dto);

        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task SignUp_UnexpectedError_ReturnsInternalServerError()
    {
        var dto = CreateRegisterDto();
        var error = new AuthError("Error inesperado");

        _mockAuthService
            .Setup(s => s.SignUpAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<AuthResponseDto, AuthError>(error));

        var result = await _controller.SignUp(dto);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region SignIn Tests

    [Test]
    public async Task SignIn_ValidCredentials_ReturnsOkWithToken()
    {
        var dto = CreateLoginDto();
        var response = CreateAuthResponse();

        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<AuthResponseDto, AuthError>(response));

        var result = await _controller.SignIn(dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task SignIn_InvalidCredentials_ReturnsUnauthorized()
    {
        var dto = CreateLoginDto();
        var error = new UnauthorizedError("Credenciales inválidas");

        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<AuthResponseDto, AuthError>(error));

        var result = await _controller.SignIn(dto);

        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Test]
    public async Task SignIn_ValidationError_ReturnsBadRequest()
    {
        var dto = CreateLoginDto();
        var error = new ValidationError("El nombre de usuario es obligatorio");

        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<AuthResponseDto, AuthError>(error));

        var result = await _controller.SignIn(dto);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task SignIn_UnexpectedError_ReturnsInternalServerError()
    {
        var dto = CreateLoginDto();
        var error = new AuthError("Error inesperado");

        _mockAuthService
            .Setup(s => s.SignInAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<AuthResponseDto, AuthError>(error));

        var result = await _controller.SignIn(dto);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region SentEmailRequest Tests

    [Test]
    public async Task SentEmailRequest_ValidEmail_ReturnsOk()
    {
        var email = "test@example.com";

        _mockAuthService
            .Setup(s => s.GetEmailAsync(email))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<global::BackEncordados.Common.Utils.Unit, AuthError>(global::BackEncordados.Common.Utils.Unit.Value));

        var result = await _controller.SentEmailRequest(email);

        result.Should().BeOfType<OkResult>();
    }

    [Test]
    public async Task SentEmailRequest_UserNotFound_ReturnsNotFound()
    {
        var email = "nonexistent@example.com";
        var error = new UserNotFoundError("Usuario no encontrado");

        _mockAuthService
            .Setup(s => s.GetEmailAsync(email))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, AuthError>(error));

        var result = await _controller.SentEmailRequest(email);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task SentEmailRequest_UnexpectedError_ReturnsInternalServerError()
    {
        var email = "test@example.com";
        var error = new AuthError("Error inesperado");

        _mockAuthService
            .Setup(s => s.GetEmailAsync(email))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, AuthError>(error));

        var result = await _controller.SentEmailRequest(email);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region ChangePassword Tests

    [Test]
    public async Task ChangePassword_ValidRequest_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        var dto = CreateChangePasswordDto();

        _mockAuthService
            .Setup(s => s.ChangePasswordAsync(userId, It.IsAny<ChangePasswordRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<global::BackEncordados.Common.Utils.Unit, AuthError>(global::BackEncordados.Common.Utils.Unit.Value));

        var result = await _controller.ChangePassword(userId, dto);

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task ChangePassword_ExpiredTimeout_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        var dto = CreateChangePasswordDto();
        var error = new PasswordChangeExpiredTimeout();

        _mockAuthService
            .Setup(s => s.ChangePasswordAsync(userId, It.IsAny<ChangePasswordRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, AuthError>(error));

        var result = await _controller.ChangePassword(userId, dto);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task ChangePassword_UserNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var dto = CreateChangePasswordDto();
        var error = new UserNotFoundError("Usuario no encontrado");

        _mockAuthService
            .Setup(s => s.ChangePasswordAsync(userId, It.IsAny<ChangePasswordRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, AuthError>(error));

        var result = await _controller.ChangePassword(userId, dto);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task ChangePassword_ValidationError_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        var dto = CreateChangePasswordDto();
        var error = new ValidationError("Las contraseñas no coinciden");

        _mockAuthService
            .Setup(s => s.ChangePasswordAsync(userId, It.IsAny<ChangePasswordRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, AuthError>(error));

        var result = await _controller.ChangePassword(userId, dto);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task ChangePassword_UnexpectedError_ReturnsInternalServerError()
    {
        var userId = Guid.NewGuid();
        var dto = CreateChangePasswordDto();
        var error = new AuthError("Error inesperado");

        _mockAuthService
            .Setup(s => s.ChangePasswordAsync(userId, It.IsAny<ChangePasswordRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, AuthError>(error));

        var result = await _controller.ChangePassword(userId, dto);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion
}