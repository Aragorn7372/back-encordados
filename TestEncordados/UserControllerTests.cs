using BackEncordados.Common.Dto;
using BackEncordados.Usuarios.Controller;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Service.CrudService;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace TestEncordados;

public class UserControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _controller = new UserController(
            NullLogger<UserController>.Instance,
            _mockUserService.Object
        );
    }

    private void SetupUserClaims(Ulid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private static UserResponseDto CreateUserResponse() => new(
        "testuser",
        "https://example.com/avatar.jpg",
        "Test User",
        100.0
    );

    private static UserWithIdDto CreateUserWithIdDto() => new(
        Ulid.NewUlid().ToString(),
        "testuser",
        "https://example.com/avatar.jpg",
        "Test User",
        "test@example.com",
        "player",
        null
    );

    private static PageResponseDto<UserWithIdDto> CreatePageResponse() => new(
        new List<UserWithIdDto> { CreateUserWithIdDto() },
        1,
        1,
        10,
        0,
        1,
        "id",
        "asc"
    );

    #region GetAll Tests

    [Test]
    public async Task GetAll_ReturnsOkWithPageResponse()
    {
        var response = CreatePageResponse();

        _mockUserService
            .Setup(s => s.GetAllUsersAsync(It.IsAny<FilterUserDto>()))
            .ReturnsAsync(response);

        var result = await _controller.GetAll(null, null, null, null, "id", 0, 10, "asc", "");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    #endregion

    #region GetSupervisors Tests

    [Test]
    public async Task GetSupervisors_ReturnsOkWithPageResponse()
    {
        var response = CreatePageResponse();

        _mockUserService
            .Setup(s => s.GetAllUsersAsync(It.IsAny<FilterUserDto>()))
            .ReturnsAsync(response);

        var result = await _controller.GetSupervisors("id", 0, 10, "asc", "");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    #endregion

    #region GetAllEncorders Tests

    [Test]
    public async Task GetAllEncorders_ReturnsOkWithPageResponse()
    {
        var response = CreatePageResponse();

        _mockUserService
            .Setup(s => s.GetAllUsersAsync(It.IsAny<FilterUserDto>()))
            .ReturnsAsync(response);

        var result = await _controller.GetAllEncorders("id", 0, 10, "asc", "");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    #endregion

    #region GetAllUsers (by tournament) Tests

    [Test]
    public async Task GetAllUsersByTournament_ReturnsOkWithPageResponse()
    {
        var tournamentId = Ulid.NewUlid();
        var response = CreatePageResponse();

        _mockUserService
            .Setup(s => s.GetAllUsersAsync(It.IsAny<FilterUserDto>()))
            .ReturnsAsync(response);

        var result = await _controller.GetAllUsers(tournamentId, "id", 0, 10, "asc", "");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    #endregion

    #region GetById Tests

    [Test]
    public async Task GetById_UserExists_ReturnsOkWithUser()
    {
        var userId = Ulid.NewUlid();
        var user = CreateUserResponse();

        _mockUserService
            .Setup(s => s.FindByIdAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<UserResponseDto, AuthError>(user));

        var result = await _controller.GetById(userId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(user);
    }

    [Test]
    public async Task GetById_UserNotFound_ReturnsNotFound()
    {
        var userId = Ulid.NewUlid();
        var error = new UserNotFoundError("Usuario no encontrado");

        _mockUserService
            .Setup(s => s.FindByIdAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.GetById(userId);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task GetById_UnexpectedError_ReturnsInternalServerError()
    {
        var userId = Ulid.NewUlid();
        var error = new AuthError("Error inesperado");

        _mockUserService
            .Setup(s => s.FindByIdAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.GetById(userId);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetMe Tests

    [Test]
    public async Task GetMe_UserExists_ReturnsOkWithUser()
    {
        var userId = Ulid.NewUlid();
        SetupUserClaims(userId);
        var user = CreateUserResponse();

        _mockUserService
            .Setup(s => s.FindByIdAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<UserResponseDto, AuthError>(user));

        var result = await _controller.GetMe();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(user);
    }

    [Test]
    public async Task GetMe_UserNotFound_ReturnsNotFound()
    {
        var userId = Ulid.NewUlid();
        SetupUserClaims(userId);
        var error = new UserNotFoundError("Usuario no encontrado");

        _mockUserService
            .Setup(s => s.FindByIdAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.GetMe();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetMe_NoUserIdClaim_ReturnsNotFound()
    {
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.GetMe();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetMe_InvalidUserIdClaim_ReturnsNotFound()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-ulid-value")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.GetMe();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetMe_UnexpectedError_ReturnsInternalServerError()
    {
        var userId = Ulid.NewUlid();
        SetupUserClaims(userId);
        var error = new AuthError("Error inesperado");

        _mockUserService
            .Setup(s => s.FindByIdAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.GetMe();

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region PatchMe Tests

    [Test]
    public async Task PatchMe_ValidRequest_ReturnsOkWithUser()
    {
        var userId = Ulid.NewUlid();
        SetupUserClaims(userId);
        var request = new UserRequestDto { Name = "New Name", Username = "newuser" };
        var user = CreateUserResponse();

        _mockUserService
            .Setup(s => s.PatchUserAsync(userId, It.IsAny<UserRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<UserResponseDto, AuthError>(user));

        var result = await _controller.PatchMe(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(user);
    }

    [Test]
    public async Task PatchMe_InvalidUserIdClaim_ReturnsNotFound()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-ulid-value")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
        var request = new UserRequestDto();

        var result = await _controller.PatchMe(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task PatchMe_ValidationError_ReturnsBadRequest()
    {
        var userId = Ulid.NewUlid();
        SetupUserClaims(userId);
        var request = new UserRequestDto();
        var error = new ValidationError("Error de validación");

        _mockUserService
            .Setup(s => s.PatchUserAsync(userId, It.IsAny<UserRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.PatchMe(request);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task PatchMe_UserNotFound_ReturnsNotFound()
    {
        var userId = Ulid.NewUlid();
        SetupUserClaims(userId);
        var request = new UserRequestDto();
        var error = new UserNotFoundError("Usuario no encontrado");

        _mockUserService
            .Setup(s => s.PatchUserAsync(userId, It.IsAny<UserRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.PatchMe(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task PatchMe_ConflictError_ReturnsConflict()
    {
        var userId = Ulid.NewUlid();
        SetupUserClaims(userId);
        var request = new UserRequestDto();
        var error = new ConflictError("Conflicto de datos");

        _mockUserService
            .Setup(s => s.PatchUserAsync(userId, It.IsAny<UserRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.PatchMe(request);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task PatchMe_UnexpectedError_ReturnsInternalServerError()
    {
        var userId = Ulid.NewUlid();
        SetupUserClaims(userId);
        var request = new UserRequestDto();
        var error = new AuthError("Error inesperado");

        _mockUserService
            .Setup(s => s.PatchUserAsync(userId, It.IsAny<UserRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.PatchMe(request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Delete Tests

    [Test]
    public async Task Delete_UserExists_ReturnsNoContent()
    {
        var userId = Ulid.NewUlid();

        _mockUserService
            .Setup(s => s.DeleteUserAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<global::BackEncordados.Common.Utils.Unit, AuthError>(global::BackEncordados.Common.Utils.Unit.Value));

        var result = await _controller.Delete(userId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task Delete_ExceptionThrown_ReturnsInternalServerError()
    {
        var userId = Ulid.NewUlid();

        _mockUserService
            .Setup(s => s.DeleteUserAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        var result = await _controller.Delete(userId);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region DeleteMe Tests

    [Test]
    public async Task DeleteMe_ReturnsNoContent()
    {
        var userId = Ulid.NewUlid();
        SetupUserClaims(userId);

        _mockUserService
            .Setup(s => s.DeleteUserAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<global::BackEncordados.Common.Utils.Unit, AuthError>(global::BackEncordados.Common.Utils.Unit.Value));

        var result = await _controller.DeleteMe();

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task DeleteMe_NoUserIdClaim_ReturnsNotFound()
    {
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _controller.DeleteMe();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task DeleteMe_ExceptionThrown_ReturnsInternalServerError()
    {
        var userId = Ulid.NewUlid();
        SetupUserClaims(userId);

        _mockUserService
            .Setup(s => s.DeleteUserAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        var result = await _controller.DeleteMe();

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Patch Tests

    [Test]
    public async Task Patch_ValidRequest_ReturnsOkWithUser()
    {
        var userId = Ulid.NewUlid();
        var request = new UserRequestDto { Name = "Updated Name" };
        var user = CreateUserResponse();

        _mockUserService
            .Setup(s => s.PatchUserAsync(userId, It.IsAny<UserRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<UserResponseDto, AuthError>(user));

        var result = await _controller.Patch(userId, request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(user);
    }

    [Test]
    public async Task Patch_UserNotFound_ReturnsNotFound()
    {
        var userId = Ulid.NewUlid();
        var request = new UserRequestDto();
        var error = new UserNotFoundError("Usuario no encontrado");

        _mockUserService
            .Setup(s => s.PatchUserAsync(userId, It.IsAny<UserRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.Patch(userId, request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Patch_ConflictError_ReturnsConflict()
    {
        var userId = Ulid.NewUlid();
        var request = new UserRequestDto();
        var error = new ConflictError("Conflicto de datos");

        _mockUserService
            .Setup(s => s.PatchUserAsync(userId, It.IsAny<UserRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.Patch(userId, request);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task Patch_ValidationError_ReturnsBadRequest()
    {
        var userId = Ulid.NewUlid();
        var request = new UserRequestDto();
        var error = new ValidationError("Error de validación");

        _mockUserService
            .Setup(s => s.PatchUserAsync(userId, It.IsAny<UserRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.Patch(userId, request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task Patch_UnexpectedError_ReturnsInternalServerError()
    {
        var userId = Ulid.NewUlid();
        var request = new UserRequestDto();
        var error = new AuthError("Error inesperado");

        _mockUserService
            .Setup(s => s.PatchUserAsync(userId, It.IsAny<UserRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.Patch(userId, request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GiveRole Tests

    [Test]
    public async Task GiveRole_ValidRequest_ReturnsNoContent()
    {
        var userId = Ulid.NewUlid();
        var role = "ADMIN";

        _mockUserService
            .Setup(s => s.GiveRoleToUserAsync(userId, role))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<bool, AuthError>(true));

        var result = await _controller.GiveRole(userId, role);

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task GiveRole_ValidationError_ReturnsBadRequest()
    {
        var userId = Ulid.NewUlid();
        var role = "INVALID";
        var error = new ValidationError("Rol inválido");

        _mockUserService
            .Setup(s => s.GiveRoleToUserAsync(userId, role))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<bool, AuthError>(error));

        var result = await _controller.GiveRole(userId, role);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task GiveRole_UserNotFound_ReturnsNotFound()
    {
        var userId = Ulid.NewUlid();
        var role = "ADMIN";
        var error = new UserNotFoundError("Usuario no encontrado");

        _mockUserService
            .Setup(s => s.GiveRoleToUserAsync(userId, role))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<bool, AuthError>(error));

        var result = await _controller.GiveRole(userId, role);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GiveRole_ConflictError_ReturnsConflict()
    {
        var userId = Ulid.NewUlid();
        var role = "ADMIN";
        var error = new ConflictError("El usuario ya tiene ese rol");

        _mockUserService
            .Setup(s => s.GiveRoleToUserAsync(userId, role))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<bool, AuthError>(error));

        var result = await _controller.GiveRole(userId, role);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task GiveRole_UnexpectedError_ReturnsInternalServerError()
    {
        var userId = Ulid.NewUlid();
        var role = "ADMIN";
        var error = new AuthError("Error inesperado");

        _mockUserService
            .Setup(s => s.GiveRoleToUserAsync(userId, role))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<bool, AuthError>(error));

        var result = await _controller.GiveRole(userId, role);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region CreateContact Tests

    [Test]
    public async Task CreateContact_ValidRequest_ReturnsOk()
    {
        var request = new ContactoPostRequestDto { Email = "contact@example.com", Name = "Contact" };
        var user = CreateUserResponse();

        _mockUserService
            .Setup(s => s.CreateContacto(It.IsAny<ContactoPostRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<UserResponseDto, AuthError>(user));

        var result = await _controller.CreateContact(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(user);
    }

    [Test]
    public async Task CreateContact_ValidationError_ReturnsBadRequest()
    {
        var request = new ContactoPostRequestDto();
        var error = new ValidationError("Datos inválidos");

        _mockUserService
            .Setup(s => s.CreateContacto(It.IsAny<ContactoPostRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.CreateContact(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task CreateContact_UserNotFound_ReturnsNotFound()
    {
        var request = new ContactoPostRequestDto { Email = "contact@example.com" };
        var error = new UserNotFoundError("Usuario no encontrado");

        _mockUserService
            .Setup(s => s.CreateContacto(It.IsAny<ContactoPostRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.CreateContact(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task CreateContact_ConflictError_ReturnsConflict()
    {
        var request = new ContactoPostRequestDto { Email = "contact@example.com" };
        var error = new ConflictError("Conflicto de datos");

        _mockUserService
            .Setup(s => s.CreateContacto(It.IsAny<ContactoPostRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.CreateContact(request);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task CreateContact_UnexpectedError_ReturnsInternalServerError()
    {
        var request = new ContactoPostRequestDto { Email = "contact@example.com" };
        var error = new AuthError("Error inesperado");

        _mockUserService
            .Setup(s => s.CreateContacto(It.IsAny<ContactoPostRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.CreateContact(request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region CreateEncoder Tests

    [Test]
    public async Task CreateEncoder_ValidRequest_ReturnsNoContent()
    {
        var userId = Ulid.NewUlid();

        _mockUserService
            .Setup(s => s.CreateEncoderAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<global::BackEncordados.Common.Utils.Unit, AuthError>(global::BackEncordados.Common.Utils.Unit.Value));

        var result = await _controller.CreateEncoder(userId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task CreateEncoder_UserNotFound_ReturnsNotFound()
    {
        var userId = Ulid.NewUlid();
        var error = new UserNotFoundError("Usuario no encontrado");

        _mockUserService
            .Setup(s => s.CreateEncoderAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, AuthError>(error));

        var result = await _controller.CreateEncoder(userId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task CreateEncoder_ConflictError_ReturnsConflict()
    {
        var userId = Ulid.NewUlid();
        var error = new ConflictError("El usuario ya es encoder");

        _mockUserService
            .Setup(s => s.CreateEncoderAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, AuthError>(error));

        var result = await _controller.CreateEncoder(userId);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task CreateEncoder_ValidationError_ReturnsBadRequest()
    {
        var userId = Ulid.NewUlid();
        var error = new ValidationError("Datos inválidos");

        _mockUserService
            .Setup(s => s.CreateEncoderAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, AuthError>(error));

        var result = await _controller.CreateEncoder(userId);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task CreateEncoder_UnexpectedError_ReturnsInternalServerError()
    {
        var userId = Ulid.NewUlid();
        var error = new AuthError("Error inesperado");

        _mockUserService
            .Setup(s => s.CreateEncoderAsync(userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, AuthError>(error));

        var result = await _controller.CreateEncoder(userId);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region AddBonos Tests

    [Test]
    public async Task AddBonos_ValidAmount_ReturnsOk()
    {
        var userId = Ulid.NewUlid();
        var cantidad = 50.0;
        var user = CreateUserResponse();

        _mockUserService
            .Setup(s => s.AddBonosAsync(userId, cantidad))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<UserResponseDto, AuthError>(user));

        var result = await _controller.AddBonos(userId, cantidad);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(user);
    }

    [Test]
    public async Task AddBonos_InvalidAmount_ReturnsBadRequest()
    {
        var userId = Ulid.NewUlid();
        var cantidad = -10.0;
        var error = new ValidationError("Cantidad inválida");

        _mockUserService
            .Setup(s => s.AddBonosAsync(userId, cantidad))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.AddBonos(userId, cantidad);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task AddBonos_UserNotFound_ReturnsNotFound()
    {
        var userId = Ulid.NewUlid();
        var cantidad = 50.0;
        var error = new UserNotFoundError("Usuario no encontrado");

        _mockUserService
            .Setup(s => s.AddBonosAsync(userId, cantidad))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.AddBonos(userId, cantidad);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task AddBonos_UnexpectedError_ReturnsInternalServerError()
    {
        var userId = Ulid.NewUlid();
        var cantidad = 50.0;
        var error = new AuthError("Error inesperado");

        _mockUserService
            .Setup(s => s.AddBonosAsync(userId, cantidad))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<UserResponseDto, AuthError>(error));

        var result = await _controller.AddBonos(userId, cantidad);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion
}