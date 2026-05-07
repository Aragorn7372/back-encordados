using BackEncordados.Common.Database.Config;
using BackEncordados.Common.Dto;
using BackEncordados.Common.Errors;
using BackEncordados.Common.Utils;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Error;
using BackEncordados.Talleres.Service;
using BackEncordados.Usuarios.Errors;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConflictError = BackEncordados.Talleres.Error.ConflictError;
using ValidationError = BackEncordados.Talleres.Error.ValidationError;

namespace BackEncordados.Talleres.Controller;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TournamentsController(ILogger<TournamentsController> logger, ITournamentService tournamentsService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TournamentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType( StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> CreateTournament([FromBody] TournamentRequestDto request) {
        logger.LogInformation("Received request to create tournament: {@Request}", request);
        return await tournamentsService.CreateTournament(request).Match(
            onSuccess: tournament => CreatedAtAction(nameof(GetTournament), new { id = tournament.Id }, tournament),
            onFailure: error => error switch {
                
                DomainErrors e => e switch {
                    
                    UserNotFoundError => NotFound(e.Error),
                    ConflictError => Conflict(e.Error),
                    ValidationError => BadRequest(e.Error),
                    _ => StatusCode(500, e.Error)
                },
                _ => StatusCode(500, "An unexpected error occurred.")
            });
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(TournamentResponseDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireEncorderRole")]
    public async Task<IActionResult> GetTournament(long id) {
        logger.LogInformation("Received request to get tournament: {id}", id);
     return await tournamentsService.GetTournament(id).Match(
            success => Ok(success),
            error => error switch {
                DomainErrors e => e switch {
                    UserNotFoundError => NotFound(e.Error),
                    TournamentNotFoundError => NotFound(e.Error),
                    _ => StatusCode(500, e.Error)
                },
                _ => StatusCode(500, "An unexpected error occurred.")
            });
    }
    [HttpGet("name/{name}")]
    [ProducesResponseType(typeof(TournamentResponseDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireEncorderRole")]
    public async Task<IActionResult> GetTournamentByName(string name) {
        logger.LogInformation("Received request to get tournament by name: {name}", name);
        return await tournamentsService.GetTournamentByName(name).Match(
            success => Ok(success),
            error => error switch {
                DomainErrors e => e switch {
                    UserNotFoundError => NotFound(e.Error),
                    TournamentNotFoundError => NotFound(e.Error),
                    _ => StatusCode(500, e.Error)
                },
                _ => StatusCode(500, "An unexpected error occurred.")
            });
     }
    [HttpGet]
    [ProducesResponseType(typeof(PageResponseDto<TournamentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireEncorderRole")]
    public async Task<IActionResult> GetTournaments(
        [FromQuery] string search,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "name",
        [FromQuery] string direction = "asc") {
        var filter = new FilterTournamentDto(search, null,page, pageSize, sortBy, direction);
        var role= User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        if (role != Usuarios.Model.User.UserRoles.ADMIN && 
            role != Usuarios.Model.User.UserRoles.OWNER) {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (Guid.TryParse(idClaim, out Guid userId))
                filter.UserId = userId;
            return Forbid("usuario no identicado");
        }
            
        logger.LogInformation("Received request to get tournaments: page {page}, pageSize {pageSize}", page, pageSize);
        return Ok(await tournamentsService.GetAllTournamentsAsync(filter));
     }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> DeleteTournament(long id) {
        logger.LogInformation("Received request to delete tournament: {id}", id);
        return await tournamentsService.DeleteTournament(id).Match(
            success =>   StatusCode(204, success),
            error => error switch {
                DomainErrors e => e switch {
                    TournamentNotFoundError => NotFound(e.Error),
                    _ => StatusCode(500, e.Error)
                },
                _ => StatusCode(500, "An unexpected error occurred.")
            });
    }
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(TournamentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> UpdateTournament(long id, [FromBody] TournamentPatchDto request) {
        logger.LogInformation("Received request to update tournament: {id} with data: {@Request}", id, request);
        return await tournamentsService.UpdateTournament(id, request).Match(
            success => Ok(success),
            error => error switch {
                DomainErrors e => e switch {
                    TournamentNotFoundError => NotFound(e.Error),
                    UserNotFoundError => NotFound(e.Error),
                    ValidationError => BadRequest(e.Error),
                    _ => StatusCode(500, e.Error)
                },
                _ => StatusCode(500, "An unexpected error occurred.")
            });
     }

    [HttpPatch("add-worker/{id:long}")]
    [ProducesResponseType(typeof(TournamentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> PatchTournament(
        long id,
        [FromBody] WorkerMachineAssignmentRequestDto worker) {
        logger.LogInformation("Received request to patch tournament with worker assignment: {@Worker}", worker);
        return await tournamentsService.AssignWorkerMachine(id,worker).Match(
            success => Ok(success),
            error => error switch {
                DomainErrors e => e switch {
                    TournamentNotFoundError => NotFound(e.Error),
                    UserNotFoundError => NotFound(e.Error),
                    ValidationError => BadRequest(e.Error),
                    _ => StatusCode(500, e.Error)
                },
                _ => StatusCode(500, "An unexpected error occurred.")
            });
        
    }
    [HttpPatch("remove-worker/{id:long}")]
    [ProducesResponseType(typeof(TournamentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> RemoveWorkerFromTournament(
        long id,
        [FromBody] string worker) {
        logger.LogInformation("Received request to patch tournament with worker removal: {@Worker}", worker);
        return await tournamentsService.UnassignWorkerMachine(id,worker).Match(
            success => Ok(success),
            error => error switch {
                DomainErrors e => e switch {
                    TournamentNotFoundError => NotFound(e.Error),
                    UserNotFoundError => NotFound(e.Error),
                    ValidationError => BadRequest(e.Error),
                    _ => StatusCode(500, e.Error)
                }, 
                _ => StatusCode(500, "An unexpected error occurred.") 
            });
    }  
    
    [HttpGet("{id:long}/workers")]
    [ProducesResponseType(typeof(List<WorkerMachineAssignmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> GetAssignedWorkers(long id) {
        logger.LogInformation("Received request to get assigned workers for tournament: {id}", id);
        return await tournamentsService.GetAssignedWorkerMachines(id).Match(
            success => Ok(success),
            error => error switch {
                DomainErrors e => e switch {
                    TournamentNotFoundError => NotFound(e.Error),
                    _ => StatusCode(500, e.Error)
                },
                _ => StatusCode(500, "An unexpected error occurred.")
            });
     }

}