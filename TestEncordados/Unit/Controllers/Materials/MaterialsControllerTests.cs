using BackEncordados.Common.Dto;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Service.Materials;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TestEncordados.Unit.Controllers.Materials;

public class MaterialsControllerTests
{
    private readonly Mock<IMaterialsService> _mockService;
    private readonly Mock<IValidator<MaterialRequestDto>> _mockValidator;
    private readonly BackEncordados.Materials.Controller.MaterialsController _controller;

    public MaterialsControllerTests()
    {
        _mockService = new Mock<IMaterialsService>();
        _mockValidator = new Mock<IValidator<MaterialRequestDto>>();
        _controller = new BackEncordados.Materials.Controller.MaterialsController(
            NullLogger<BackEncordados.Materials.Controller.MaterialsController>.Instance,
            _mockService.Object,
            _mockValidator.Object
        );
    }

    private static MaterialResponseDto CreateMaterialResponse() => new(
        Id: 1,
        TournamentId: Ulid.NewUlid(),
        Marca: "Wilson",
        Modelo: "Pro Staff",
        Stock: 10,
        Precio: 25.99,
        MaterialType: "Racket"
    );

    private static MaterialRequestDto CreateMaterialRequest() => new()
    {
        Marca = "Wilson",
        TournamentId = Ulid.NewUlid(),
        Modelo = "Pro Staff",
        Stock = 10,
        Precio = 25.99,
        Type = "Racket"
    };

    private static MaterialPatchDto CreateMaterialPatch() => new()
    {
        Marca = "Wilson",
        Modelo = "Pro Staff V14",
        Stock = 20,
        Precio = 29.99,
        Type = "Racket"
    };

    private static PageResponseDto<MaterialResponseDto> CreateEmptyPageResponse() => new(
        Content: [],
        TotalPages: 0,
        TotalElements: 0,
        PageSize: 10,
        PageNumber: 0,
        TotalPageElements: 0,
        SortBy: "id",
        Direction: "asc"
    );

    private static PageResponseDto<MaterialResponseDto> CreatePageResponseWithItems() => new(
        Content: [CreateMaterialResponse()],
        TotalPages: 1,
        TotalElements: 1,
        PageSize: 10,
        PageNumber: 0,
        TotalPageElements: 1,
        SortBy: "id",
        Direction: "asc"
    );

    #region GetAll Tests

    [Test]
    public async Task GetAll_EmptyResults_ReturnsOkWithEmptyList()
    {
        _mockService.Setup(s => s.FindAllAsync(It.IsAny<MaterialFilterDto>()))
            .ReturnsAsync(CreateEmptyPageResponse());

        var result = await _controller.GetAll(null, "id", 0, 10, "asc", "");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PageResponseDto<MaterialResponseDto>>().Subject;
        response.Content.Should().BeEmpty();
        response.TotalElements.Should().Be(0);
    }

    [Test]
    public async Task GetAll_WithResults_ReturnsOkWithItems()
    {
        _mockService.Setup(s => s.FindAllAsync(It.IsAny<MaterialFilterDto>()))
            .ReturnsAsync(CreatePageResponseWithItems());

        var result = await _controller.GetAll(null, "id", 0, 10, "asc", "");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PageResponseDto<MaterialResponseDto>>().Subject;
        response.Content.Should().HaveCount(1);
        response.TotalElements.Should().Be(1);
    }

    [Test]
    public async Task GetAll_WithSearchFilter_PassesSearchToService()
    {
        _mockService.Setup(s => s.FindAllAsync(It.Is<MaterialFilterDto>(f => f.Search == "Wilson")))
            .ReturnsAsync(CreateEmptyPageResponse());

        await _controller.GetAll(null, "id", 0, 10, "asc", "Wilson");

        _mockService.Verify(s => s.FindAllAsync(It.Is<MaterialFilterDto>(f => f.Search == "Wilson")), Times.Once);
    }

    [Test]
    public async Task GetAll_WithTournamentFilter_PassesTournamentIdToService()
    {
        var tournamentId = Ulid.NewUlid();
        _mockService.Setup(s => s.FindAllAsync(It.Is<MaterialFilterDto>(f => f.TournamentId == tournamentId)))
            .ReturnsAsync(CreateEmptyPageResponse());

        await _controller.GetAll(tournamentId, "id", 0, 10, "asc", "");

        _mockService.Verify(s => s.FindAllAsync(It.Is<MaterialFilterDto>(f => f.TournamentId == tournamentId)), Times.Once);
    }

    [Test]
    public async Task GetAll_WithPagination_PassesPaginationToService()
    {
        _mockService.Setup(s => s.FindAllAsync(It.IsAny<MaterialFilterDto>()))
            .ReturnsAsync(CreateEmptyPageResponse());

        await _controller.GetAll(null, "id", 2, 20, "desc", "");

        _mockService.Verify(s => s.FindAllAsync(It.Is<MaterialFilterDto>(f => f.Page == 2 && f.Size == 20 && f.Direction == "desc")), Times.Once);
    }

    #endregion

    #region GetById Tests

    [Test]
    public async Task GetById_ExistingId_ReturnsOkWithMaterial()
    {
        var response = CreateMaterialResponse();
        _mockService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<MaterialResponseDto, MaterialError>(response));

        var result = await _controller.GetById(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var error = new MaterialNotFoundError("Material not found");
        _mockService.Setup(s => s.FindByIdAsync(999))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<MaterialResponseDto, MaterialError>(error));

        var result = await _controller.GetById(999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetById_ServerError_Returns500()
    {
        var error = new MaterialError("Server error");
        _mockService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<MaterialResponseDto, MaterialError>(error));

        var result = await _controller.GetById(1);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetByName Tests

    [Test]
    public async Task GetByName_ExistingName_ReturnsOkWithMaterial()
    {
        var response = CreateMaterialResponse();
        _mockService.Setup(s => s.FindByNameAsync("Pro Staff"))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<MaterialResponseDto, MaterialError>(response));

        var result = await _controller.GetByName("Pro Staff");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task GetByName_NonExistingName_ReturnsNotFound()
    {
        var error = new MaterialNotFoundError("Material not found");
        _mockService.Setup(s => s.FindByNameAsync("NonExistent"))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<MaterialResponseDto, MaterialError>(error));

        var result = await _controller.GetByName("NonExistent");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetByName_ServerError_Returns500()
    {
        var error = new MaterialError("Server error");
        _mockService.Setup(s => s.FindByNameAsync("Pro Staff"))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<MaterialResponseDto, MaterialError>(error));

        var result = await _controller.GetByName("Pro Staff");

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Create Tests

    [Test]
    public async Task Create_ValidRequest_ReturnsCreatedResult()
    {
        var request = CreateMaterialRequest();
        var response = CreateMaterialResponse();
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<MaterialRequestDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreateAsync(It.IsAny<MaterialRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<MaterialResponseDto, MaterialError>(response));

        var result = await _controller.Create(request);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().Be(response);
    }

    [Test]
    public async Task Create_InvalidRequest_ReturnsBadRequest()
    {
        var request = CreateMaterialRequest();
        var validationErrors = new List<ValidationFailure>
        {
            new("Marca", "La marca es obligatoria")
        };
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<MaterialRequestDto>(), default))
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
    public async Task Create_ConflictError_ReturnsConflict()
    {
        var request = CreateMaterialRequest();
        var error = new MaterialConflictError("Material already exists");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<MaterialRequestDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreateAsync(It.IsAny<MaterialRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<MaterialResponseDto, MaterialError>(error));

        var result = await _controller.Create(request);

        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task Create_ServerError_Returns500()
    {
        var request = CreateMaterialRequest();
        var error = new MaterialError("Server error");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<MaterialRequestDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreateAsync(It.IsAny<MaterialRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<MaterialResponseDto, MaterialError>(error));

        var result = await _controller.Create(request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task Create_NonConflictValidationErrorFromService_Returns500()
    {
        var request = CreateMaterialRequest();
        var error = new MaterialValidationError("Invalid data");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<MaterialRequestDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreateAsync(It.IsAny<MaterialRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<MaterialResponseDto, MaterialError>(error));

        var result = await _controller.Create(request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task Create_CallsValidator_WithCorrectRequest()
    {
        var request = CreateMaterialRequest();
        _mockValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreateAsync(It.IsAny<MaterialRequestDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<MaterialResponseDto, MaterialError>(CreateMaterialResponse()));

        await _controller.Create(request);

        _mockValidator.Verify(v => v.ValidateAsync(request, default), Times.Once);
    }

    #endregion

    #region Update Tests

    [Test]
    public async Task Update_ValidRequest_ReturnsOkWithMaterial()
    {
        var response = CreateMaterialResponse();
        _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<MaterialPatchDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<MaterialResponseDto, MaterialError>(response));

        var result = await _controller.Update(1, CreateMaterialPatch());

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Test]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var error = new MaterialNotFoundError("Material not found");
        _mockService.Setup(s => s.UpdateAsync(999, It.IsAny<MaterialPatchDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<MaterialResponseDto, MaterialError>(error));

        var result = await _controller.Update(999, CreateMaterialPatch());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Update_ConflictError_ReturnsConflict()
    {
        var error = new MaterialConflictError("Material already exists");
        _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<MaterialPatchDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<MaterialResponseDto, MaterialError>(error));

        var result = await _controller.Update(1, CreateMaterialPatch());

        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task Update_ServerError_Returns500()
    {
        var error = new MaterialError("Server error");
        _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<MaterialPatchDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<MaterialResponseDto, MaterialError>(error));

        var result = await _controller.Update(1, CreateMaterialPatch());

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task Update_CallsServiceWithCorrectId()
    {
        const long id = 42;
        _mockService.Setup(s => s.UpdateAsync(id, It.IsAny<MaterialPatchDto>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<MaterialResponseDto, MaterialError>(CreateMaterialResponse()));

        await _controller.Update(id, CreateMaterialPatch());

        _mockService.Verify(s => s.UpdateAsync(id, It.IsAny<MaterialPatchDto>()), Times.Once);
    }

    #endregion

    #region Delete Tests

    [Test]
    public async Task Delete_ExistingId_ReturnsOk()
    {
        _mockService.Setup(s => s.DeleteAsync(1))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<global::BackEncordados.Common.Utils.Unit, MaterialError>(global::BackEncordados.Common.Utils.Unit.Value));

        var result = await _controller.Delete(1);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var error = new MaterialNotFoundError("Material not found");
        _mockService.Setup(s => s.DeleteAsync(999))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, MaterialError>(error));

        var result = await _controller.Delete(999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Delete_ServerError_Returns500()
    {
        var error = new MaterialError("Server error");
        _mockService.Setup(s => s.DeleteAsync(1))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<global::BackEncordados.Common.Utils.Unit, MaterialError>(error));

        var result = await _controller.Delete(1);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task Delete_CallsServiceWithCorrectId()
    {
        const long id = 42;
        _mockService.Setup(s => s.DeleteAsync(id))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success<global::BackEncordados.Common.Utils.Unit, MaterialError>(global::BackEncordados.Common.Utils.Unit.Value));

        await _controller.Delete(id);

        _mockService.Verify(s => s.DeleteAsync(id), Times.Once);
    }

    #endregion
}