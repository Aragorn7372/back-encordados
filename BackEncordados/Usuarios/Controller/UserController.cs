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
[Produces("application/json")]
public class UserController(ILogger<UserController> logger, IUserService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PageResponseDto<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? findUsers = null,
        [FromQuery] bool? findEncorders = null,
        [FromQuery] string sortBy = "id",
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string direction = "asc",
        [FromQuery] string search = "")
    {
        var filter = new FilterUserDto(
            FindUsers: findUsers,
            FindEncorders: findEncorders,
            Search: search,
            SortBy: sortBy,
            Page: page,
            Size: size,
            Direction: direction);

        var result = await service.GetAllUsersAsync(filter);
        return Ok(result);
    }

    [HttpGet("encorders")]
    [ProducesResponseType(typeof(PageResponseDto<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> GetAllEncorders(
        [FromQuery] string sortBy = "id",
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string direction = "asc",
        [FromQuery] string search = "")
    {
        var filter = new FilterUserDto(
            FindUsers: null,
            FindEncorders: true,
            Search: search,
            SortBy: sortBy,
            Page: page,
            Size: size,
            Direction: direction);

        var result = await service.GetAllUsersAsync(filter);
        return Ok(result);
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(PageResponseDto<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireEncorderRole")]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] string sortBy = "id",
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string direction = "asc",
        [FromQuery] string search = "")
    {
        var filter = new FilterUserDto(
            FindUsers: true,
            FindEncorders: null,
            Search: search,
            SortBy: sortBy,
            Page: page,
            Size: size,
            Direction: direction);

        var result = await service.GetAllUsersAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> GetById(Guid id)
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
        logger.LogInformation("Total claims received: {ClaimCount}", User.Claims.Count());
        foreach (var claim in User.Claims)
        {
            logger.LogInformation("Claim Type: {ClaimType}, Value: {ClaimValue}", claim.Type, claim.Value);
        }
        
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
        {
            logger.LogWarning("NameIdentifier claim not found. Looking for alternative claim types...");
            logger.LogWarning("Available claim types: {ClaimTypes}", string.Join(", ", User.Claims.Select(c => c.Type)));
            return NotFound(new { message = "User ID claim not found or invalid" });
        }
        
        if (!Guid.TryParse(userIdClaim.Value, out var userId))
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

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> Delete(Guid id)
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
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> DeleteMe()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
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

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] UserRequestDto request)
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

    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> GiveRole(Guid id, [FromQuery] string role)
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
}