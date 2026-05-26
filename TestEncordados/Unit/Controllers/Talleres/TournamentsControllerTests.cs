using System.Security.Claims;
using BackEncordados.Common.Dto;
using BackEncordados.Common.Errors;
using BackEncordados.Talleres.Controller;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Error;
using BackEncordados.Talleres.Service;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ConflictError = BackEncordados.Talleres.Error.ConflictError;
using TalleresValidationError = BackEncordados.Talleres.Error.ValidationError;

namespace TestEncordados.Unit.Controllers.Talleres;

public class TournamentsControllerTests
{
    private readonly Mock<ITournamentService> _mockService;
    private readonly TournamentsController _controller;

    public TournamentsControllerTests()
    {
        _mockService = new Mock<ITournamentService>();
        _controller = new TournamentsController(
            NullLogger<TournamentsController>.Instance,
            _mockService.Object
        );
        ConfigureControllerUrl();
        ConfigureControllerWithAdminUser();
    }

    private void ConfigureControllerUrl()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        _controller.Url = new Mock<IUrlHelper>().Object;
    }

    private void ConfigureControllerWithAdminUser()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "ADMIN"),
            new(ClaimTypes.NameIdentifier, Ulid.NewUlid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        _controller.Url = new Mock<IUrlHelper>().Object;
    }

    private TournamentsController CreateControllerWithUserRole(string role, Ulid? userId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role)
        };
        if (userId.HasValue)
        {
            claims.Add(new(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var controller = new TournamentsController(
            NullLogger<TournamentsController>.Instance,
            _mockService.Object
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        controller.Url = new Mock<IUrlHelper>().Object;
        return controller;
    }

    #region Factory Methods

    private static TournamentResponseDto CreateTournamentResponse() => new(
        Id: Ulid.NewUlid(),
        Name: "Torneo de Prueba",
        EndTournament: DateTime.Now.AddDays(30),
        StartTournament: DateTime.Now,
        Logotype: "https://example.com/logo.png"
    );

    private static TournamentResponseDetailsDto CreateTournamentDetailsResponse() => new(
        Id: Ulid.NewUlid(),
        Name: "Torneo de Prueba",
        StartDate: DateTime.Now,
        EndDate: DateTime.Now.AddDays(30),
        Logotype: "https://example.com/logo.png",
        User: [CreateUserResponse()],
        Owner: CreateUserResponse(),
        Supevisors: []
    );

    private static UserResponseDto CreateUserResponse() => new(
        Username: "testuser",
        ImageUrl: "https://example.com/image.png",
        Name: "Test User",
        Bonos: 100.0
    );

    private static TournamentAdminRequestDto CreateAdminRequest() => new()
    {
        Name = "Torneo de Prueba",
        OwnerId = Ulid.NewUlid(),
        EndTournament = DateTime.Now.AddDays(30),
        StartTournament = DateTime.Now,
        Logotype = null
    };

    private static TournamentRequestDto CreateOwnerRequest() => new()
    {
        Name = "Torneo de Prueba",
        EndTournament = DateTime.Now.AddDays(30),
        StartTournament = DateTime.Now,
        Logotype = null
    };

    private static TournamentPatchDto CreatePatchRequest() => new()
    {
        Name = "Torneo Actualizado",
        StartTournament = DateTime.Now,
        EndTournament = DateTime.Now.AddDays(30),
        Logotype = null
    };

    private static PageResponseDto<TournamentResponseDto> CreateEmptyPageResponse() => new(
        Content: [],
        TotalPages: 0,
        TotalElements: 0,
        PageSize: 10,
        PageNumber: 0,
        TotalPageElements: 0,
        SortBy: "name",
        Direction: "asc"
    );

    private static PageResponseDto<TournamentResponseDto> CreatePageResponseWithItems() => new(
        Content: [CreateTournamentResponse()],
        TotalPages: 1,
        TotalElements: 1,
        PageSize: 10,
        PageNumber: 0,
        TotalPageElements: 1,
        SortBy: "name",
        Direction: "asc"
    );

    private static WorkerMachineAssignmentRequestDto CreateWorkerAssignmentRequest() => new()
    {
        UserId = Ulid.NewUlid().ToString(),
        MachineName = "Machine-001"
    };

    private static SupervisorAsignmentRequestDto CreateSupervisorAssignmentRequest() => new()
    {
        TournamentId = Ulid.NewUlid(),
        SupervisorId = Ulid.NewUlid().ToString()
    };

    private static List<WorkerMachineAssignmentResponseDto> CreateWorkerMachineList() =>
    [
        new WorkerMachineAssignmentResponseDto(
            MachineName: "Machine-001",
            User: CreateUserResponse()
        )
    ];

    #endregion

    #region CreateTournament (Admin) Tests

    [Test]
    public async Task CreateTournament_ValidRequest_ReturnsCreatedResult()
    {
        var request = CreateAdminRequest();
        var response = CreateTournamentDetailsResponse();
        _mockService.Setup(s => s.CreateTournament(It.IsAny<TournamentAdminRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<TournamentResponseDetailsDto, DomainErrors>(response));

        var result = await _controller.CreateTournament(request);

        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().Be(response);
    }

    [Test]
    public async Task CreateTournament_UserNotFound_ReturnsNotFound()
    {
        var request = CreateAdminRequest();
        var error = new UserNotFoundError("Owner not found");
        _mockService.Setup(s => s.CreateTournament(It.IsAny<TournamentAdminRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.CreateTournament(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task CreateTournament_ConflictError_ReturnsConflict()
    {
        var request = CreateAdminRequest();
        var error = new ConflictError("Tournament already exists");
        _mockService.Setup(s => s.CreateTournament(It.IsAny<TournamentAdminRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.CreateTournament(request);

        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task CreateTournament_ValidationError_ReturnsBadRequest()
    {
        var request = CreateAdminRequest();
        var error = new TalleresValidationError("Invalid data");
        _mockService.Setup(s => s.CreateTournament(It.IsAny<TournamentAdminRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.CreateTournament(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task CreateTournament_ServerError_Returns500()
    {
        var request = CreateAdminRequest();
        var error = new TournamentsErrors("Server error");
        _mockService.Setup(s => s.CreateTournament(It.IsAny<TournamentAdminRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.CreateTournament(request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetTournament Tests

    [Test]
    public async Task GetTournament_ExistingId_ReturnsOkWithTournament()
    {
        var id = Ulid.NewUlid();
        var response = CreateTournamentDetailsResponse();
        _mockService.Setup(s => s.GetTournament(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<TournamentResponseDetailsDto, DomainErrors>(response));

        var result = await _controller.GetTournament(id);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task GetTournament_TournamentNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var error = new TournamentNotFoundError("Tournament not found");
        _mockService.Setup(s => s.GetTournament(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.GetTournament(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetTournament_UserNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var error = new UserNotFoundError("User not found");
        _mockService.Setup(s => s.GetTournament(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.GetTournament(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetTournament_ServerError_Returns500()
    {
        var id = Ulid.NewUlid();
        var error = new TournamentsErrors("Server error");
        _mockService.Setup(s => s.GetTournament(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.GetTournament(id);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetTournamentByName Tests

    [Test]
    public async Task GetTournamentByName_ExistingName_ReturnsOkWithTournament()
    {
        var name = "Torneo de Prueba";
        var response = CreateTournamentDetailsResponse();
        _mockService.Setup(s => s.GetTournamentByName(name))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<TournamentResponseDetailsDto, DomainErrors>(response));

        var result = await _controller.GetTournamentByName(name);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task GetTournamentByName_TournamentNotFound_ReturnsNotFound()
    {
        var name = "NonExistent";
        var error = new TournamentNotFoundError("Tournament not found");
        _mockService.Setup(s => s.GetTournamentByName(name))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.GetTournamentByName(name);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetTournamentByName_UserNotFound_ReturnsNotFound()
    {
        var name = "Torneo de Prueba";
        var error = new UserNotFoundError("User not found");
        _mockService.Setup(s => s.GetTournamentByName(name))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.GetTournamentByName(name);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetTournamentByName_ServerError_Returns500()
    {
        var name = "Torneo de Prueba";
        var error = new TournamentsErrors("Server error");
        _mockService.Setup(s => s.GetTournamentByName(name))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.GetTournamentByName(name);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetTournaments Tests

    private TournamentsController CreateControllerForGetTournaments(string role, Ulid? userId = null)
    {
        var roleValue = role == "ADMIN" ? "ADMIN" : "SUPERVISOR";
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, roleValue)
        };
        if (userId.HasValue && role != "ADMIN")
        {
            claims.Add(new(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        
        var controller = new TournamentsController(
            NullLogger<TournamentsController>.Instance,
            _mockService.Object
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        return controller;
    }

    private TournamentsController CreateControllerForGetTournamentsWithInvalidUserId(string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "SUPERVISOR"),
            new(ClaimTypes.NameIdentifier, "invalid-ulid-value")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        
        var controller = new TournamentsController(
            NullLogger<TournamentsController>.Instance,
            _mockService.Object
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        return controller;
    }

    [Test]
    public async Task GetTournaments_EmptyResults_ReturnsOkWithEmptyList()
    {
        var controller = CreateControllerForGetTournaments("ADMIN");
        _mockService.Setup(s => s.GetAllTournamentsAsync(It.IsAny<FilterTournamentDto>()))
            .ReturnsAsync(CreateEmptyPageResponse());

        var result = await controller.GetTournaments("", 0, 10, "name", "asc");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PageResponseDto<TournamentResponseDto>>().Subject;
        response.Content.Should().BeEmpty();
        response.TotalElements.Should().Be(0);
    }

    [Test]
    public async Task GetTournaments_WithResults_ReturnsOkWithItems()
    {
        var controller = CreateControllerForGetTournaments("ADMIN");
        _mockService.Setup(s => s.GetAllTournamentsAsync(It.IsAny<FilterTournamentDto>()))
            .ReturnsAsync(CreatePageResponseWithItems());

        var result = await controller.GetTournaments("", 0, 10, "name", "asc");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PageResponseDto<TournamentResponseDto>>().Subject;
        response.Content.Should().HaveCount(1);
        response.TotalElements.Should().Be(1);
    }

    [Test]
    public async Task GetTournaments_WithSearchFilter_PassesSearchToService()
    {
        var controller = CreateControllerForGetTournaments("ADMIN");
        _mockService.Setup(s => s.GetAllTournamentsAsync(It.Is<FilterTournamentDto>(f => f.Search == "Torneo")))
            .ReturnsAsync(CreateEmptyPageResponse());

        await controller.GetTournaments("Torneo", 0, 10, "name", "asc");

        _mockService.Verify(s => s.GetAllTournamentsAsync(It.Is<FilterTournamentDto>(f => f.Search == "Torneo")), Times.Once);
    }

    [Test]
    public async Task GetTournaments_WithPagination_PassesPaginationToService()
    {
        var controller = CreateControllerForGetTournaments("ADMIN");
        _mockService.Setup(s => s.GetAllTournamentsAsync(It.IsAny<FilterTournamentDto>()))
            .ReturnsAsync(CreateEmptyPageResponse());

        await controller.GetTournaments("", 2, 20, "name", "desc");

        _mockService.Verify(s => s.GetAllTournamentsAsync(It.Is<FilterTournamentDto>(f => f.Page == 2 && f.Size == 20 && f.Direction == "desc")), Times.Once);
    }

    [Test]
    public async Task GetTournaments_NonAdminUserWithValidId_PassesUserIdToService()
    {
        var userId = Ulid.NewUlid();
        var controller = CreateControllerForGetTournaments("SUPERVISOR", userId);
        _mockService.Setup(s => s.GetAllTournamentsAsync(It.Is<FilterTournamentDto>(f => f.UserId == userId)))
            .ReturnsAsync(CreateEmptyPageResponse());

        var result = await controller.GetTournaments("", 0, 10, "name", "asc");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _mockService.Verify(s => s.GetAllTournamentsAsync(It.Is<FilterTournamentDto>(f => f.UserId == userId)), Times.Once);
    }

    [Test]
    public async Task GetTournaments_NonAdminUserWithInvalidId_ReturnsForbid()
    {
        var controller = CreateControllerForGetTournamentsWithInvalidUserId("SUPERVISOR");

        var result = await controller.GetTournaments("", 0, 10, "name", "asc");

        var forbidResult = result.Should().BeOfType<ForbidResult>().Subject;
    }

    #endregion

    #region DeleteTournament Tests

    [Test]
    public async Task DeleteTournament_ExistingId_ReturnsNoContent()
    {
        var id = Ulid.NewUlid();
        _mockService.Setup(s => s.DeleteTournament(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<global::BackEncordados.Common.Utils.Unit, TournamentsErrors>(global::BackEncordados.Common.Utils.Unit.Value));

        var result = await _controller.DeleteTournament(id);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(204);
    }

    [Test]
    public async Task DeleteTournament_TournamentNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var error = new TournamentNotFoundError("Tournament not found");
        _mockService.Setup(s => s.DeleteTournament(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, TournamentsErrors>(error));

        var result = await _controller.DeleteTournament(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task DeleteTournament_ServerError_Returns500()
    {
        var id = Ulid.NewUlid();
        var error = new TournamentsErrors("Server error");
        _mockService.Setup(s => s.DeleteTournament(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, TournamentsErrors>(error));

        var result = await _controller.DeleteTournament(id);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task DeleteTournament_CallsServiceWithCorrectId()
    {
        var id = Ulid.NewUlid();
        _mockService.Setup(s => s.DeleteTournament(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<global::BackEncordados.Common.Utils.Unit, TournamentsErrors>(global::BackEncordados.Common.Utils.Unit.Value));

        await _controller.DeleteTournament(id);

        _mockService.Verify(s => s.DeleteTournament(id), Times.Once);
    }

    #endregion

    #region UpdateTournament Tests

    [Test]
    public async Task UpdateTournament_ValidRequest_ReturnsOkWithTournament()
    {
        var id = Ulid.NewUlid();
        var response = CreateTournamentResponse();
        _mockService.Setup(s => s.UpdateTournament(id, It.IsAny<TournamentPatchDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<TournamentResponseDto, TournamentsErrors>(response));

        var result = await _controller.UpdateTournament(id, CreatePatchRequest());

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task UpdateTournament_TournamentNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var error = new TournamentNotFoundError("Tournament not found");
        _mockService.Setup(s => s.UpdateTournament(id, It.IsAny<TournamentPatchDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDto, TournamentsErrors>(error));

        var result = await _controller.UpdateTournament(id, CreatePatchRequest());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UpdateTournament_ValidationError_ReturnsBadRequest()
    {
        var id = Ulid.NewUlid();
        var error = new TalleresValidationError("Invalid data");
        _mockService.Setup(s => s.UpdateTournament(id, It.IsAny<TournamentPatchDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDto, TournamentsErrors>(error));

        var result = await _controller.UpdateTournament(id, CreatePatchRequest());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task UpdateTournament_ServerError_Returns500()
    {
        var id = Ulid.NewUlid();
        var error = new TournamentsErrors("Server error");
        _mockService.Setup(s => s.UpdateTournament(id, It.IsAny<TournamentPatchDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDto, TournamentsErrors>(error));

        var result = await _controller.UpdateTournament(id, CreatePatchRequest());

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task UpdateTournament_CallsServiceWithCorrectId()
    {
        var id = Ulid.NewUlid();
        _mockService.Setup(s => s.UpdateTournament(id, It.IsAny<TournamentPatchDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<TournamentResponseDto, TournamentsErrors>(CreateTournamentResponse()));

        await _controller.UpdateTournament(id, CreatePatchRequest());

        _mockService.Verify(s => s.UpdateTournament(id, It.IsAny<TournamentPatchDto>()), Times.Once);
    }

    #endregion

    #region AssignWorkerMachine Tests

    [Test]
    public async Task AssignWorkerMachine_ValidRequest_ReturnsOkWithTournament()
    {
        var id = Ulid.NewUlid();
        var request = CreateWorkerAssignmentRequest();
        var response = CreateTournamentDetailsResponse();
        _mockService.Setup(s => s.AssignWorkerMachine(id, request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<TournamentResponseDetailsDto, DomainErrors>(response));

        var result = await _controller.PatchTournament(id, request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task AssignWorkerMachine_TournamentNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var request = CreateWorkerAssignmentRequest();
        var error = new TournamentNotFoundError("Tournament not found");
        _mockService.Setup(s => s.AssignWorkerMachine(id, request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.PatchTournament(id, request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task AssignWorkerMachine_UserNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var request = CreateWorkerAssignmentRequest();
        var error = new UserNotFoundError("User not found");
        _mockService.Setup(s => s.AssignWorkerMachine(id, request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.PatchTournament(id, request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task AssignWorkerMachine_ValidationError_ReturnsBadRequest()
    {
        var id = Ulid.NewUlid();
        var request = CreateWorkerAssignmentRequest();
        var error = new TalleresValidationError("Invalid data");
        _mockService.Setup(s => s.AssignWorkerMachine(id, request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.PatchTournament(id, request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task AssignWorkerMachine_ServerError_Returns500()
    {
        var id = Ulid.NewUlid();
        var request = CreateWorkerAssignmentRequest();
        var error = new TournamentsErrors("Server error");
        _mockService.Setup(s => s.AssignWorkerMachine(id, request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.PatchTournament(id, request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region UnassignWorkerMachine Tests

    [Test]
    public async Task UnassignWorkerMachine_ValidRequest_ReturnsOkWithTournament()
    {
        var id = Ulid.NewUlid();
        var worker = Ulid.NewUlid().ToString();
        var response = CreateTournamentDetailsResponse();
        _mockService.Setup(s => s.UnassignWorkerMachine(id, worker))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<TournamentResponseDetailsDto, DomainErrors>(response));

        var result = await _controller.RemoveWorkerFromTournament(id, worker);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task UnassignWorkerMachine_TournamentNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var worker = Ulid.NewUlid().ToString();
        var error = new TournamentNotFoundError("Tournament not found");
        _mockService.Setup(s => s.UnassignWorkerMachine(id, worker))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.RemoveWorkerFromTournament(id, worker);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UnassignWorkerMachine_UserNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var worker = Ulid.NewUlid().ToString();
        var error = new UserNotFoundError("User not found");
        _mockService.Setup(s => s.UnassignWorkerMachine(id, worker))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.RemoveWorkerFromTournament(id, worker);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UnassignWorkerMachine_ValidationError_ReturnsBadRequest()
    {
        var id = Ulid.NewUlid();
        var worker = Ulid.NewUlid().ToString();
        var error = new TalleresValidationError("Invalid data");
        _mockService.Setup(s => s.UnassignWorkerMachine(id, worker))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.RemoveWorkerFromTournament(id, worker);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task UnassignWorkerMachine_ServerError_Returns500()
    {
        var id = Ulid.NewUlid();
        var worker = Ulid.NewUlid().ToString();
        var error = new TournamentsErrors("Server error");
        _mockService.Setup(s => s.UnassignWorkerMachine(id, worker))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.RemoveWorkerFromTournament(id, worker);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetAssignedWorkers Tests

    [Test]
    public async Task GetAssignedWorkers_ExistingTournament_ReturnsOkWithWorkers()
    {
        var id = Ulid.NewUlid();
        var workers = CreateWorkerMachineList();
        _mockService.Setup(s => s.GetAssignedWorkerMachines(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>(workers));

        var result = await _controller.GetAssignedWorkers(id);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<List<WorkerMachineAssignmentResponseDto>>().Subject;
        response.Should().HaveCount(1);
    }

    [Test]
    public async Task GetAssignedWorkers_TournamentNotFound_ReturnsNotFound()
    {
        var id = Ulid.NewUlid();
        var error = new TournamentNotFoundError("Tournament not found");
        _mockService.Setup(s => s.GetAssignedWorkerMachines(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>(error));

        var result = await _controller.GetAssignedWorkers(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetAssignedWorkers_ServerError_Returns500()
    {
        var id = Ulid.NewUlid();
        var error = new TournamentsErrors("Server error");
        _mockService.Setup(s => s.GetAssignedWorkerMachines(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>(error));

        var result = await _controller.GetAssignedWorkers(id);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region OwnerCreateTournament Tests

    private TournamentsController CreateControllerWithUserClaim(Ulid userId)
    {
        var controller = new TournamentsController(
            NullLogger<TournamentsController>.Instance,
            _mockService.Object
        );

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        controller.Url = new Mock<IUrlHelper>().Object;

        return controller;
    }

    private TournamentsController CreateControllerWithInvalidUserClaim()
    {
        var controller = new TournamentsController(
            NullLogger<TournamentsController>.Instance,
            _mockService.Object
        );

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-ulid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        controller.Url = new Mock<IUrlHelper>().Object;

        return controller;
    }

    [Test]
    public async Task OwnerCreateTournament_ValidRequest_ReturnsCreatedResult()
    {
        var request = CreateOwnerRequest();
        var userId = Ulid.NewUlid();
        var response = CreateTournamentDetailsResponse();
        
        var controller = CreateControllerWithUserClaim(userId);
        _mockService.Setup(s => s.OwnerCreateTournament(request, userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<TournamentResponseDetailsDto, DomainErrors>(response));

        var result = await controller.CreateTournament(request);

        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().Be(response);
    }

    [Test]
    public async Task OwnerCreateTournament_UserNotFound_ReturnsNotFound()
    {
        var request = CreateOwnerRequest();
        var userId = Ulid.NewUlid();
        var error = new UserNotFoundError("Owner not found");
        
        var controller = CreateControllerWithUserClaim(userId);
        _mockService.Setup(s => s.OwnerCreateTournament(request, userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await controller.CreateTournament(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task OwnerCreateTournament_ConflictError_ReturnsConflict()
    {
        var request = CreateOwnerRequest();
        var userId = Ulid.NewUlid();
        var error = new ConflictError("Tournament already exists");
        
        var controller = CreateControllerWithUserClaim(userId);
        _mockService.Setup(s => s.OwnerCreateTournament(request, userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await controller.CreateTournament(request);

        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task OwnerCreateTournament_ValidationError_ReturnsBadRequest()
    {
        var request = CreateOwnerRequest();
        var userId = Ulid.NewUlid();
        var error = new TalleresValidationError("Invalid data");
        
        var controller = CreateControllerWithUserClaim(userId);
        _mockService.Setup(s => s.OwnerCreateTournament(request, userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await controller.CreateTournament(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task OwnerCreateTournament_ServerError_Returns500()
    {
        var request = CreateOwnerRequest();
        var userId = Ulid.NewUlid();
        var error = new TournamentsErrors("Server error");
        
        var controller = CreateControllerWithUserClaim(userId);
        _mockService.Setup(s => s.OwnerCreateTournament(request, userId))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await controller.CreateTournament(request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task OwnerCreateTournament_InvalidUserClaim_ReturnsNotFound()
    {
        var request = CreateOwnerRequest();
        var controller = CreateControllerWithInvalidUserClaim();

        var result = await controller.CreateTournament(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region AssignSupervisor Tests

    [Test]
    public async Task AssignSupervisor_ValidRequest_ReturnsOkWithTournament()
    {
        var request = CreateSupervisorAssignmentRequest();
        var response = CreateTournamentDetailsResponse();
        _mockService.Setup(s => s.AssingSupervisor(request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<TournamentResponseDetailsDto, DomainErrors>(response));

        var result = await _controller.AssignSupervisorToTournament(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task AssignSupervisor_TournamentNotFound_ReturnsNotFound()
    {
        var request = CreateSupervisorAssignmentRequest();
        var error = new TournamentNotFoundError("Tournament not found");
        _mockService.Setup(s => s.AssingSupervisor(request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.AssignSupervisorToTournament(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task AssignSupervisor_UserNotFound_ReturnsNotFound()
    {
        var request = CreateSupervisorAssignmentRequest();
        var error = new UserNotFoundError("User not found");
        _mockService.Setup(s => s.AssingSupervisor(request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.AssignSupervisorToTournament(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task AssignSupervisor_ValidationError_ReturnsBadRequest()
    {
        var request = CreateSupervisorAssignmentRequest();
        var error = new TalleresValidationError("Invalid data");
        _mockService.Setup(s => s.AssingSupervisor(request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.AssignSupervisorToTournament(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task AssignSupervisor_ServerError_Returns500()
    {
        var request = CreateSupervisorAssignmentRequest();
        var error = new TournamentsErrors("Server error");
        _mockService.Setup(s => s.AssingSupervisor(request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.AssignSupervisorToTournament(request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region RemoveSupervisor Tests

    [Test]
    public async Task RemoveSupervisor_ValidRequest_ReturnsOkWithTournament()
    {
        var request = CreateSupervisorAssignmentRequest();
        var response = CreateTournamentDetailsResponse();
        _mockService.Setup(s => s.AnassingSupervisor(request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<TournamentResponseDetailsDto, DomainErrors>(response));

        var result = await _controller.RemoveSupervisorFromTournament(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task RemoveSupervisor_TournamentNotFound_ReturnsNotFound()
    {
        var request = CreateSupervisorAssignmentRequest();
        var error = new TournamentNotFoundError("Tournament not found");
        _mockService.Setup(s => s.AnassingSupervisor(request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.RemoveSupervisorFromTournament(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task RemoveSupervisor_UserNotFound_ReturnsNotFound()
    {
        var request = CreateSupervisorAssignmentRequest();
        var error = new UserNotFoundError("User not found");
        _mockService.Setup(s => s.AnassingSupervisor(request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.RemoveSupervisorFromTournament(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task RemoveSupervisor_ValidationError_ReturnsBadRequest()
    {
        var request = CreateSupervisorAssignmentRequest();
        var error = new TalleresValidationError("Invalid data");
        _mockService.Setup(s => s.AnassingSupervisor(request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.RemoveSupervisorFromTournament(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task RemoveSupervisor_ServerError_Returns500()
    {
        var request = CreateSupervisorAssignmentRequest();
        var error = new TournamentsErrors("Server error");
        _mockService.Setup(s => s.AnassingSupervisor(request))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<TournamentResponseDetailsDto, DomainErrors>(error));

        var result = await _controller.RemoveSupervisorFromTournament(request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion
}