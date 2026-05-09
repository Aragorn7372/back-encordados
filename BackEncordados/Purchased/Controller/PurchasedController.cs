using BackEncordados.Common.Database.Config;
using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Errors;
using BackEncordados.Purchased.Service;
using BackEncordados.Usuarios.Errors;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackEncordados.Purchased.Controller;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PurchasedController(ILogger<PurchasedController> logger, IPurchasedService service) : ControllerBase {
    [HttpGet]
    [ProducesResponseType(typeof(PageResponseDto<PurchasedResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<IActionResult> GetAll(
        [FromQuery] string sortBy = "id",
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string direction = "asc",
        [FromQuery] string search = "") {
        logger.LogInformation("Get all purchased with search {Search}, sortBy {SortBy}, page {Page}, size {Size} and direction {Direction}",
            search, sortBy, page, size, direction);
        var filter = new FilterPurchasedDto(
            null,
            null,
            null,
            Search: search,
            SortBy: sortBy,
            Page: page,
            Size: size,
            Direction: direction);
        var role= User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        
        if (role == Usuarios.Model.User.UserRoles.ADMIN ||
            role == Usuarios.Model.User.UserRoles.OWNER) {
            return Ok(await service.FindAllAsync(filter));
        }
        
        if (role == Usuarios.Model.User.UserRoles.ENCORDER ||
            role == Usuarios.Model.User.UserRoles.USER) {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim)) {
                logger.LogWarning("User claim not found for role {Role}", role);
                return Forbid();
            }
            
            filter.UserId = idClaim;
            if (role == Usuarios.Model.User.UserRoles.ENCORDER) {
                filter.IsEncorder = true;
            } else if (role == Usuarios.Model.User.UserRoles.USER) {
                filter.IsUser = true;
            }
            
            return Ok(await service.FindAllAsync(filter));
        }
        
        logger.LogWarning("Access denied for role {Role}", role);
        return Forbid();
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PurchasedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<IActionResult> GetById(string id) { 
        logger.LogInformation("Get purchased by id {Id}", id);
        if (Ulid.TryParse(id, out var ulid)) {
            return await service.FindByIdAsync(ulid).Match(
                onSuccess: purchased => Ok(purchased),
                onFailure: error => error switch {
                    PurchasedNotFoundError => NotFound(new{error.Error}),
                    UserNotFoundError => NotFound(new{error.Error}),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new{error.Error})

                });
        }
        var error = new PurchasedNotFoundError();
        return NotFound(new{error.Error});
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(PurchasedResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireAdminRole")]
    public async Task<IActionResult> Create(PurchasedRequestDto request) {
        logger.LogInformation("Create purchased {@Request}", request);
        var result = await service.CreatePurchasedAsync(request);
        return result.Match(
            onSuccess: purchased => CreatedAtAction(nameof(GetById), new { id = purchased.Id }, purchased),
            onFailure: error => error switch {
                UserNotFoundError => BadRequest(new{error.Error}),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new{error.Error})
            });
     }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PurchasedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireAdminRole")]
    public async Task<IActionResult> Update(string id, PurchasedPatchDto request) {
        logger.LogInformation("Update purchased with id {Id} {@Request}", id, request);
        if (Ulid.TryParse(id, out var ulid)) {
            return await service.UpdatePurchasedAsync(ulid, request).Match(
                onSuccess: purchased => Ok(purchased),
                onFailure: error => error switch {
                    PurchasedNotFoundError => NotFound(new { error.Error }),
                    UserNotFoundError => NotFound(new { error.Error }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Error })
                });
        }

        var error = new PurchasedNotFoundError();
        return NotFound(new { error.Error });
    }
    
    [HttpPatch("cancel/{id}")]
    [ProducesResponseType(typeof(PurchasedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Transactional(typeof(PedidosDbContext))]
    [Authorize(policy: "RequireUserRole")]
    public async Task<IActionResult> CancelPurchased( string id) {
        logger.LogInformation("Cancel purchased with id {Id}", id);
        if (Ulid.TryParse(id, out var ulid)) {
            var role= User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var isUser = false;
            var idUser= User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (role == Usuarios.Model.User.UserRoles.USER) {
                isUser = true;
            }
            return await service.CancelPurchasedAsync(ulid,isUser,idUser).Match(
                onSuccess: purchased => Ok(purchased),
                onFailure: error => error switch {
                    PurchasedNotFoundError => NotFound(new { error.Error }),
                    UserNotFoundError => NotFound(new { error.Error }),
                    UnauthorizedError => StatusCode(StatusCodes.Status403Forbidden, new { error.Error }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Error })
                });
        }

        var error = new PurchasedNotFoundError();
        return NotFound(new { error.Error });
     }
    
    [HttpPatch("change-status/{id}")]
    [ProducesResponseType(typeof(PurchasedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireEcorderRole")]
    public async Task<IActionResult> ChangeStatusPurchased(string id, [FromQuery] string status) {
        logger.LogInformation("Change status purchased with id {Id} to status {Status}", id, status);
        if (Ulid.TryParse(id, out var ulid)) {
            return await service.ChangeStatusPurchasedAsync(ulid, status).Match(
                onSuccess: purchased => Ok(purchased),
                onFailure: error => error switch {
                    PurchasedNotFoundError => NotFound(new { error.Error }),
                    UserNotFoundError => NotFound(new { error.Error }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Error })
                });
        }

        var error = new PurchasedNotFoundError();
        return NotFound(new { error.Error });
     }
    
    [HttpPatch("change-payment-status/{id}")]
    [ProducesResponseType(typeof(PurchasedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireEcorderRole")]
    public async Task<IActionResult> ChangePaymentStatusPurchased(string id, [FromQuery] string paymentStatus) {
        logger.LogInformation("Change payment status purchased with id {Id} to payment status {PaymentStatus}", id, paymentStatus);
        if (Ulid.TryParse(id, out var ulid)) {
            return await service.ChangePaymentStatusPurchasedAsync(ulid, paymentStatus).Match(
                onSuccess: purchased => Ok(purchased),
                onFailure: error => error switch {
                    UserNotFoundError => NotFound(new { error.Error }),
                    PurchasedNotFoundError => NotFound(new { error.Error }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Error })
                });
        }
        var error = new PurchasedNotFoundError();
        return NotFound(new { error.Error });
    }
}