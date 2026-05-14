using BackEncordados.Common.Dto;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Service.CrudService;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackEncordados.Usuarios.Controller;

[ApiController]
[Route("api/[controller]")]
public class UserController(ILogger<UserController> logger, IUserService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PageResponseDto<UserWithIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> GetAll(
        [FromQuery] long? tournamentId,
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

    [HttpGet("users/{tournament:long}")]
    [ProducesResponseType(typeof(PageResponseDto<UserWithIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireEncorderRole")]
    public async Task<IActionResult> GetAllUsers(
        long tournament,
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