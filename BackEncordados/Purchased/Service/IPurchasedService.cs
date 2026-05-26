using BackEncordados.Common.Dto;
using BackEncordados.Common.Errors;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Errors;
using BackEncordados.Purchased.Model;
using CSharpFunctionalExtensions;

namespace BackEncordados.Purchased.Service;

/// <summary>
/// Defines the application-layer service contracts for the Purchased (pedidos) bounded context.
/// </summary>
/// <remarks>
/// <para>
/// <c>IPurchasedService</c> orchestrates the full lifecycle of purchase orders and their line items.
/// It co operates across multiple infrastructure concerns:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Concern</term>
///     <description>Service(s)</description>
///   </listheader>
///   <item>
///     <term>Persistence</term>
///     <description><see cref="Repository.IPuchasedRepository"/>, <see cref="Usuarios.Repository.IUserRepository"/></description>
///   </item>
///   <item>
///     <term>Caching</term>
///     <description><see cref="Common.Service.Cache.ICacheService"/> (L1 + L2 hybrid, user data and purchased DTOs)</description>
///   </item>
///   <item>
///     <term>Image resolution</term>
///     <description><see cref="Common.Service.Cloudinary.ICloudinaryService"/> for user avatar URLs</description>
///   </item>
///   <item>
///     <term>Notifications</term>
///     <description><see cref="Common.Service.Email.IEmailService"/>, <see cref="Common.Service.WhatsApp.IWhatsAppService"/>, SignalR hub</description>
///   </item>
/// </list>
/// <para>
/// All write operations return <see cref="Result{T,E}"/> with <see cref="DomainErrors"/> on failure.
/// Read-only queries return DTOs directly (non-Result) — they throw on infrastructure errors.
/// Error types used include:
/// <see cref="PurchasedNotFoundError"/>, <see cref="Usuarios.Errors.UserNotFoundError"/>,
/// <see cref="InvalidStatusError"/>, <see cref="Usuarios.Errors.UnauthorizedError"/>,
/// <see cref="Usuarios.Errors.ConcurrencyError"/>.
/// </para>
/// </remarks>
public interface IPurchasedService
{
    /// <summary>
    /// Returns a paginated list of orders according to the supplied filter, with player and encorder user data resolved.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Orchestration flow:
    /// </para>
    /// <list type="number">
    ///   <item><description>Call repository <c>FindAllAsync</c> to get the page of order headers and total count.</description></item>
    ///   <item><description>Collect all unique <c>PlayerId</c> and <c>AssignedTo</c> values from the result set.</description></item>
    ///   <item><description>Attempt to resolve each user ID from the L1/L2 cache (key: <c>CacheKeys.UserDataKey + id</c>).</description></item>
    ///   <item><description>For cache misses, batch-fetch from <c>IUserRepository.FindByIdsAsync</c>, cache each result (10-minute TTL).</description></item>
    ///   <item><description>Map each order through <see cref="Mapper.PurchasedMapper.ToDto(Pedidos, Usuarios.Dto.UserResponseDto, Usuarios.Dto.UserResponseDto)"/>.</description></item>
    ///   <item><description>Skip orders whose player or encorder could not be resolved (logged as warning).</description></item>
    ///   <item><description>Return a <see cref="PageResponseDto{PurchasedResponseDto}"/> with pagination metadata.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="filter">Pagination, sort, and search parameters from the query string.</param>
    /// <returns>A <see cref="PageResponseDto{PurchasedResponseDto}"/> with resolved user DTOs and pagination metadata.</returns>
    Task<PageResponseDto<PurchasedResponseDto>> FindAllAsync(FilterPurchasedDto filter);

    /// <summary>
    /// Retrieves a single order by <see cref="Ulid"/> with a cache-first strategy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On cache miss, the order is loaded from the repository, its player and encorder are resolved
    /// via <see cref="GetUserDtoCachedAsync"/> (private helper), and the assembled response is cached
    /// for 5 minutes before returning.
    /// </para>
    /// </remarks>
    /// <param name="id">The order <see cref="Ulid"/>.</param>
    /// <returns>The order DTO on success, or a <see cref="PurchasedNotFoundError"/> / <see cref="Usuarios.Errors.UserNotFoundError"/> on failure.</returns>
    Task<Result<PurchasedResponseDto, DomainErrors>> FindByIdAsync(Ulid id);

    /// <summary>
    /// Creates a new purchase order with automatic bonus payment logic.
    /// </summary>
    /// <remarks>
    /// <para>Creation flow:</para>
    /// <list type="number">
    ///   <item><description>Lookup player and encorder by username (cache-first, then DB).</description></item>
    ///   <item><description>Map the request DTO to a <see cref="Pedidos"/> entity via <see cref="Mapper.PurchasedMapper.ToEntity(PurchasedRequestDto, Ulid, Ulid)"/>.</description></item>
    ///   <item><description>If <c>player.Bonos >= Price</c>: set <c>PayStatus = PAID</c>, deduct bonus, retry DB update up to 3 times (<see cref="PurchasedService.UpdateUserWithRetryAsync"/>).</description></item>
    ///   <item><description>If <c>0 &lt; player.Bonos &lt; Price</c>: append a shortfall comment ("Falta por pagar: ...").</description></item>
    ///   <item><description>Persist the aggregate via the repository.</description></item>
    ///   <item><description>Cache the resulting DTO (5-minute TTL).</description></item>
    ///   <item><description>Fire-and-forget: send SignalR notification (<c>PEDIDO_CREADO</c> to tournament group + admin group).</description></item>
    ///   <item><description>If auto-paid: enqueue a payment confirmation email to the player.</description></item>
    /// </list>
    /// <para>Requires <c>[Transactional(typeof(PedidosDbContext), typeof(UserDbContext))]</c> at the controller level for distributed consistency.</para>
    /// </remarks>
    /// <param name="request">The order creation payload (tournament, player/encorder usernames, line items, price, payment status).</param>
    /// <returns>The created order DTO or a <see cref="Usuarios.Errors.UserNotFoundError"/> if the player or encorder could not be found.</returns>
    Task<Result<PurchasedResponseDto, DomainErrors>> CreatePurchasedAsync(PurchasedRequestDto request);

    /// <summary>
    /// Partially updates an existing order's Machine, Comments, and PayStatus fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Loads the existing order, applies the patch, persists, invalidates the old cache entry,
    /// and re-caches the updated DTO (5-minute TTL). Sends a <c>PEDIDO_ACTUALIZADO</c> SignalR notification.
    /// </para>
    /// </remarks>
    /// <param name="id">The order <see cref="Ulid"/>.</param>
    /// <param name="request">The patch payload with optional (nullable) fields.</param>
    /// <returns>The updated order DTO or a <see cref="PurchasedNotFoundError"/>.</returns>
    Task<Result<PurchasedResponseDto, DomainErrors>> UpdatePurchasedAsync(Ulid id, PurchasedPatchDto request);

    /// <summary>
    /// Cancels an order (and all its line items) with role-based authorization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Flow:
    /// </para>
    /// <list type="number">
    ///   <item><description>Call <c>repository.CancelPurchasedAsync</c> — sets order to CANCELED and all lines to CANCELED.</description></item>
    ///   <item><description>If the caller has <c>USER</c> role (<paramref name="isUser"/> = true), verify <c>PlayerId == idUser</c>; return <see cref="Usuarios.Errors.UnauthorizedError"/> on mismatch.</description></item>
    ///   <item><description>Re-cache the updated DTO (5-minute TTL).</description></item>
    ///   <item><description>Fire-and-forget: send cancellation email, WhatsApp notification, and SignalR broadcast (<c>PEDIDO_CANCELADO</c>).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">The order <see cref="Ulid"/>.</param>
    /// <param name="isUser"><c>true</c> if the caller has the <c>USER</c> role, triggering ownership verification.</param>
    /// <param name="idUser">The caller's <see cref="Ulid"/> string, used for the ownership check when <paramref name="isUser"/> is <c>true</c>.</param>
    /// <returns>The canceled order DTO or a domain error.</returns>
    Task<Result<PurchasedResponseDto, DomainErrors>> CancelPurchasedAsync(Ulid id, bool isUser, string? idUser);

    /// <summary>
    /// Changes the payment status of an existing order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Validates the status string against <see cref="PaymentStatus"/> enum values.
    /// If the new status is <c>PAID</c>, enqueues a payment confirmation email to the player.
    /// </para>
    /// </remarks>
    /// <param name="id">The order <see cref="Ulid"/>.</param>
    /// <param name="payStatus">The new payment status string (e.g. "PAID", "PENDING", "CANCELED"). Case-insensitive.</param>
    /// <returns>The updated order DTO or a <see cref="PurchasedNotFoundError"/> / <see cref="InvalidStatusError"/>.</returns>
    Task<Result<PurchasedResponseDto, DomainErrors>> ChangePaymentStatusPurchasedAsync(Ulid id, string payStatus);

    /// <summary>
    /// Applies a partial patch to a single <see cref="PedidoLinea"/> identified by its Ulid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Loads the existing line, applies the merge-patch, persists, and invalidates the parent order cache entry
    /// so the next read reflects the updated line data. Sends a <c>LINEA_PEDIDO_ACTUALIZADA</c> SignalR notification.
    /// </para>
    /// </remarks>
    /// <param name="lineaId">The line <see cref="Ulid"/>.</param>
    /// <param name="request">The patch payload with optional fields.</param>
    /// <returns>The updated line DTO or a <see cref="PurchasedNotFoundError"/>.</returns>
    Task<Result<PedidoLineaResponseDto, DomainErrors>> UpdateLineaAsync(Ulid lineaId, PedidoLineaPatchDto request);

    /// <summary>
    /// Cancels a single line item with role-aware validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For <c>USER</c> callers, cancellation is blocked if the line status is already
    /// <see cref="Status.COMPLETED"/> or <see cref="Status.DELIVERED_TOpLAYER"/>.
    /// Sends a WhatsApp cancellation notification and a <c>ESTATUS_LINEA_PEDIDO_ACTUALIZADA</c> SignalR broadcast.
    /// </para>
    /// </remarks>
    /// <param name="lineaId">The line <see cref="Ulid"/>.</param>
    /// <param name="userId">The caller's user ID string (used for role-based checking only — not ownership).</param>
    /// <param name="userRole">The caller's role string (e.g. "USER", "ENCORDER", "ADMIN").</param>
    /// <returns>The canceled line DTO or a domain error (<see cref="InvalidStatusError"/> if line is in a terminal state).</returns>
    Task<Result<PedidoLineaResponseDto, DomainErrors>> CancelLineaAsync(Ulid lineaId, string? userId, string? userRole);

    /// <summary>
    /// Changes the status of a single <see cref="PedidoLinea"/> and sends appropriate notifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Flow:
    /// </para>
    /// <list type="number">
    ///   <item><description>Parse and validate the status string against <see cref="Status"/> enum.</description></item>
    ///   <item><description>Load the existing line from the repository.</description></item>
    ///   <item><description>Persist the status change.</description></item>
    ///   <item><description>Invalidate the parent order cache.</description></item>
    ///   <item><description>If new status is <c>COMPLETED</c>: send completion email + WhatsApp to the player.</description></item>
    ///   <item><description>If new status is <c>DELIVERED_TOpLAYER</c>: send delivery email + WhatsApp to the player.</description></item>
    ///   <item><description>If new status is <c>CANCELED</c>: send WhatsApp cancellation to the player.</description></item>
    ///   <item><description>Send <c>ESTATUS_LINEA_PEDIDO_ACTUALIZADA</c> SignalR notification.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="lineaId">The line <see cref="Ulid"/>.</param>
    /// <param name="status">The target status string (e.g. "PENDING", "IN_PROGRESS", "COMPLETED", "DELIVERED_TOpLAYER", "CANCELED"). Case-insensitive.</param>
    /// <returns>The updated line DTO or a domain error.</returns>
    Task<Result<PedidoLineaResponseDto, DomainErrors>> ChangeLineaStatusAsync(Ulid lineaId, string status);

    /// <summary>
    /// Changes the status of every non-conflicting line item in an order to the given value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Conflict rules:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>When target is <c>CANCELED</c>: lines already <c>COMPLETED</c> or <c>DELIVERED_TOpLAYER</c> are skipped (cannot cancel completed work).</description></item>
    ///   <item><description>When target is any non-canceled status: lines already <c>CANCELED</c> are skipped (cannot revive a canceled line).</description></item>
    /// </list>
    /// <para>After updating all eligible lines, the parent order cache entry is invalidated.</para>
    /// </remarks>
    /// <param name="purchasedId">The order <see cref="Ulid"/>.</param>
    /// <param name="status">The target status string for all eligible lines.</param>
    /// <returns>The updated order DTO or a <see cref="PurchasedNotFoundError"/> / <see cref="InvalidStatusError"/>.</returns>
    Task<Result<PurchasedResponseDto, DomainErrors>> ChangeAllLineasStatusAsync(Ulid purchasedId, string status);
}
