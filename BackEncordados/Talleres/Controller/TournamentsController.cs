using BackEncordados.Common.Dto;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Error;
using BackEncordados.Talleres.Service;
using BackEncordados.Usuarios.Errors;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
    public async Task<IActionResult> CreateTournament([FromForm] TournamentAdminRequestDto adminRequest) {
        logger.LogInformation("Received adminRequest to create tournament: {@Request}", adminRequest);
        return await tournamentsService.CreateTournament(adminRequest).Match(
            onSuccess: tournament => CreatedAtAction(nameof(GetTournament), new { id = tournament.Id }, tournament),
            onFailure: error => error switch {
                UserNotFoundError => NotFound(error.Error),
                ConflictError => Conflict(error.Error),
                ValidationError => BadRequest(error.Error),
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
                UserNotFoundError => NotFound(error.Error),
                TournamentNotFoundError => NotFound(error.Error),
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
                { } e => e switch {
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
    [Authorize(Policy = "RequireSupervisorRole")]
    public async Task<IActionResult> GetTournaments(
        [FromQuery] string search="",
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "name",
        [FromQuery] string direction = "asc") {
        var filter = new FilterTournamentDto(search, null,page, pageSize, sortBy, direction);
        var role= User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        if (role != Usuarios.Model.User.UserRoles.ADMIN) {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (Ulid.TryParse(idClaim, out Ulid userId))
                filter.UserId = userId;
            else return Forbid("usuario no identicado");
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
                TournamentNotFoundError => NotFound(error.Error),
                _ => StatusCode(500, "An unexpected error occurred.")
            });
    }
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(TournamentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> UpdateTournament(long id, [FromBody] TournamentPatchDto request) {
        logger.LogInformation("Received request to update tournament: {id} with data: {@Request}", id, request);
        return await tournamentsService.UpdateTournament(id, request).Match(
            success => Ok(success),
            error => error switch {
                TournamentNotFoundError => NotFound(error.Error),
                ValidationError => BadRequest(error.Error),
                _ => StatusCode(500, "An unexpected error occurred.")
            });
     }

    [HttpPatch("add-worker/{id:long}")]
    [ProducesResponseType(typeof(TournamentResponseDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> PatchTournament(
        long id,
        [FromBody] WorkerMachineAssignmentRequestDto worker) {
        logger.LogInformation("Received request to patch tournament with worker assignment: {@Worker}", worker);
        return await tournamentsService.AssignWorkerMachine(id,worker).Match(
            success => Ok(success),
            error => error switch {
                TournamentNotFoundError => NotFound(error.Error),
                UserNotFoundError => NotFound(error.Error),
                ValidationError => BadRequest(error.Error),
                _ => StatusCode(500, "An unexpected error occurred.")
            });
        
    }
    [HttpPatch("remove-worker/{id:long}")]
    [ProducesResponseType(typeof(TournamentResponseDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> RemoveWorkerFromTournament(
        long id,
        [FromBody] string worker) {
        logger.LogInformation("Received request to patch tournament with worker removal: {@Worker}", worker);
        return await tournamentsService.UnassignWorkerMachine(id,worker).Match(
            success => Ok(success),
            error => error switch {
                TournamentNotFoundError => NotFound(error.Error),
                UserNotFoundError => NotFound(error.Error),
                ValidationError => BadRequest(error.Error),
                _ => StatusCode(500, "An unexpected error occurred.")
            });
    }  
    
    [HttpGet("{id:long}/workers")]
    [ProducesResponseType(typeof(List<WorkerMachineAssignmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> GetAssignedWorkers(long id) {
        logger.LogInformation("Received request to get assigned workers for tournament: {id}", id);
        return await tournamentsService.GetAssignedWorkerMachines(id).Match(
            success => Ok(success),
            error => error switch {
                    TournamentNotFoundError => NotFound(error.Error),
                    _ => StatusCode(500, error.Error)
            });
     }
    [HttpPost("owner-create")]
    [ProducesResponseType(typeof(TournamentResponseDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType( StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> CreateTournament([FromForm] TournamentRequestDto adminRequest) {
        logger.LogInformation("Received adminRequest to create tournament: {@Request}", adminRequest);
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Ulid.TryParse(userIdClaim.Value, out var userId))
            return NotFound(new { message = "User ID claim not found or invalid" });
        return await tournamentsService.OwnerCreateTournament(adminRequest,userId).Match(
            onSuccess: tournament => CreatedAtAction(nameof(GetTournament), new { id = tournament.Id }, tournament),
            onFailure: error => error switch {
                UserNotFoundError => NotFound(error.Error),
                ConflictError => Conflict(error.Error),
                ValidationError => BadRequest(error.Error),
                _ => StatusCode(500, "An unexpected error occurred.")
            });
    }

    [HttpPatch("assign-supervisor")]
    [ProducesResponseType(typeof(TournamentResponseDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> AssignSupervisorToTournament(
        [FromBody] SupervisorAsignmentRequestDto request) {
        logger.LogInformation("Received request to assign supervisor to tournament: {@Request}", request);
        return await tournamentsService.AssingSupervisor(request).Match(
            success => Ok(success),
            error => error switch {
                TournamentNotFoundError => NotFound(error.Error),
                UserNotFoundError => NotFound(error.Error),
                ValidationError => BadRequest(error.Error),
                _ => StatusCode(500, "An unexpected error occurred.")
            });
    }
    
    [HttpPatch("remove-supervisor")]
    [ProducesResponseType(typeof(TournamentResponseDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> RemoveSupervisorFromTournament(
        [FromBody] SupervisorAsignmentRequestDto request) {
        logger.LogInformation("Received request to remove supervisor from tournament: {@Request}", request);
        return await tournamentsService.AnassingSupervisor(request).Match(
            success => Ok(success),
            error => error switch {
                TournamentNotFoundError => NotFound(error.Error),
                UserNotFoundError => NotFound(error.Error),
                ValidationError => BadRequest(error.Error),
                _ => StatusCode(500, "An unexpected error occurred.")
            });
     }
    

}