using BackEncordados.Common.Dto;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Service.CrudService;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackEncordados.Usuarios.Controller;

/// <summary>
/// Controlador de API para la gestión de usuarios del sistema.
/// </summary>
/// <remarks>
/// <para>Proporciona once endpoints para operaciones CRUD de usuarios:</para>
/// <list type="table">
///   <listheader>
///     <term>Endpoint</term>
///     <description>Método</description>
///     <description>Policy requerida</description>
///     <description>Uso</description>
///   </listheader>
///   <item>
///     <term><c>GET api/user</c></term>
///     <description><c>GetAll</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Lista paginada de todos los usuarios con filtros.</description>
///   </item>
///   <item>
///     <term><c>GET api/user/supervisors</c></term>
///     <description><c>GetSupervisors</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Lista paginada de supervisores.</description>
///   </item>
///   <item>
///     <term><c>GET api/user/encorders</c></term>
///     <description><c>GetAllEncorders</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Lista paginada de encordadores.</description>
///   </item>
///   <item>
///     <term><c>GET api/user/users/{tournament}</c></term>
///     <description><c>GetAllUsers</c></description>
///     <description>RequireEncorderRole</description>
///     <description>Lista paginada de usuarios de un torneo.</description>
///   </item>
///   <item>
///     <term><c>GET api/user/{id}</c></term>
///     <description><c>GetById</c></description>
///     <description>RequireAdminRole</description>
///     <description>Obtiene un usuario por ULID.</description>
///   </item>
///   <item>
///     <term><c>GET api/user/me</c></term>
///     <description><c>GetMe</c></description>
///     <description>RequireUserRole</description>
///     <description>Obtiene el perfil del usuario autenticado.</description>
///   </item>
///   <item>
///     <term><c>PATCH api/user/me</c></term>
///     <description><c>PatchMe</c></description>
///     <description>Autenticado</description>
///     <description>Actualiza el perfil del usuario autenticado.</description>
///   </item>
///   <item>
///     <term><c>DELETE api/user/{id}</c></term>
///     <description><c>Delete</c></description>
///     <description>RequireAdminRole</description>
///     <description>Elimina un usuario (soft delete).</description>
///   </item>
///   <item>
///     <term><c>DELETE api/user/me</c></term>
///     <description><c>DeleteMe</c></description>
///     <description>Autenticado</description>
///     <description>Auto-eliminación del usuario autenticado.</description>
///   </item>
///   <item>
///     <term><c>PATCH api/user/{id}</c></term>
///     <description><c>Patch</c></description>
///     <description>RequireAdminRole</description>
///     <description>Actualiza datos de un usuario por ID.</description>
///   </item>
///   <item>
///     <term><c>POST api/user/{id}/roles</c></term>
///     <description><c>GiveRole</c></description>
///     <description>RequireAdminRole</description>
///     <description>Cambia el rol de un usuario.</description>
///   </item>
///   <item>
///     <term><c>POST api/user/create-contact</c></term>
///     <description><c>CreateContact</c></description>
///     <description>RequireEncorderRole</description>
///     <description>Crea un contacto visitante.</description>
///   </item>
///   <item>
///     <term><c>POST api/user/create-encoder</c></term>
///     <description><c>CreateEncoder</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Promociona un usuario a ENCORDER.</description>
///   </item>
///   <item>
///     <term><c>POST api/user/{id}/bonos</c></term>
///     <description><c>AddBonos</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Añade bonos al saldo de un usuario.</description>
///   </item>
/// </list>
/// <para>Los errores de validación se mapean a 400, conflictos a 409, no encontrado a 404 y errores internos a 500.</para>
/// </remarks>
/// <param name="logger">Logger para seguimiento de operaciones de usuario.</param>
/// <param name="service">Servicio de lógica de negocio CRUD de usuarios.</param>
[ApiController]
[Route("api/[controller]")]
public class UserController(ILogger<UserController> logger, IUserService service) : ControllerBase
{
    /// <summary>
    /// Obtiene una lista paginada de todos los usuarios con filtros opcionales.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Construye un <see cref="FilterUserDto"/> a partir de los query parameters.</description></item>
    ///   <item><description>Delega la consulta a <c>service.GetAllUsersAsync</c>.</description></item>
    ///   <item><description>Retorna 200 OK con la página de resultados.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="tournamentId">Filtro opcional por torneo (ULID).</param>
    /// <param name="findUsers">Si es true, incluye solo usuarios con rol USER.</param>
    /// <param name="findEncorders">Si es true, incluye solo encordadores.</param>
    /// <param name="findSupervisors">Si es true, incluye solo supervisores.</param>
    /// <param name="sortBy">Campo de ordenación (name, username, email, createdAt, id).</param>
    /// <param name="page">Número de página (desde 0).</param>
    /// <param name="size">Tamaño de página.</param>
    /// <param name="direction">Dirección: asc o desc.</param>
    /// <param name="search">Término de búsqueda textual.</param>
    /// <returns>Página de DTOs de usuario con ID.</returns>
    /// <response code="200">Lista paginada de usuarios.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PageResponseDto<UserWithIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> GetAll(
        [FromQuery] Ulid? tournamentId,
        [FromQuery] bool? findUsers = null,
        [FromQuery] bool? findEncorders = null,
        [FromQuery] bool? findSupervisors = null,
        [FromQuery] string sortBy = "id",
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string direction = "asc",
        [FromQuery] string search = "")
    {
        var filter = new FilterUserDto(
            FindUsers: findUsers,
            FindEncorders: findEncorders,
            FindSupervisors: findSupervisors,
            TournamentId: tournamentId,
            Search: search,
            SortBy: sortBy,
            Page: page,
            Size: size,
            Direction: direction);

        var result = await service.GetAllUsersAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene una lista paginada de usuarios con rol SUPERVISOR.
    /// </summary>
    /// <remarks>
    /// <para>Atajo que filtra <c>findSupervisors=true</c> en el <see cref="FilterUserDto"/>.</para>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="sortBy">Campo de ordenación.</param>
    /// <param name="page">Número de página.</param>
    /// <param name="size">Tamaño de página.</param>
    /// <param name="direction">Dirección: asc o desc.</param>
    /// <param name="search">Término de búsqueda.</param>
    /// <returns>Página de supervisores.</returns>
    /// <response code="200">Lista paginada de supervisores.</response>
    [HttpGet("supervisors")]
    [ProducesResponseType(typeof(PageResponseDto<UserWithIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> GetSupervisors(
        [FromQuery] string sortBy = "id",
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string direction = "asc",
        [FromQuery] string search = "") {
            var filter = new FilterUserDto(
                FindUsers: null,
                TournamentId: null,
                FindEncorders: null,
                FindSupervisors: true,
                Search: search,
                SortBy: sortBy,
                Page: page,
                Size: size,
                Direction: direction);

            var result = await service.GetAllUsersAsync(filter);
            return Ok(result);
    }

    /// <summary>
    /// Obtiene una lista paginada de usuarios con rol ENCORDER.
    /// </summary>
    /// <remarks>
    /// <para>Atajo que filtra <c>findEncorders=true</c> en el <see cref="FilterUserDto"/>.</para>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="sortBy">Campo de ordenación.</param>
    /// <param name="page">Número de página.</param>
    /// <param name="size">Tamaño de página.</param>
    /// <param name="direction">Dirección: asc o desc.</param>
    /// <param name="search">Término de búsqueda.</param>
    /// <returns>Página de encordadores.</returns>
    [HttpGet("encorders")]
    [ProducesResponseType(typeof(PageResponseDto<UserWithIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> GetAllEncorders(
        [FromQuery] string sortBy = "id",
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string direction = "asc",
        [FromQuery] string search = "")
    {
        var filter = new FilterUserDto(
            FindUsers: null,
            TournamentId: null,
            FindEncorders: true,
            FindSupervisors: null,
            Search: search,
            SortBy: sortBy,
            Page: page,
            Size: size,
            Direction: direction);

        var result = await service.GetAllUsersAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene una lista paginada de usuarios (rol USER) de un torneo específico.
    /// </summary>
    /// <remarks>
    /// <para>Atajo que filtra <c>findUsers=true</c> y <c>TournamentId=tournament</c>.</para>
    /// <para>Requiere policy <c>RequireEncorderRole</c>.</para>
    /// </remarks>
    /// <param name="tournament">ULID del torneo en la ruta.</param>
    /// <param name="sortBy">Campo de ordenación.</param>
    /// <param name="page">Número de página.</param>
    /// <param name="size">Tamaño de página.</param>
    /// <param name="direction">Dirección: asc o desc.</param>
    /// <param name="search">Término de búsqueda.</param>
    /// <returns>Página de usuarios del torneo.</returns>
    [HttpGet("users/{tournament:ulid}")]
    [ProducesResponseType(typeof(PageResponseDto<UserWithIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireEncorderRole")]
    public async Task<IActionResult> GetAllUsers(
        Ulid tournament,
        [FromQuery] string sortBy = "id",
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string direction = "asc",
        [FromQuery] string search = "")
    {
        var filter = new FilterUserDto(
            FindUsers: true,
            FindEncorders: null,
            FindSupervisors: null,
            TournamentId: tournament,
            Search: search,
            SortBy: sortBy,
            Page: page,
            Size: size,
            Direction: direction);

        var result = await service.GetAllUsersAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene un usuario por su identificador ULID.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>service.FindByIdAsync</c> con el ULID proporcionado.</description></item>
    ///   <item><description>Si existe, retorna 200 OK con el DTO del usuario.</description></item>
    ///   <item><description>Si no existe, retorna 404 NotFound.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireAdminRole</c>.</para>
    /// </remarks>
    /// <param name="id">ULID del usuario en la ruta.</param>
    /// <returns>DTO público del usuario.</returns>
    /// <response code="200">Usuario encontrado.</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpGet("{id:ulid}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> GetById(Ulid id)
    {
        var result = await service.FindByIdAsync(id);
        return result.Match(
            onSuccess: user => Ok(user),
            onFailure: error => error switch
            {
                UserNotFoundError err => NotFound(new { message = err.Error }),
                _ => StatusCode(500, new { message = error.Error })
            });
    }

    /// <summary>
    /// Obtiene el perfil del usuario autenticado.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Extrae el <c>NameIdentifier</c> del JWT.</description></item>
    ///   <item><description>Si falta o no es un ULID válido → 404 NotFound.</description></item>
    ///   <item><description>Llama a <c>service.FindByIdAsync</c> con el ULID extraído.</description></item>
    ///   <item><description>Retorna 200 OK con el perfil del usuario.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireUserRole</c>.</para>
    /// </remarks>
    /// <returns>DTO público del usuario autenticado.</returns>
    /// <response code="200">Perfil del usuario autenticado.</response>
    /// <response code="404">Claim de ID no encontrado en el token, o usuario no existe.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> GetMe()
    {
        logger.LogInformation("GetMe called - User authenticated: {IsAuthenticated}", User.Identity?.IsAuthenticated);
        
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
        {
            return NotFound(new { message = "User ID claim not found or invalid" });
        }
        
        if (!Ulid.TryParse(userIdClaim.Value, out var userId))
        {
            logger.LogWarning("Failed to parse user ID claim value: {Value}", userIdClaim.Value);
            return NotFound(new { message = "User ID claim not found or invalid" });
        }

        var result = await service.FindByIdAsync(userId);
        return result.Match(
            onSuccess: user => Ok(user),
            onFailure: error => error switch
            {
                UserNotFoundError err => NotFound(new { message = err.Error }),
                _ => StatusCode(500, new { message = error.Error })
            });
    }

    /// <summary>
    /// Actualiza el perfil del usuario autenticado.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Extrae el <c>NameIdentifier</c> del JWT y lo parsea como ULID.</description></item>
    ///   <item><description>Si el claim falta o es inválido → 404 NotFound.</description></item>
    ///   <item><description>Llama a <c>service.PatchUserAsync</c> con el ULID y el DTO.</description></item>
    ///   <item><description>Mapea el error a HTTP según el tipo: 400 (Validation), 404 (NotFound), 409 (Conflict).</description></item>
    /// </list>
    /// <para>Requiere autenticación (cualquier rol).</para>
    /// </remarks>
    /// <param name="request">DTO multipart con campos opcionales (Name, Email, Telefono, Username, Avatar).</param>
    /// <returns>DTO actualizado del usuario.</returns>
    /// <response code="200">Perfil actualizado correctamente.</response>
    /// <response code="400">Error de validación.</response>
    /// <response code="404">Usuario no encontrado o claim inválido.</response>
    /// <response code="409">Conflicto (ej: username ya en uso).</response>
    [HttpPatch("me")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<IActionResult> PatchMe([FromForm] UserRequestDto request)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Ulid.TryParse(userIdClaim.Value, out var userId))
            return NotFound(new { message = "User ID claim not found or invalid" });

        var result = await service.PatchUserAsync(userId, request);
        return result.Match(
            onSuccess: user => Ok(user),
            onFailure: error => error switch
            {
                ValidationError validationError => BadRequest(new { message = validationError.Error }),
                UserNotFoundError userNotFoundError => NotFound(new { message = userNotFoundError.Error }),
                ConflictError conflictError => Conflict(new { message = conflictError.Error }),
                _ => StatusCode(500, new { message = error.Error })
            });
    }
    
    /// <summary>
    /// Elimina un usuario (soft delete) por ULID.
    /// </summary>
    /// <remarks>
    /// <para>Requiere policy <c>RequireAdminRole</c>.</para>
    /// <para>Marca el usuario como eliminado y reemplaza su username con un UUID para liberar el nombre único.</para>
    /// </remarks>
    /// <param name="id">ULID del usuario a eliminar.</param>
    /// <response code="204">Usuario eliminado correctamente.</response>
    /// <response code="500">Error interno durante la eliminación.</response>
    [HttpDelete("{id:ulid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> Delete(Ulid id)
    {
        try
        {
            await service.DeleteUserAsync(id);
            return NoContent();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error deleting user with ID {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the user" });
        }
    }

    /// <summary>
    /// Auto-eliminación (soft delete) del usuario autenticado.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Extrae el <c>NameIdentifier</c> del JWT.</description></item>
    ///   <item><description>Si el claim falta o es inválido → 404 NotFound.</description></item>
    ///   <item><description>Llama a <c>service.DeleteUserAsync</c>.</description></item>
    /// </list>
    /// <para>Requiere autenticación (cualquier rol).</para>
    /// </remarks>
    /// <response code="204">Usuario eliminado correctamente.</response>
    /// <response code="404">Claim de ID no encontrado en el token.</response>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<IActionResult> DeleteMe()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Ulid.TryParse(userIdClaim.Value, out var userId))
            return NotFound(new { message = "User ID claim not found or invalid" });

        try
        {
            await service.DeleteUserAsync(userId);
            return NoContent();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error deleting user with ID {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while deleting the user" });
        }
    }

    /// <summary>
    /// Actualiza parcialmente los datos de un usuario por ULID (solo administradores).
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>service.PatchUserAsync</c> con el ULID y el DTO.</description></item>
    ///   <item><description>Mapea el error a HTTP según el tipo.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireAdminRole</c>.</para>
    /// </remarks>
    /// <param name="id">ULID del usuario a actualizar.</param>
    /// <param name="request">DTO multipart con campos opcionales.</param>
    /// <returns>DTO actualizado del usuario.</returns>
    /// <response code="200">Usuario actualizado correctamente.</response>
    /// <response code="400">Error de validación.</response>
    /// <response code="404">Usuario no encontrado.</response>
    /// <response code="409">Conflicto.</response>
    [HttpPatch("{id:ulid}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> Patch(Ulid id, [FromForm] UserRequestDto request)
    {
        var result = await service.PatchUserAsync(id, request);
        return result.Match(
            onSuccess: user => Ok(user),
            onFailure: error => error switch
            {
                ValidationError validationError => BadRequest(new { message = validationError.Error }),
                UserNotFoundError userNotFoundError => NotFound(new { message = userNotFoundError.Error }),
                ConflictError conflictError => Conflict(new { message = conflictError.Error }),
                _ => StatusCode(500, new { message = error.Error })
            });
    }

    /// <summary>
    /// Cambia el rol de un usuario.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>service.GiveRoleToUserAsync</c> con el ULID y el nombre del rol.</description></item>
    ///   <item><description>Si es exitoso, retorna 204 NoContent.</description></item>
    ///   <item><description>Si falla, mapea el error a HTTP según el tipo.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireAdminRole</c>.</para>
    /// </remarks>
    /// <param name="id">ULID del usuario.</param>
    /// <param name="role">Nombre del rol: ADMIN, USER, OWNER o ENCORDER.</param>
    /// <response code="204">Rol cambiado correctamente.</response>
    /// <response code="400">Rol inválido.</response>
    /// <response code="404">Usuario no encontrado.</response>
    /// <response code="409">El usuario ya tiene ese rol.</response>
    [HttpPost("{id:ulid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> GiveRole(Ulid id, [FromBody] string role)
    {
        var result = await service.GiveRoleToUserAsync(id, role);
        if (result.IsSuccess)
            return NoContent();

        var error = result.Error;
        return error switch
        {
            ValidationError validationError => BadRequest(new { message = validationError.Error }),
            UserNotFoundError userNotFoundError => NotFound(new { message = userNotFoundError.Error }),
            ConflictError conflictError => Conflict(new { message = conflictError.Error }),
            _ => StatusCode(500, new { message = error.Error })
        };
    }

    /// <summary>
    /// Crea un nuevo contacto visitante en el sistema asociado a un torneo.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>service.CreateContacto</c> con el DTO de solicitud.</description></item>
    ///   <item><description>Mapea el error a HTTP según el tipo.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireEncorderRole</c>.</para>
    /// </remarks>
    /// <param name="request">DTO con nombre, email/teléfono opcional y TournamentId.</param>
    /// <returns>DTO del contacto creado.</returns>
    /// <response code="200">Contacto creado correctamente.</response>
    /// <response code="400">Error de validación.</response>
    /// <response code="404">Error al crear contacto.</response>
    /// <response code="409">Conflicto al crear contacto.</response>
    [HttpPost("create-contact")]
    [ProducesResponseType(typeof(UserResponseDto),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireEncorderRole")]
    public async Task<IActionResult> CreateContact([FromBody] ContactoPostRequestDto request)
    {
        return await service.CreateContacto(request).Match(
            onSuccess: user => Ok(user),
            onFailure: error => error switch
            {
                ValidationError validationError => BadRequest(new { message = validationError.Error }),
                UserNotFoundError userNotFoundError => NotFound(new { message = userNotFoundError.Error }),
                ConflictError conflictError => Conflict(new { message = conflictError.Error }),
                _ => StatusCode(500, new { message = error.Error })
            });
    }
    
    /// <summary>
    /// Promociona un usuario existente al rol de Encordador (ENCORDER).
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>service.CreateEncoderAsync</c> con el ULID del usuario.</description></item>
    ///   <item><description>Si es exitoso, retorna 204 NoContent.</description></item>
    ///   <item><description>Si falla, mapea el error a HTTP según el tipo.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="userId">ULID del usuario a promocionar.</param>
    /// <response code="204">Usuario promocionado a ENCORDER correctamente.</response>
    /// <response code="400">Error de validación.</response>
    /// <response code="404">Usuario no encontrado.</response>
    /// <response code="409">El usuario ya es ENCORDER.</response>
    [HttpPost("create-encoder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> CreateEncoder([FromBody] Ulid userId) {
        
        logger.LogInformation("Creating encoder for user with ID {UserId}", userId);
        var result = await service.CreateEncoderAsync(userId);
        if (result.IsSuccess)
            return NoContent();

        var error = result.Error;
        return error switch
        {
            ValidationError validationError => BadRequest(new { message = validationError.Error }),
            UserNotFoundError userNotFoundError => NotFound(new { message = userNotFoundError.Error }),
            ConflictError conflictError => Conflict(new { message = conflictError.Error }),
            _ => StatusCode(500, new { message = error.Error })
        };
    }

    /// <summary>
    /// Añade bonos al saldo de un usuario.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>service.AddBonosAsync</c> con el ULID y la cantidad.</description></item>
    ///   <item><description>Si es exitoso, retorna 200 OK con el DTO actualizado.</description></item>
    ///   <item><description>Si falla, mapea el error a HTTP según el tipo.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="id">ULID del usuario.</param>
    /// <param name="cantidad">Cantidad positiva de bonos a añadir.</param>
    /// <returns>DTO del usuario con el nuevo saldo de bonos.</returns>
    /// <response code="200">Bonos añadidos correctamente.</response>
    /// <response code="400">La cantidad debe ser mayor a 0.</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpPost("{id:ulid}/bonos")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> AddBonos(Ulid id, [FromBody] double cantidad)
    {
        logger.LogInformation("Adding {Cantidad} bonos to user with ID {UserId}", cantidad, id);
        var result = await service.AddBonosAsync(id, cantidad);
        return result.Match(
            onSuccess: user => Ok(user),
            onFailure: error => error switch
            {
                ValidationError validationError => BadRequest(new { message = validationError.Error }),
                UserNotFoundError userNotFoundError => NotFound(new { message = userNotFoundError.Error }),
                _ => StatusCode(500, new { message = error.Error })
            });
    }
}