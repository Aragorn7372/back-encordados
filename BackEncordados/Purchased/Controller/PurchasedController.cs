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

/// <summary>
/// REST API controller for managing purchase orders (pedidos) and their line items (PedidoLinea).
/// </summary>
/// <remarks>
/// <para>
/// All endpoints require authentication via the <c>[Authorize]</c> attribute. Role-based access
/// is enforced for write operations using named policies:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Policy</term>
///     <description>Endpoints</description>
///   </listheader>
///   <item>
///     <term><c>RequireEncorderRole</c></term>
///     <description>Create, Update, Cancel, ChangeStatus, Linea operations</description>
///   </item>
///   <item>
///     <term><c>[Authorize]</c> (any authenticated)</term>
///     <description>GetAll, GetById</description>
///   </item>
/// </list>
/// <para>
/// The controller handles role-aware data filtering:
/// </para>
/// <list type="bullet">
///   <item><description><b>ADMIN</b> — sees all orders across all tournaments.</description></item>
///   <item><description><b>OWNER / SUPERVISOR</b> — must specify a <c>tournamentId</c> query parameter (otherwise 403 Forbidden).</description></item>
///   <item><description><b>ENCORDER</b> — sees only orders assigned to themselves (<c>IsEncorder = true</c>).</description></item>
///   <item><description><b>USER</b> — sees only their own orders (<c>IsUser = true</c> with ownership check on cancel).</description></item>
/// </list>
/// <para>
/// Some write endpoints are decorated with <c>[Transactional(typeof(PedidosDbContext), typeof(UserDbContext))]</c>
/// to ensure distributed consistency when the order and user bonus updates are persisted together.
/// </para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PurchasedController(
    ILogger<PurchasedController> logger, 
    IPurchasedService service,
    IValidator<PurchasedRequestDto> validator) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated, sorted, and role-filtered list of purchase orders.
    /// </summary>
    /// <remarks>
    /// <para>Role-based filtering behavior:</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Role</term>
    ///     <description>Filter applied</description>
    ///   </listheader>
    ///   <item><term>ADMIN</term><description>No filter — sees all orders.</description></item>
    ///   <item><term>OWNER / SUPERVISOR</term><description>Must provide <c>tournamentId</c>; sees all orders in that tournament.</description></item>
    ///   <item><term>ENCORDER</term><description>Filtered by <c>AssignedTo == current user</c>.</description></item>
    ///   <item><term>USER</term><description>Filtered by <c>PlayerId == current user</c>.</description></item>
    /// </list>
    /// <para>Example request:</para>
    /// <code>
    /// GET /api/Purchased?tournamentId=01HXYZ...&amp;sortBy=createdAt&amp;direction=desc&amp;page=0&amp;size=10&amp;search=Wilson
    /// </code>
    /// </remarks>
    /// <param name="tournamentId">Optional tournament ULID to filter by.</param>
    /// <param name="sortBy">Sort field: "createdAt", "machine", "playerId", "encorder", or default by "id".</param>
    /// <param name="page">Zero-based page number (default 0).</param>
    /// <param name="size">Page size (default 10).</param>
    /// <param name="direction">Sort direction: "asc" or "desc" (default "desc").</param>
    /// <param name="search">Free-text search across Comments, Machine, and RaquetModel.</param>
    /// <response code="200">Returns the paginated list of orders with pagination metadata.</response>
    /// <response code="403">If OWNER or SUPERVISOR did not provide a tournamentId.</response>
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

    /// <summary>
    /// Retrieves a single purchase order by its ULID, including all line items.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses a cache-first strategy — on hit returns from L1/L2 cache; on miss loads from DB,
    /// assembles the full response with resolved user data, caches for 5 minutes, and returns.
    /// </para>
    /// </remarks>
    /// <param name="id">The order ULID as a string (will be parsed to <see cref="Ulid"/>).</param>
    /// <response code="200">Returns the order DTO with resolved player/encorder data.</response>
    /// <response code="404">If the order ID is not valid or not found.</response>
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

    /// <summary>
    /// Creates a new purchase order with optional auto-payment from player bonuses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This endpoint is decorated with <c>[Transactional]</c> spanning both <c>PedidosDbContext</c> and
    /// <c>UserDbContext</c> to ensure that the order creation and bonus deduction happen atomically.
    /// </para>
    /// <para>Validation:</para>
    /// <list type="bullet">
    ///   <item><description>Tournament must exist and not be soft-deleted.</description></item>
    ///   <item><description>Player username must reference a valid USER role account.</description></item>
    ///   <item><description>Encorder username must reference a valid ENCORDER or OWNER role account.</description></item>
    ///   <item><description>PayStatus must be a valid <see cref="Model.PaymentStatus"/> enum value.</description></item>
    ///   <item><description>At least one line item must be provided, each validated by <see cref="PedidoLineaRequestValidator"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="request">The order creation payload (player/encorder usernames, line items, price, payment status).</param>
    /// <response code="201">Returns the created order with its new ID in the Location header.</response>
    /// <response code="400">If validation fails or the player/encorder is not found.</response>
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

    /// <summary>
    /// Partially updates an existing order's Machine, Comments, and PayStatus fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the three fields listed above are mutable via this endpoint. Line items are not affected.
    /// Null fields on the patch DTO are skipped (merge-patch pattern).
    /// </para>
    /// </remarks>
    /// <param name="id">The order ULID to update.</param>
    /// <param name="request">The patch payload with optional fields (Machine, Comments, PayStatus).</param>
    /// <response code="200">Returns the updated order with resolved user data.</response>
    /// <response code="404">If the order ID is not valid or the order was not found.</response>
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

    /// <summary>
    /// Cancels an order (sets PayStatus to CANCELED and all line statuses to CANCELED).
    /// </summary>
    /// <remarks>
    /// <para>
    /// For <c>USER</c> role callers, the endpoint verifies that the caller owns the order
    /// (<c>PlayerId</c> matches the caller's identity claim). If not, returns 403 Forbidden.
    /// Sends cancellation email, WhatsApp, and SignalR notifications asynchronously.
    /// </para>
    /// </remarks>
    /// <param name="id">The order ULID to cancel.</param>
    /// <response code="200">Returns the canceled order DTO.</response>
    /// <response code="403">If a USER tries to cancel an order they do not own.</response>
    /// <response code="404">If the order ID is not valid or the order was not found.</response>
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

    /// <summary>
    /// Changes only the payment status of an existing order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the new status is <c>PAID</c>, the service enqueues a payment confirmation email
    /// to the player's email address.
    /// </para>
    /// </remarks>
    /// <param name="id">The order ULID.</param>
    /// <param name="paymentStatus">The new payment status string (e.g. "PAID", "PENDING", "CANCELED").</param>
    /// <response code="200">Returns the updated order DTO.</response>
    /// <response code="400">If the payment status string is not a valid <see cref="Model.PaymentStatus"/> value.</response>
    /// <response code="404">If the order was not found.</response>
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
                    UserNotFoundError => NotFound(new { error.Error }),
                    InvalidStatusError => BadRequest(new { error.Error }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Error })
                });
        }
        var error = new PurchasedNotFoundError();
        return NotFound(new { error.Error });
    }

    /// <summary>
    /// Changes the status of every non-conflicting line item in an order at once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Conflict prevention rules:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Target <c>CANCELED</c> — skips lines already <c>COMPLETED</c> or <c>DELIVERED_TOpLAYER</c>.</description></item>
    ///   <item><description>Any other target — skips lines already <c>CANCELED</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">The order ULID.</param>
    /// <param name="status">The target status string for all eligible line items.</param>
    /// <response code="200">Returns the updated order DTO with all lines reflecting the new status where applicable.</response>
    /// <response code="400">If the status string is invalid.</response>
    /// <response code="404">If the order was not found.</response>
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

    /// <summary>
    /// Partially updates a single line item (PedidoLinea) within an order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the fields provided in the patch DTO are modified (merge-patch pattern).
    /// Updates RaquetModel, Nudos, DateString, Logotype, Color, Status, and/or StringSetup.
    /// Invalidates the parent order's cache entry so the next read reflects the change.
    /// </para>
    /// </remarks>
    /// <param name="lineaId">The line ULID to update.</param>
    /// <param name="request">The patch payload with optional fields.</param>
    /// <response code="200">Returns the updated line item DTO.</response>
    /// <response code="404">If the line ID is not valid or not found.</response>
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

    /// <summary>
    /// Cancels a single line item by setting its status to <see cref="Model.Status.CANCELED"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For <c>USER</c> role callers, cancellation is blocked if the line is already
    /// <c>COMPLETED</c> or <c>DELIVERED_TOpLAYER</c> (returns 400 Bad Request).
    /// Otherwise, the cancellation proceeds and sends WhatsApp + SignalR notifications.
    /// </para>
    /// </remarks>
    /// <param name="lineaId">The line ULID to cancel.</param>
    /// <response code="200">Returns the canceled line item DTO.</response>
    /// <response code="400">If the line is in a terminal state (COMPLETED/DELIVERED) and the caller is a USER.</response>
    /// <response code="403">If the caller is not authorized to cancel this line.</response>
    /// <response code="404">If the line was not found.</response>
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

    /// <summary>
    /// Changes the status of a single line item and sends appropriate notifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Depending on the new status, the following notifications are triggered:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>COMPLETED</c> — sends completion email + WhatsApp + SignalR.</description></item>
    ///   <item><description><c>DELIVERED_TOpLAYER</c> — sends delivery email + WhatsApp + SignalR.</description></item>
    ///   <item><description><c>CANCELED</c> — sends WhatsApp cancellation + SignalR.</description></item>
    ///   <item><description>Other statuses — SignalR only.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="lineaId">The line ULID.</param>
    /// <param name="status">The new status string (e.g. "PENDING", "IN_PROGRESS", "COMPLETED", "DELIVERED_TOpLAYER", "CANCELED").</param>
    /// <response code="200">Returns the updated line item DTO.</response>
    /// <response code="400">If the status string is invalid.</response>
    /// <response code="404">If the line was not found.</response>
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
