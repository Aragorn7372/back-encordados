using BackEncordados.Common.Dto;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Service.Materials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CSharpFunctionalExtensions;
namespace BackEncordados.Materials.Controller;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MaterialsController(ILogger<MaterialsController> logger, IMaterialsService service) : ControllerBase{
    
    [HttpGet]
    [ProducesResponseType(typeof(PageResponseDto<MaterialResponseDto>),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(policy: "RequireAdminRole")]
    public async Task<IActionResult> GetAll(
        [FromQuery] long? TournamentId,
        [FromQuery] string sortBy = "id",
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string direction = "asc",
        [FromQuery] string search = "") {
        logger.LogInformation("Get all materials with search {Search}, sortBy {SortBy}, page {Page}, size {Size} and direction {Direction}",
            search, sortBy, page, size, direction);
        var filter = new MaterialFilterDto(
            TournamentId: TournamentId,
            Search: search,
            SortBy: sortBy,
            Page: page,
            Size: size,
            Direction: direction);
        return Ok(await service.FindAllAsync(filter));
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(MaterialResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireAdminRole")]
    public async Task<IActionResult> GetById(long id) {
        logger.LogInformation("Get material by id {Id}", id);
        return await service.FindByIdAsync(id).Match(
            material => Ok(material),
            error => error switch {
                MaterialNotFoundError => NotFound(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    [HttpGet("name/{name}")]
    [ProducesResponseType(typeof(MaterialResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireAdminRole")]
    public async Task<IActionResult> GetByName(string name) {
        logger.LogInformation("Get material by name {Name}", name);
        return await service.FindByNameAsync(name).Match(
            material => Ok(material),
            error => error switch {
                MaterialNotFoundError => NotFound(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    [HttpPost]
    [ProducesResponseType(typeof(MaterialResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireAdminRole")]
    public async Task<IActionResult> Create(MaterialRequestDto request) {
        logger.LogInformation("Create material with name {Name}", request.Modelo);
        return await service.CreateAsync(request).Match(
            onSuccess: material => CreatedAtAction(nameof(GetById), new { id = material.Id }, material),
            onFailure: error => error switch {
                MaterialConflictError => Conflict(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(MaterialResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireAdminRole")]
    public async Task<IActionResult> Update(long id, MaterialPatchDto request) {
        logger.LogInformation("Update material with id {Id}", id);
        return await service.UpdateAsync(id, request).Match(
            onSuccess: material => Ok(material),
            onFailure: error => error switch {
                MaterialNotFoundError => NotFound(new { message = error.Error }),
                MaterialConflictError => Conflict(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(MaterialResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireAdminRole")]
    public async Task<IActionResult> Delete(long id) {
        logger.LogInformation("Delete material with id {Id}", id);
        return await service.DeleteAsync(id).Match(
            onSuccess: _ => Ok(new { message = "Material deleted successfully" }),
            onFailure: error => error switch {
                MaterialNotFoundError => NotFound(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

}