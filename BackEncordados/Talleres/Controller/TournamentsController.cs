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

/// <summary>
/// Controlador de API para la gestión de torneos (CRUD, asignación de trabajadores, máquinas y supervisores).
/// </summary>
/// <remarks>
/// <para>Proporciona diez endpoints para la gestión completa de torneos:</para>
/// <list type="table">
///   <listheader>
///     <term>Endpoint</term>
///     <description>Método</description>
///     <description>Policy requerida</description>
///     <description>Uso</description>
///   </listheader>
///   <item>
///     <term><c>POST api/tournaments</c></term>
///     <description><c>CreateTournament</c> (Admin)</description>
///     <description>RequireAdminRole</description>
///     <description>Crea torneo especificando OwnerId.</description>
///   </item>
///   <item>
///     <term><c>GET api/tournaments/{id}</c></term>
///     <description><c>GetTournament</c></description>
///     <description>RequireEncorderRole</description>
///     <description>Obtiene detalle completo del torneo.</description>
///   </item>
///   <item>
///     <term><c>GET api/tournaments/name/{name}</c></term>
///     <description><c>GetTournamentByName</c></description>
///     <description>RequireEncorderRole</description>
///     <description>Busca torneo por nombre exacto.</description>
///   </item>
///   <item>
///     <term><c>GET api/tournaments</c></term>
///     <description><c>GetTournaments</c></description>
///     <description>RequireSupervisorRole</description>
///     <description>Lista paginada con filtros (filtro automático por usuario si no es admin).</description>
///   </item>
///   <item>
///     <term><c>DELETE api/tournaments/{id}</c></term>
///     <description><c>DeleteTournament</c></description>
///     <description>RequireAdminRole</description>
///     <description>Elimina torneo (soft delete).</description>
///   </item>
///   <item>
///     <term><c>PUT api/tournaments/{id}</c></term>
///     <description><c>UpdateTournament</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Actualiza datos del torneo.</description>
///   </item>
///   <item>
///     <term><c>PATCH api/tournaments/add-worker/{id}</c></term>
///     <description><c>PatchTournament</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Asigna trabajador + máquina.</description>
///   </item>
///   <item>
///     <term><c>PATCH api/tournaments/remove-worker/{id}</c></term>
///     <description><c>RemoveWorkerFromTournament</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Desasigna trabajador y máquina.</description>
///   </item>
///   <item>
///     <term><c>GET api/tournaments/{id}/workers</c></term>
///     <description><c>GetAssignedWorkers</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Lista asignaciones trabajador-máquina.</description>
///   </item>
///   <item>
///     <term><c>POST api/tournaments/owner-create</c></term>
///     <description><c>CreateTournament</c> (Owner)</description>
///     <description>RequireOwnerRole</description>
///     <description>Crea torneo con ownerId del JWT.</description>
///   </item>
///   <item>
///     <term><c>PATCH api/tournaments/assign-supervisor</c></term>
///     <description><c>AssignSupervisorToTournament</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Asigna supervisor al torneo.</description>
///   </item>
///   <item>
///     <term><c>PATCH api/tournaments/remove-supervisor</c></term>
///     <description><c>RemoveSupervisorFromTournament</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Desasigna supervisor del torneo.</description>
///   </item>
/// </list>
/// <para>Los errores se mapean: 400 (validación), 403 (Forbid si el usuario no está identificado), 404 (no encontrado), 409 (conflicto), 500 (error interno).</para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TournamentsController(ILogger<TournamentsController> logger, ITournamentService tournamentsService) : ControllerBase
{
    /// <summary>
    /// Crea un nuevo torneo (solo administradores, especificando OwnerId).
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Recibe el DTO con nombre, OwnerId, fechas y logotipo opcional.</description></item>
    ///   <item><description>Delega la creación a <c>tournamentsService.CreateTournament</c>.</description></item>
    ///   <item><description>Si es exitoso, retorna 201 Created con Location header apuntando a <c>GetTournament</c>.</description></item>
    ///   <item><description>Los errores se mapean: 404 (Owner no encontrado), 409 (conflicto), 400 (validación).</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireAdminRole</c>.</para>
    /// </remarks>
    /// <param name="adminRequest">DTO multipart con nombre, OwnerId, fechas y logotipo opcional.</param>
    /// <returns>201 Created con el DTO detallado del torneo creado.</returns>
    /// <response code="201">Torneo creado correctamente.</response>
    /// <response code="400">Error de validación.</response>
    /// <response code="404">Owner no encontrado.</response>
    /// <response code="409">Conflicto.</response>
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
            onSuccess: tournament => Created(Url.Action("GetTournament", "Tournaments", new { id = tournament.Id }, Request.Scheme)!, tournament),
            onFailure: error => error switch {
                UserNotFoundError => NotFound(error.Error),
                ConflictError => Conflict(error.Error),
                ValidationError => BadRequest(error.Error),
                _ => StatusCode(500, "An unexpected error occurred.")
            });
    }

    /// <summary>
    /// Obtiene el detalle completo de un torneo por ULID.
    /// </summary>
    /// <remarks>
    /// <para>Requiere policy <c>RequireEncorderRole</c>.</para>
    /// </remarks>
    /// <param name="id">ULID del torneo.</param>
    /// <returns>DTO detallado del torneo con usuarios, owner y supervisores.</returns>
    /// <response code="200">Torneo encontrado.</response>
    /// <response code="404">Torneo no encontrado.</response>
    [HttpGet("{id:ulid}", Name = "GetTournament")]
    [ProducesResponseType(typeof(TournamentResponseDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireEncorderRole")]
    public async Task<IActionResult> GetTournament(Ulid id) {
        logger.LogInformation("Received request to get tournament: {id}", id);
     return await tournamentsService.GetTournament(id).Match(
            success => Ok(success),
            error => error switch {
                UserNotFoundError => NotFound(error.Error),
                TournamentNotFoundError => NotFound(error.Error),
                _ => StatusCode(500, "An unexpected error occurred.")
            });
    }

    /// <summary>
    /// Busca un torneo por su nombre exacto.
    /// </summary>
    /// <param name="name">Nombre exacto del torneo.</param>
    /// <returns>DTO detallado del torneo.</returns>
    /// <response code="200">Torneo encontrado.</response>
    /// <response code="404">Torneo no encontrado.</response>
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

    /// <summary>
    /// Obtiene una lista paginada de torneos con filtros.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Construye un <see cref="FilterTournamentDto"/> a partir de los query parameters.</description></item>
    ///   <item><description>Si el usuario no es ADMIN, filtra automáticamente por su UserId (<c>NameIdentifier</c> claim)
    ///   para mostrar solo torneos donde participa (como owner, worker o supervisor).</description></item>
    ///   <item><description>Delega la consulta a <c>tournamentsService.GetAllTournamentsAsync</c>.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireSupervisorRole</c>.</para>
    /// </remarks>
    /// <param name="search">Término de búsqueda textual.</param>
    /// <param name="page">Número de página.</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="sortBy">Campo de ordenación.</param>
    /// <param name="direction">Dirección: asc o desc.</param>
    /// <returns>Página de DTOs básicos de torneo.</returns>
    /// <response code="200">Lista paginada de torneos.</response>
    /// <response code="403">Usuario no identificado (claim inválido).</response>
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

    /// <summary>
    /// Elimina un torneo (soft delete).
    /// </summary>
    /// <param name="id">ULID del torneo a eliminar.</param>
    /// <response code="204">Torneo eliminado correctamente.</response>
    /// <response code="404">Torneo no encontrado.</response>
    [HttpDelete("{id:ulid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> DeleteTournament(Ulid id) {
        logger.LogInformation("Received request to delete tournament: {id}", id);
        return await tournamentsService.DeleteTournament(id).Match(
            success =>   StatusCode(204, success),
            error => error switch {
                TournamentNotFoundError => NotFound(error.Error),
                _ => StatusCode(500, "An unexpected error occurred.")
            });
    }

    /// <summary>
    /// Actualiza parcialmente un torneo existente.
    /// </summary>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="request">DTO multipart con campos opcionales (nombre, fechas, logotipo).</param>
    /// <returns>DTO básico del torneo actualizado.</returns>
    /// <response code="200">Torneo actualizado correctamente.</response>
    /// <response code="400">Error de validación.</response>
    /// <response code="404">Torneo no encontrado.</response>
    [HttpPut("{id:ulid}")]
    [ProducesResponseType(typeof(TournamentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> UpdateTournament(Ulid id, [FromForm] TournamentPatchDto request) {
        logger.LogInformation("Received request to update tournament: {id} with data: {@Request}", id, request);
        return await tournamentsService.UpdateTournament(id, request).Match(
            success => Ok(success),
            error => error switch {
                TournamentNotFoundError => NotFound(error.Error),
                ValidationError => BadRequest(error.Error),
                _ => StatusCode(500, "An unexpected error occurred.")
            });
     }

    /// <summary>
    /// Asigna un trabajador a una máquina dentro del torneo.
    /// </summary>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="worker">DTO con UserId (string) y MachineName.</param>
    /// <returns>DTO detallado del torneo actualizado.</returns>
    /// <response code="200">Trabajador asignado correctamente.</response>
    /// <response code="400">UserId inválido (no es un ULID válido).</response>
    /// <response code="404">Torneo o usuario no encontrado.</response>
    [HttpPatch("add-worker/{id:ulid}")]
    [ProducesResponseType(typeof(TournamentResponseDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> PatchTournament(
        Ulid id,
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

    /// <summary>
    /// Desasigna un trabajador del torneo y elimina sus asignaciones de máquina.
    /// </summary>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="worker">ULID del trabajador en formato string.</param>
    /// <returns>DTO detallado del torneo actualizado.</returns>
    /// <response code="200">Trabajador desasignado correctamente.</response>
    /// <response code="400">UserId inválido (no es un ULID válido).</response>
    /// <response code="404">Torneo o usuario no encontrado.</response>
    [HttpPatch("remove-worker/{id:ulid}")]
    [ProducesResponseType(typeof(TournamentResponseDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> RemoveWorkerFromTournament(
        Ulid id,
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
    
    /// <summary>
    /// Obtiene todas las asignaciones trabajador-máquina de un torneo.
    /// </summary>
    /// <param name="id">ULID del torneo.</param>
    /// <returns>Lista de asignaciones con datos del usuario.</returns>
    /// <response code="200">Lista de asignaciones.</response>
    /// <response code="404">Torneo no encontrado.</response>
    [HttpGet("{id:ulid}/workers")]
    [ProducesResponseType(typeof(List<WorkerMachineAssignmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> GetAssignedWorkers(Ulid id) {
        logger.LogInformation("Received request to get assigned workers for tournament: {id}", id);
        return await tournamentsService.GetAssignedWorkerMachines(id).Match(
            success => Ok(success),
            error => error switch {
                    TournamentNotFoundError => NotFound(error.Error),
                    _ => StatusCode(500, error.Error)
            });
     }

    /// <summary>
    /// Crea un torneo desde el propietario autenticado (ownerId extraído del JWT).
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Extrae el <c>NameIdentifier</c> del JWT.</description></item>
    ///   <item><description>Si el claim falta o es inválido → 404 NotFound.</description></item>
    ///   <item><description>Delega la creación a <c>tournamentsService.OwnerCreateTournament</c>.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="adminRequest">DTO multipart con nombre, fechas y logotipo opcional.</param>
    /// <returns>201 Created con el DTO detallado del torneo creado.</returns>
    /// <response code="201">Torneo creado correctamente.</response>
    /// <response code="400">Error de validación.</response>
    /// <response code="404">Owner no encontrado o claim inválido.</response>
    /// <response code="409">Conflicto.</response>
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
            onSuccess: tournament => Created(Url.Action("GetTournament", "Tournaments", new { id = tournament.Id }, Request.Scheme)!, tournament),
            onFailure: error => error switch {
                UserNotFoundError => NotFound(error.Error),
                ConflictError => Conflict(error.Error),
                ValidationError => BadRequest(error.Error),
                _ => StatusCode(500, "An unexpected error occurred.")
            });
    }

    /// <summary>
    /// Asigna un supervisor a un torneo.
    /// </summary>
    /// <param name="request">DTO con TournamentId y SupervisorId.</param>
    /// <returns>DTO detallado del torneo actualizado.</returns>
    /// <response code="200">Supervisor asignado correctamente.</response>
    /// <response code="400">SupervisorId inválido.</response>
    /// <response code="404">Torneo o supervisor no encontrado.</response>
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
    
    /// <summary>
    /// Desasigna un supervisor de un torneo.
    /// </summary>
    /// <param name="request">DTO con TournamentId y SupervisorId.</param>
    /// <returns>DTO detallado del torneo actualizado.</returns>
    /// <response code="200">Supervisor desasignado correctamente.</response>
    /// <response code="400">SupervisorId inválido.</response>
    /// <response code="404">Torneo o supervisor no encontrado.</response>
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