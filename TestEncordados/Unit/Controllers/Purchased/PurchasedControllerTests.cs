using System.Security.Claims;
using BackEncordados.Common.Dto;
using BackEncordados.Common.Errors;
using BackEncordados.Purchased.Controller;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Errors;
using BackEncordados.Purchased.Service;
using BackEncordados.Purchased.Validator;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Model;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Role = BackEncordados.Usuarios.Model.User.UserRoles;

namespace TestEncordados.Unit.Controllers.Purchased;

public class PurchasedControllerTests
{
    private readonly Mock<IPurchasedService> _mockService;
    private readonly Mock<IValidator<PurchasedRequestDto>> _mockValidator;
    private readonly PurchasedController _controller;

    public PurchasedControllerTests()
    {
        _mockService = new Mock<IPurchasedService>();
        _mockValidator = new Mock<IValidator<PurchasedRequestDto>>();
        _controller = new PurchasedController(
            NullLogger<PurchasedController>.Instance,
            _mockService.Object,
            _mockValidator.Object
        );
    }

    #region Factory Methods

    private static UserResponseDto CreateUserResponse() => new(
        Username: "testuser",
        ImageUrl: "https://example.com/image.png",
        Name: "Test User",
        Bonos: 100.0
    );

    private static PurchasedResponseDto CreatePurchasedResponse() => new(
        Id: Ulid.NewUlid(),
        TournamentId: Ulid.NewUlid(),
        Player: CreateUserResponse(),
        Encorder: CreateUserResponse(),
        Machine: "Machine-001",
        Comments: "Test comments",
        PayStatus: "PENDING",
        CreatedAt: DateTime.Now,
        UpdatedAt: DateTime.Now,
        Price: 50.0,
        Lineas: [CreatePedidoLineaResponse()]
    );

    private static PedidoLineaResponseDto CreatePedidoLineaResponse() => new(
        Id: Ulid.NewUlid(),
        RaquetModel: "Wilson Pro Staff",
        Nudos: 2,
        DateString: DateTime.Now,
        Logotype: true,
        Color: "Red",
        Status: BackEncordados.Purchased.Model.Status.PENDING,
        StringSetup: new BackEncordados.Purchased.Model.StringSetup { StringV = "Polyester", StringH = "Multifilament", TensionV = 55, TensionH = 52, PreStetchV = 0, PreStetchH = 0 }
    );

    private static PurchasedRequestDto CreatePurchasedRequest() => new()
    {
        TournamentId = Ulid.NewUlid(),
        PlayerName = "Player 1",
        AssignedToName = "Encorder 1",
        Machine = "Machine-001",
        Comments = "Test comments",
        PayStatus = "PENDING",
        Price = 50.0,
        Lineas = [new PedidoLineaRequestDto { RaquetModel = "Wilson", Nudos = 2, DateString = DateTime.Now, Logotype = true, Color = "Red", StringSetup = new StringSetupDto { StringV = "Polyester", StringH = "Multifilament", TensionV = 55, TensionH = 52, PreStetchV = 0, PreStetchH = 0 } }]
    };

    private static PurchasedPatchDto CreatePurchasedPatch() => new()
    {
        Machine = "Machine-002",
        Comments = "Updated comments",
        PayStatus = "PAID"
    };

    private static PedidoLineaPatchDto CreatePedidoLineaPatch() => new()
    {
        RaquetModel = "Updated Model",
        Nudos = 3,
        Color = "Blue"
    };

    private static PageResponseDto<PurchasedResponseDto> CreateEmptyPageResponse() => new(
        Content: [],
        TotalPages: 0,
        TotalElements: 0,
        PageSize: 10,
        PageNumber: 0,
        TotalPageElements: 0,
        SortBy: "createdAt",
        Direction: "desc"
    );

    private static PageResponseDto<PurchasedResponseDto> CreatePageResponseWithItems() => new(
        Content: [CreatePurchasedResponse()],
        TotalPages: 1,
        TotalElements: 1,
        PageSize: 10,
        PageNumber: 0,
        TotalPageElements: 1,
        SortBy: "createdAt",
        Direction: "desc"
    );

    private PurchasedController CreateControllerWithRole(string role, Ulid? tournamentId = null, string? userId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role)
        };
        if (!string.IsNullOrEmpty(userId))
        {
            claims.Add(new(ClaimTypes.NameIdentifier, userId));
        }
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal,
            Request = { Scheme = "https" }
        };
        
        var controller = new PurchasedController(
            NullLogger<PurchasedController>.Instance,
            _mockService.Object,
            _mockValidator.Object
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        return controller;
    }

    #endregion

    #region GetAll Tests

    [Test]
    public async Task GetAll_AdminRole_ReturnsOkWithAllResults()
    {
        var controller = CreateControllerWithRole(Role.ADMIN.ToString());
        _mockService.Setup(s => s.FindAllAsync(It.IsAny<FilterPurchasedDto>()))
            .ReturnsAsync(CreatePageResponseWithItems());

        var result = await controller.GetAll(null, "createdAt", 0, 10, "desc", "");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PageResponseDto<PurchasedResponseDto>>().Subject;
        response.Content.Should().HaveCount(1);
    }

    [Test]
    public async Task GetAll_OwnerRoleWithoutTournamentId_ReturnsForbid()
    {
        var controller = CreateControllerWithRole(Role.OWNER.ToString());

        var result = await controller.GetAll(null, "createdAt", 0, 10, "desc", "");

        result.Should().BeOfType<ForbidResult>();
    }

    [Test]
    public async Task GetAll_OwnerRoleWithTournamentId_ReturnsOkWithFilteredResults()
    {
        var tournamentId = Ulid.NewUlid();
        var userId = Ulid.NewUlid().ToString();
        var controller = CreateControllerWithRole(Role.OWNER.ToString(), tournamentId, userId);
        
        _mockService.Setup(s => s.FindAllAsync(It.Is<FilterPurchasedDto>(f => 
            f.UserId == userId && f.TournamentId == tournamentId)))
            .ReturnsAsync(CreatePageResponseWithItems());

        var result = await controller.GetAll(tournamentId, "createdAt", 0, 10, "desc", "");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _mockService.Verify(s => s.FindAllAsync(It.Is<FilterPurchasedDto>(f => 
            f.UserId == userId && f.TournamentId == tournamentId)), Times.Once);
    }

    [Test]
    public async Task GetAll_SupervisorRoleWithoutTournamentId_ReturnsForbid()
    {
        var controller = CreateControllerWithRole(Role.SUPERVISOR.ToString());

        var result = await controller.GetAll(null, "createdAt", 0, 10, "desc", "");

        result.Should().BeOfType<ForbidResult>();
    }

    [Test]
    public async Task GetAll_SupervisorRoleWithTournamentId_ReturnsOkWithFilteredResults()
    {
        var tournamentId = Ulid.NewUlid();
        var userId = Ulid.NewUlid().ToString();
        var controller = CreateControllerWithRole(Role.SUPERVISOR.ToString(), tournamentId, userId);
        
        _mockService.Setup(s => s.FindAllAsync(It.Is<FilterPurchasedDto>(f => 
            f.UserId == userId && f.TournamentId == tournamentId)))
            .ReturnsAsync(CreatePageResponseWithItems());

        var result = await controller.GetAll(tournamentId, "createdAt", 0, 10, "desc", "");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _mockService.Verify(s => s.FindAllAsync(It.Is<FilterPurchasedDto>(f => 
            f.UserId == userId && f.TournamentId == tournamentId)), Times.Once);
    }

    [Test]
    public async Task GetAll_EncorderRole_ReturnsOkWithFilteredResults()
    {
        var userId = Ulid.NewUlid().ToString();
        var controller = CreateControllerWithRole(Role.ENCORDER.ToString(), userId: userId);
        
        _mockService.Setup(s => s.FindAllAsync(It.Is<FilterPurchasedDto>(f => 
            f.UserId == userId && f.IsEncorder == true)))
            .ReturnsAsync(CreatePageResponseWithItems());

        var result = await controller.GetAll(null, "createdAt", 0, 10, "desc", "");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _mockService.Verify(s => s.FindAllAsync(It.Is<FilterPurchasedDto>(f => 
            f.UserId == userId && f.IsEncorder == true)), Times.Once);
    }

    [Test]
    public async Task GetAll_UserRole_ReturnsOkWithFilteredResults()
    {
        var userId = Ulid.NewUlid().ToString();
        var controller = CreateControllerWithRole(Role.USER.ToString(), userId: userId);
        
        _mockService.Setup(s => s.FindAllAsync(It.Is<FilterPurchasedDto>(f => 
            f.UserId == userId && f.IsUser == true)))
            .ReturnsAsync(CreatePageResponseWithItems());

        var result = await controller.GetAll(null, "createdAt", 0, 10, "desc", "");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _mockService.Verify(s => s.FindAllAsync(It.Is<FilterPurchasedDto>(f => 
            f.UserId == userId && f.IsUser == true)), Times.Once);
    }

    private PurchasedController CreateControllerWithRoleButNoUserId(string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal,
            Request = { Scheme = "https" }
        };
        
        var controller = new PurchasedController(
            NullLogger<PurchasedController>.Instance,
            _mockService.Object,
            _mockValidator.Object
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        return controller;
    }

    [Test]
    public async Task GetAll_EncorderRoleWithoutUserId_ReturnsForbid()
    {
        var controller = CreateControllerWithRoleButNoUserId(Role.ENCORDER.ToString());

        var result = await controller.GetAll(null, "createdAt", 0, 10, "desc", "");

        result.Should().BeOfType<ForbidResult>();
    }

    [Test]
    public async Task GetAll_UserRoleWithoutUserId_ReturnsForbid()
    {
        var controller = CreateControllerWithRoleButNoUserId(Role.USER.ToString());

        var result = await controller.GetAll(null, "createdAt", 0, 10, "desc", "");

        result.Should().BeOfType<ForbidResult>();
    }

    [Test]
    public async Task GetAll_NoUserClaims_ReturnsForbid()
    {
        var controller = new PurchasedController(
            NullLogger<PurchasedController>.Instance,
            _mockService.Object,
            _mockValidator.Object
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await controller.GetAll(null, "createdAt", 0, 10, "desc", "");

        result.Should().BeOfType<ForbidResult>();
    }

    [Test]
    public async Task GetAll_WithSearchFilter_PassesSearchToService()
    {
        var controller = CreateControllerWithRole(Role.ADMIN.ToString());
        _mockService.Setup(s => s.FindAllAsync(It.Is<FilterPurchasedDto>(f => f.Search == "test")))
            .ReturnsAsync(CreateEmptyPageResponse());

        await controller.GetAll(null, "createdAt", 0, 10, "desc", "test");

        _mockService.Verify(s => s.FindAllAsync(It.Is<FilterPurchasedDto>(f => f.Search == "test")), Times.Once);
    }

    #endregion

    #region GetById Tests

    [Test]
    public async Task GetById_ValidId_ReturnsOkWithPurchased()
    {
        var id = Ulid.NewUlid();
        var response = CreatePurchasedResponse();
        _mockService.Setup(s => s.FindByIdAsync(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<PurchasedResponseDto, DomainErrors>(response));

        var result = await _controller.GetById(id.ToString());

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        var result = await _controller.GetById("invalid-id");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetById_PurchasedNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var error = new PurchasedNotFoundError();
        _mockService.Setup(s => s.FindByIdAsync(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.GetById(id.ToString());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetById_UserNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var error = new UserNotFoundError("User not found");
        _mockService.Setup(s => s.FindByIdAsync(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.GetById(id.ToString());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetById_ServerError_Returns500()
    {
        var id = Ulid.NewUlid();
        var error = new PurchasedErrors("Server error");
        _mockService.Setup(s => s.FindByIdAsync(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.GetById(id.ToString());

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Create Tests

    [Test]
    public async Task Create_ValidRequest_ReturnsCreatedResult()
    {
        var request = CreatePurchasedRequest();
        var response = CreatePurchasedResponse();
        
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<PurchasedRequestDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreatePurchasedAsync(It.IsAny<PurchasedRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<PurchasedResponseDto, DomainErrors>(response));

        var result = await _controller.Create(request);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().Be(response);
    }

    [Test]
    public async Task Create_InvalidRequest_ReturnsBadRequest()
    {
        var request = CreatePurchasedRequest();
        var validationErrors = new List<ValidationFailure>
        {
            new("TournamentId", "El ID del torneo es obligatorio")
        };
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<PurchasedRequestDto>(), default))
            .ReturnsAsync(new ValidationResult(validationErrors));

        var result = await _controller.Create(request);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errors = ((System.Collections.IEnumerable)badRequestResult.Value).Cast<object>().ToList();
        errors.Should().NotBeEmpty();

        var firstError = errors[0];
        var errorType = firstError.GetType();
        errorType.GetProperty("PropertyName").Should().NotBeNull();
        errorType.GetProperty("ErrorMessage").Should().NotBeNull();
    }

    [Test]
    public async Task Create_UserNotFoundError_ReturnsBadRequest()
    {
        var request = CreatePurchasedRequest();
        var error = new UserNotFoundError("User not found");
        
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<PurchasedRequestDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreatePurchasedAsync(It.IsAny<PurchasedRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.Create(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task Create_ServerError_Returns500()
    {
        var request = CreatePurchasedRequest();
        var error = new PurchasedErrors("Server error");
        
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<PurchasedRequestDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreatePurchasedAsync(It.IsAny<PurchasedRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.Create(request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task Create_CallsValidator_WithCorrectRequest()
    {
        var request = CreatePurchasedRequest();
        _mockValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreatePurchasedAsync(It.IsAny<PurchasedRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<PurchasedResponseDto, DomainErrors>(CreatePurchasedResponse()));

        await _controller.Create(request);

        _mockValidator.Verify(v => v.ValidateAsync(request, default), Times.Once);
    }

    #endregion

    #region Update Tests

    [Test]
    public async Task Update_ValidId_ReturnsOkWithPurchased()
    {
        var id = Ulid.NewUlid();
        var request = CreatePurchasedPatch();
        var response = CreatePurchasedResponse();
        
        _mockService.Setup(s => s.UpdatePurchasedAsync(id, request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<PurchasedResponseDto, DomainErrors>(response));

        var result = await _controller.Update(id.ToString(), request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        var result = await _controller.Update("invalid-id", CreatePurchasedPatch());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Update_PurchasedNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var request = CreatePurchasedPatch();
        var error = new PurchasedNotFoundError();
        
        _mockService.Setup(s => s.UpdatePurchasedAsync(id, request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.Update(id.ToString(), request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Update_ServerError_Returns500()
    {
        var id = Ulid.NewUlid();
        var request = CreatePurchasedPatch();
        var error = new PurchasedErrors("Server error");
        
        _mockService.Setup(s => s.UpdatePurchasedAsync(id, request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.Update(id.ToString(), request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region CancelPurchased Tests

    [Test]
    public async Task CancelPurchased_ValidIdAsEncorder_ReturnsOk()
    {
        var id = Ulid.NewUlid();
        var response = CreatePurchasedResponse();
        
        var controller = CreateControllerWithRole(Role.ENCORDER.ToString());
        _mockService.Setup(s => s.CancelPurchasedAsync(id, false, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<PurchasedResponseDto, DomainErrors>(response));

        var result = await controller.CancelPurchased(id.ToString());

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task CancelPurchased_ValidIdAsUser_ReturnsOk()
    {
        var id = Ulid.NewUlid();
        var userId = Ulid.NewUlid().ToString();
        var response = CreatePurchasedResponse();
        
        var controller = CreateControllerWithRole(Role.USER.ToString(), userId: userId);
        _mockService.Setup(s => s.CancelPurchasedAsync(id, true, userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<PurchasedResponseDto, DomainErrors>(response));

        var result = await controller.CancelPurchased(id.ToString());

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task CancelPurchased_InvalidId_ReturnsNotFound()
    {
        var controller = CreateControllerWithRole(Role.ENCORDER.ToString());
        var result = await controller.CancelPurchased("invalid-id");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task CancelPurchased_PurchasedNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var error = new PurchasedNotFoundError();
        
        var controller = CreateControllerWithRole(Role.ENCORDER.ToString());
        _mockService.Setup(s => s.CancelPurchasedAsync(id, false, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await controller.CancelPurchased(id.ToString());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task CancelPurchased_UnauthorizedError_ReturnsForbidden()
    {
        var id = Ulid.NewUlid();
        var error = new UnauthorizedError("Unauthorized");
        
        var controller = CreateControllerWithRole(Role.USER.ToString());
        _mockService.Setup(s => s.CancelPurchasedAsync(id, true, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await controller.CancelPurchased(id.ToString());

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(403);
    }

    [Test]
    public async Task CancelPurchased_ServerError_Returns500()
    {
        var id = Ulid.NewUlid();
        var error = new PurchasedErrors("Server error");
        
        var controller = CreateControllerWithRole(Role.ENCORDER.ToString());
        _mockService.Setup(s => s.CancelPurchasedAsync(id, false, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await controller.CancelPurchased(id.ToString());

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region ChangePaymentStatusPurchased Tests

    [Test]
    public async Task ChangePaymentStatusPurchased_ValidRequest_ReturnsOk()
    {
        var id = Ulid.NewUlid();
        var response = CreatePurchasedResponse();
        
        _mockService.Setup(s => s.ChangePaymentStatusPurchasedAsync(id, "PAID"))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<PurchasedResponseDto, DomainErrors>(response));

        var result = await _controller.ChangePaymentStatusPurchased(id.ToString(), "PAID");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task ChangePaymentStatusPurchased_InvalidId_ReturnsNotFound()
    {
        var result = await _controller.ChangePaymentStatusPurchased("invalid-id", "PAID");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task ChangePaymentStatusPurchased_PurchasedNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var error = new PurchasedNotFoundError();
        
        _mockService.Setup(s => s.ChangePaymentStatusPurchasedAsync(id, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.ChangePaymentStatusPurchased(id.ToString(), "PAID");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task ChangePaymentStatusPurchased_InvalidStatusError_ReturnsBadRequest()
    {
        var id = Ulid.NewUlid();
        var error = new InvalidStatusError("Invalid status");
        
        _mockService.Setup(s => s.ChangePaymentStatusPurchasedAsync(id, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.ChangePaymentStatusPurchased(id.ToString(), "INVALID");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task ChangePaymentStatusPurchased_ServerError_Returns500()
    {
        var id = Ulid.NewUlid();
        var error = new PurchasedErrors("Server error");
        
        _mockService.Setup(s => s.ChangePaymentStatusPurchasedAsync(id, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.ChangePaymentStatusPurchased(id.ToString(), "PAID");

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region ChangeAllLineasStatus Tests

    [Test]
    public async Task ChangeAllLineasStatus_ValidRequest_ReturnsOk()
    {
        var id = Ulid.NewUlid();
        var response = CreatePurchasedResponse();
        
        _mockService.Setup(s => s.ChangeAllLineasStatusAsync(id, "COMPLETED"))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<PurchasedResponseDto, DomainErrors>(response));

        var result = await _controller.ChangeAllLineasStatus(id.ToString(), "COMPLETED");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task ChangeAllLineasStatus_InvalidId_ReturnsNotFound()
    {
        var result = await _controller.ChangeAllLineasStatus("invalid-id", "COMPLETED");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task ChangeAllLineasStatus_PurchasedNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var error = new PurchasedNotFoundError();
        
        _mockService.Setup(s => s.ChangeAllLineasStatusAsync(id, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.ChangeAllLineasStatus(id.ToString(), "COMPLETED");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task ChangeAllLineasStatus_InvalidStatusError_ReturnsBadRequest()
    {
        var id = Ulid.NewUlid();
        var error = new InvalidStatusError("Invalid status");
        
        _mockService.Setup(s => s.ChangeAllLineasStatusAsync(id, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.ChangeAllLineasStatus(id.ToString(), "INVALID");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task ChangeAllLineasStatus_ServerError_Returns500()
    {
        var id = Ulid.NewUlid();
        var error = new PurchasedErrors("Server error");
        
        _mockService.Setup(s => s.ChangeAllLineasStatusAsync(id, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PurchasedResponseDto, DomainErrors>(error));

        var result = await _controller.ChangeAllLineasStatus(id.ToString(), "COMPLETED");

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region UpdateLinea Tests

    [Test]
    public async Task UpdateLinea_ValidId_ReturnsOk()
    {
        var lineaId = Ulid.NewUlid();
        var request = CreatePedidoLineaPatch();
        var response = CreatePedidoLineaResponse();
        
        _mockService.Setup(s => s.UpdateLineaAsync(lineaId, request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<PedidoLineaResponseDto, DomainErrors>(response));

        var result = await _controller.UpdateLinea(lineaId.ToString(), request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task UpdateLinea_InvalidId_ReturnsNotFound()
    {
        var result = await _controller.UpdateLinea("invalid-id", CreatePedidoLineaPatch());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UpdateLinea_PurchasedNotFound_ReturnsNotFound()
    {
        var lineaId = Ulid.NewUlid();
        var request = CreatePedidoLineaPatch();
        var error = new PurchasedNotFoundError();
        
        _mockService.Setup(s => s.UpdateLineaAsync(lineaId, request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PedidoLineaResponseDto, DomainErrors>(error));

        var result = await _controller.UpdateLinea(lineaId.ToString(), request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UpdateLinea_ServerError_Returns500()
    {
        var lineaId = Ulid.NewUlid();
        var request = CreatePedidoLineaPatch();
        var error = new PurchasedErrors("Server error");
        
        _mockService.Setup(s => s.UpdateLineaAsync(lineaId, request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PedidoLineaResponseDto, DomainErrors>(error));

        var result = await _controller.UpdateLinea(lineaId.ToString(), request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region CancelLinea Tests

    [Test]
    public async Task CancelLinea_ValidId_ReturnsOk()
    {
        var lineaId = Ulid.NewUlid();
        var response = CreatePedidoLineaResponse();
        var userId = Ulid.NewUlid().ToString();
        
        var controller = CreateControllerWithRole(Role.ENCORDER.ToString(), userId: userId);
        _mockService.Setup(s => s.CancelLineaAsync(lineaId, userId, Role.ENCORDER.ToString()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<PedidoLineaResponseDto, DomainErrors>(response));

        var result = await controller.CancelLinea(lineaId.ToString());

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task CancelLinea_InvalidId_ReturnsNotFound()
    {
        var controller = CreateControllerWithRole(Role.ENCORDER.ToString());
        var result = await controller.CancelLinea("invalid-id");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task CancelLinea_PurchasedNotFound_ReturnsNotFound()
    {
        var lineaId = Ulid.NewUlid();
        var error = new PurchasedNotFoundError();
        var userId = Ulid.NewUlid().ToString();
        
        var controller = CreateControllerWithRole(Role.ENCORDER.ToString(), userId: userId);
        _mockService.Setup(s => s.CancelLineaAsync(lineaId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PedidoLineaResponseDto, DomainErrors>(error));

        var result = await controller.CancelLinea(lineaId.ToString());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task CancelLinea_UnauthorizedError_ReturnsForbidden()
    {
        var lineaId = Ulid.NewUlid();
        var error = new UnauthorizedError("Unauthorized");
        var userId = Ulid.NewUlid().ToString();
        
        var controller = CreateControllerWithRole(Role.ENCORDER.ToString(), userId: userId);
        _mockService.Setup(s => s.CancelLineaAsync(lineaId, userId, Role.ENCORDER.ToString()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PedidoLineaResponseDto, DomainErrors>(error));

        var result = await controller.CancelLinea(lineaId.ToString());

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(403);
    }

    [Test]
    public async Task CancelLinea_InvalidStatusError_ReturnsBadRequest()
    {
        var lineaId = Ulid.NewUlid();
        var error = new InvalidStatusError("Invalid status");
        var userId = Ulid.NewUlid().ToString();
        
        var controller = CreateControllerWithRole(Role.ENCORDER.ToString(), userId: userId);
        _mockService.Setup(s => s.CancelLineaAsync(lineaId, userId, Role.ENCORDER.ToString()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PedidoLineaResponseDto, DomainErrors>(error));

        var result = await controller.CancelLinea(lineaId.ToString());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task CancelLinea_ServerError_Returns500()
    {
        var lineaId = Ulid.NewUlid();
        var error = new PurchasedErrors("Server error");
        var userId = Ulid.NewUlid().ToString();
        
        var controller = CreateControllerWithRole(Role.ENCORDER.ToString(), userId: userId);
        _mockService.Setup(s => s.CancelLineaAsync(lineaId, userId, Role.ENCORDER.ToString()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PedidoLineaResponseDto, DomainErrors>(error));

        var result = await controller.CancelLinea(lineaId.ToString());

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region ChangeLineaStatus Tests

    [Test]
    public async Task ChangeLineaStatus_ValidRequest_ReturnsOk()
    {
        var lineaId = Ulid.NewUlid();
        var response = CreatePedidoLineaResponse();
        
        _mockService.Setup(s => s.ChangeLineaStatusAsync(lineaId, "COMPLETED"))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<PedidoLineaResponseDto, DomainErrors>(response));

        var result = await _controller.ChangeLineaStatus(lineaId.ToString(), "COMPLETED");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task ChangeLineaStatus_InvalidId_ReturnsNotFound()
    {
        var result = await _controller.ChangeLineaStatus("invalid-id", "COMPLETED");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task ChangeLineaStatus_PurchasedNotFound_ReturnsNotFound()
    {
        var lineaId = Ulid.NewUlid();
        var error = new PurchasedNotFoundError();
        
        _mockService.Setup(s => s.ChangeLineaStatusAsync(lineaId, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PedidoLineaResponseDto, DomainErrors>(error));

        var result = await _controller.ChangeLineaStatus(lineaId.ToString(), "COMPLETED");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task ChangeLineaStatus_InvalidStatusError_ReturnsBadRequest()
    {
        var lineaId = Ulid.NewUlid();
        var error = new InvalidStatusError("Invalid status");
        
        _mockService.Setup(s => s.ChangeLineaStatusAsync(lineaId, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PedidoLineaResponseDto, DomainErrors>(error));

        var result = await _controller.ChangeLineaStatus(lineaId.ToString(), "INVALID");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task ChangeLineaStatus_ServerError_Returns500()
    {
        var lineaId = Ulid.NewUlid();
        var error = new PurchasedErrors("Server error");
        
        _mockService.Setup(s => s.ChangeLineaStatusAsync(lineaId, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<PedidoLineaResponseDto, DomainErrors>(error));

        var result = await _controller.ChangeLineaStatus(lineaId.ToString(), "COMPLETED");

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion
}