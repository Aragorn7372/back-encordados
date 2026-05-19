using BackEncordados.Common.Database.Config;
using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Errors;
using BackEncordados.Purchased.Service;
using BackEncordados.Purchased.Validator;
using BackEncordados.Usuarios.Errors;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Role = BackEncordados.Usuarios.Model.User.UserRoles;

namespace BackEncordados.Purchased.Controller;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PurchasedController(
    ILogger<PurchasedController> logger, 
    IPurchasedService service,
    IValidator<PurchasedRequestDto> validator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PageResponseDto<PurchasedResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<IActionResult> GetAll(
        [FromQuery] Ulid? tournamentId,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string direction = "desc",
        [FromQuery] string search = "")
    {
        logger.LogInformation("Get all purchased with search {Search}, sortBy {SortBy}, page {Page}, size {Size} and direction {Direction}",
            search, sortBy, page, size, direction);
        var filter = new FilterPurchasedDto(
            null,
            null,
            null,
            tournamentId,
            Search: search,
            SortBy: sortBy,
            Page: page,
            Size: size,
            Direction: direction);
        var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (role == Role.ADMIN)
        {
            return Ok(await service.FindAllAsync(filter));
        }

        if (role == Role.ENCORDER || role == Role.USER || role == Role.OWNER || role == Role.SUPERVISOR) {
            if ((role == Role.OWNER || role == Role.SUPERVISOR) && tournamentId is null)
                return Forbid();
            
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim))
            {
                logger.LogWarning("User claim not found for role {Role}", role);
                return Forbid();
            }

            filter.UserId = idClaim;
            if (role == Role.ENCORDER)
            {
                filter.IsEncorder = true;
            }
            else if (role == Role.USER)
            {
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
    public async Task<IActionResult> GetById(string id)
    {
        logger.LogInformation("Get purchased by id {Id}", id);
        if (Ulid.TryParse(id, out var ulid))
        {
            return await service.FindByIdAsync(ulid).Match(
                onSuccess: purchased => Ok(purchased),
                onFailure: error => error switch
                {
                    PurchasedNotFoundError => NotFound(new { error.Error }),
                    UserNotFoundError => NotFound(new { error.Error }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Error })
                });
        }
        var error = new PurchasedNotFoundError();
        return NotFound(new { error.Error });
    }

    [HttpPost]
    [ProducesResponseType(typeof(PurchasedResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Transactional(typeof(PedidosDbContext), typeof(UserDbContext))]
    [Authorize(policy: "RequireEncorderRole")]
    public async Task<IActionResult> Create(PurchasedRequestDto request)
    {
        logger.LogInformation("Create purchased {@Request}", request);
        
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        
        var result = await service.CreatePurchasedAsync(request);
        return result.Match(
            onSuccess: purchased => CreatedAtAction(nameof(GetById), new { id = purchased.Id }, purchased),
            onFailure: error => error switch
            {
                UserNotFoundError => BadRequest(new { error.Error }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Error })
            });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PurchasedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireEncorderRole")]
    public async Task<IActionResult> Update(string id, PurchasedPatchDto request)
    {
        logger.LogInformation("Update purchased with id {Id} {@Request}", id, request);
        if (Ulid.TryParse(id, out var ulid))
        {
            return await service.UpdatePurchasedAsync(ulid, request).Match(
                onSuccess: purchased => Ok(purchased),
                onFailure: error => error switch
                {
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
    [Authorize(policy: "RequireEncorderRole")]
    public async Task<IActionResult> CancelPurchased(string id)
    {
        logger.LogInformation("Cancel purchased with id {Id}", id);
        if (Ulid.TryParse(id, out var ulid))
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var isUser = false;
            var idUser = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (role == Role.USER)
            {
                isUser = true;
            }
            return await service.CancelPurchasedAsync(ulid, isUser, idUser).Match(
                onSuccess: purchased => Ok(purchased),
                onFailure: error => error switch
                {
                    PurchasedNotFoundError => NotFound(new { error.Error }),
                    UserNotFoundError => NotFound(new { error.Error }),
                    UnauthorizedError => StatusCode(StatusCodes.Status403Forbidden, new { error.Error }),
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
    [Authorize(policy: "RequireEncorderRole")]
    public async Task<IActionResult> ChangePaymentStatusPurchased(string id, [FromBody] string paymentStatus)
    {
        logger.LogInformation("Change payment status purchased with id {Id} to payment status {PaymentStatus}", id, paymentStatus);
        if (Ulid.TryParse(id, out var ulid))
        {
            return await service.ChangePaymentStatusPurchasedAsync(ulid, paymentStatus).Match(
                onSuccess: purchased => Ok(purchased),
                onFailure: error => error switch
                {
                    PurchasedNotFoundError => NotFound(new { error.Error }),
                    InvalidStatusError => BadRequest(new { error.Error }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Error })
                });
        }
        var error = new PurchasedNotFoundError();
        return NotFound(new { error.Error });
    }

    [HttpPatch("change-status/{id}")]
    [ProducesResponseType(typeof(PurchasedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Transactional(typeof(PedidosDbContext))]
    [Authorize(policy: "RequireEncorderRole")]
    public async Task<IActionResult> ChangeAllLineasStatus(string id, [FromBody] string status)
    {
        logger.LogInformation("Change all lineas status for purchased with id {Id} to {Status}", id, status);
        if (Ulid.TryParse(id, out var ulid))
        {
            return await service.ChangeAllLineasStatusAsync(ulid, status).Match(
                onSuccess: purchased => Ok(purchased),
                onFailure: error => error switch
                {
                    PurchasedNotFoundError => NotFound(new { error.Error }),
                    InvalidStatusError => BadRequest(new { error.Error }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Error })
                });
        }
        var error = new PurchasedNotFoundError();
        return NotFound(new { error.Error });
    }

    [HttpPut("lineas/{lineaId}")]
    [ProducesResponseType(typeof(PedidoLineaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireEncorderRole")]
    public async Task<IActionResult> UpdateLinea(string lineaId, PedidoLineaPatchDto request)
    {
        logger.LogInformation("Update linea with id {LineaId} {@Request}", lineaId, request);
        if (Ulid.TryParse(lineaId, out var ulidLinea))
        {
            return await service.UpdateLineaAsync(ulidLinea, request).Match(
                onSuccess: linea => Ok(linea),
                onFailure: error => error switch
                {
                    PurchasedNotFoundError => NotFound(new { error.Error }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Error })
                });
        }
        var error = new PurchasedNotFoundError();
        return NotFound(new { error.Error });
    }

    [HttpPatch("lineas/{lineaId}/cancel")]
    [ProducesResponseType(typeof(PedidoLineaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireEncorderRole")]
    public async Task<IActionResult> CancelLinea(string lineaId)
    {
        logger.LogInformation("Cancel linea with id {LineaId}", lineaId);
        var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        var idUser = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (Ulid.TryParse(lineaId, out var ulidLinea))
        {
            return await service.CancelLineaAsync(ulidLinea, idUser, role).Match(
                onSuccess: linea => Ok(linea),
                onFailure: error => error switch
                {
                    PurchasedNotFoundError => NotFound(new { error.Error }),
                    UnauthorizedError => StatusCode(StatusCodes.Status403Forbidden, new { error.Error }),
                    InvalidStatusError => BadRequest(new { error.Error }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Error })
                });
        }
        var error = new PurchasedNotFoundError();
        return NotFound(new { error.Error });
    }

    [HttpPatch("lineas/{lineaId}/status")]
    [ProducesResponseType(typeof(PedidoLineaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireEncorderRole")]
    public async Task<IActionResult> ChangeLineaStatus(string lineaId, [FromBody] string status)
    {
        logger.LogInformation("Change linea status with id {LineaId} to {Status}", lineaId, status);
        if (Ulid.TryParse(lineaId, out var ulidLinea))
        {
            return await service.ChangeLineaStatusAsync(ulidLinea, status).Match(
                onSuccess: linea => Ok(linea),
                onFailure: error => error switch
                {
                    PurchasedNotFoundError => NotFound(new { error.Error }),
                    InvalidStatusError => BadRequest(new { error.Error }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Error })
                });
        }
        var error = new PurchasedNotFoundError();
        return NotFound(new { error.Error });
    }
}