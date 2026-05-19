using BackEncordados.Common.Dto;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Service.Cuerdas;
using BackEncordados.Materials.Validator.Strings;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEncordados.Materials.Controller;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CuerdasController(
    ILogger<CuerdasController> logger, 
    ICuerdasService service,
    IValidator<CuerdaRequestDto> validator) : ControllerBase{
    
    [HttpGet]
    [ProducesResponseType(typeof(PageResponseDto<CuerdaResponseDto>),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(policy: "RequireEncorderRole")]
    public async Task<IActionResult> GetAll(
        [FromQuery] Ulid? tournamentId,
        [FromQuery] string sortBy = "id",
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string direction = "asc",
        [FromQuery] string search = "") {
        logger.LogInformation("Get all cuerdas with search {Search}, sortBy {SortBy}, page {Page}, size {Size}, direction {Direction} and tournamentId {TournamentId}",
            search, sortBy, page, size, direction, tournamentId);
        var filter = new CuerdaFilterdto(
            TournamentId: tournamentId,
            Search: search,
            SortBy: sortBy,
            Page: page,
            Size: size,
            Direction: direction);
        return Ok(await service.FindAllAsync(filter));
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(CuerdaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireOwnerRole")]
    public async Task<IActionResult> GetById(long id) {
        logger.LogInformation("Get cuerda by id {Id}", id);
        return await service.FindByIdAsync(id).Match(
            cuerda => Ok(cuerda),
            error => error switch {
                CuerdaNotFoundError => NotFound(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    [HttpGet("name/{name}")]
    [ProducesResponseType(typeof(CuerdaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireOwnerRole")]
    public async Task<IActionResult> GetByName(string name) {
        logger.LogInformation("Get cuerda by name {Name}", name);
        return await service.FindByNameAsync(name).Match(
            cuerda => Ok(cuerda),
            error => error switch {
                CuerdaNotFoundError => NotFound(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    [HttpPost]
    [ProducesResponseType(typeof(CuerdaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireAdminRole")]
    public async Task<IActionResult> Create(CuerdaRequestDto request) {
        logger.LogInformation("Create cuerda with name {Name}", request.Modelo);
        
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        
        return await service.CreateAsync(request).Match(
            onSuccess: cuerda => CreatedAtAction(nameof(GetById), new { id = cuerda.Id }, cuerda),
            onFailure: error => error switch {
                ConflictError => Conflict(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(CuerdaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireAdminRole")]
    public async Task<IActionResult> Update(long id, CuerdaPatchDto request) {
        logger.LogInformation("Update cuerda with id {Id}", id);
        return await service.UpdateAsync(id, request).Match(
            onSuccess: cuerda => Ok(cuerda),
            onFailure: error => error switch {
                CuerdaNotFoundError => NotFound(new { message = error.Error }),
                ConflictError => Conflict(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(CuerdaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireAdminRole")]
    public async Task<IActionResult> Delete(long id) {
        logger.LogInformation("Delete cuerda with id {Id}", id);
        return await service.DeleteAsync(id).Match(
            onSuccess: _ => Ok(new { message = "cuerda deleted successfully" }),
            onFailure: error => error switch {
                CuerdaNotFoundError => NotFound(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

}