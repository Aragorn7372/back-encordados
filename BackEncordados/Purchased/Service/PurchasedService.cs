using BackEncordados.Common.Dto;
using BackEncordados.Common.Errors;
using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.keys;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Common.Service.Email;
using BackEncordados.Common.Service.WhatsApp;
using BackEncordados.Common.SignalR;
using BackEncordados.Common.Utils;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Errors;
using BackEncordados.Purchased.Mapper;
using BackEncordados.Purchased.Model;
using BackEncordados.Purchased.Repository;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Mapper;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Purchased.Service;

/// <summary>
/// Application service for the Purchased (pedidos) bounded context.
/// Orchestrates the full lifecycle of purchase orders and their line items.
/// </summary>
/// <remarks>
/// <para>
/// <c>PurchasedService</c> co operates across multiple infrastructure concerns:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Dependency</term>
///     <description>Purpose</description>
///   </listheader>
///   <item>
///     <term><see cref="Repository.IPuchasedRepository"/></term>
///     <description>Order and line item persistence.</description>
///   </item>
///   <item>
///     <term><see cref="Usuarios.Repository.IUserRepository"/></term>
///     <description>Player and encorder lookup, bonus deduction with concurrency retries.</description>
///   </item>
///   <item>
///     <term><see cref="Common.Service.Cache.ICacheService"/></term>
///     <description>L1/L2 hybrid caching for user data (10-min TTL) and purchased DTOs (5-min TTL).</description>
///   </item>
///   <item>
///     <term><see cref="Common.Service.Cloudinary.ICloudinaryService"/></term>
///     <description>User avatar URL resolution in response DTOs.</description>
///   </item>
///   <item>
///     <term><see cref="Common.Service.Email.IEmailService"/></term>
///     <description>Payment confirmation, cancellation, line completion/delivery emails via MailKit channel queue.</description>
///   </item>
///   <item>
///     <term><see cref="Common.Service.WhatsApp.IWhatsAppService"/></term>
///     <description>WhatsApp Graph API notifications for cancellations and status changes.</description>
///   </item>
///   <item>
///     <term><c>IHubContext&lt;SignalHub&gt;</c></term>
///     <description>Real-time SignalR notifications to tournament-specific and admin groups.</description>
///   </item>
/// </list>
/// <para>
/// Business logic highlights:
/// </para>
/// <list type="bullet">
///   <item><description><b>Auto-payment</b> — if <c>player.Bonos >= Price</c>, the order is marked PAID and bonus is deducted with up to 3 retry attempts for concurrency conflicts.</description></item>
///   <item><description><b>Partial bonus</b> — if <c>0 &lt; player.Bonos &lt; Price</c>, a shortfall comment is appended to the order.</description></item>
///   <item><description><b>Cache invalidation</b> — any mutation on an order or line item invalidates the cached <c>PurchasedResponseDto</c> so the next read is fresh.</description></item>
///   <item><description><b>Fire-and-forget notifications</b> — emails, WhatsApp, and SignalR broadcasts are dispatched on background tasks via <c>TapAsync</c> after the DB transaction succeeds.</description></item>
/// </list>
/// </remarks>
public class PurchasedService(
    IPuchasedRepository repository,
    IUserRepository userRepository, 
    ILogger<PurchasedService> logger, 
    ICacheService cache,
    ICloudinaryService cloudinary,
    IEmailService emailService,
    IWhatsAppService whatsAppService,
    IHubContext<SignalHub> signal
    ) : IPurchasedService
{
    /// <summary>
    /// Returns a paginated list of orders with player and encorder user data resolved from cache or DB.
    /// </summary>
    /// <remarks>
    /// <para>Orchestration flow:</para>
    /// <list type="number">
    ///   <item><description>Call <see cref="Repository.IPuchasedRepository.FindAllAsync"/> to get the page of order headers and total count.</description></item>
    ///   <item><description>Collect all unique <c>PlayerId</c> and <c>AssignedTo</c> values from the result set.</description></item>
    ///   <item><description>Attempt to resolve each user ID from the L1/L2 cache using key <c>CacheKeys.UserDataKey + id</c> (set by <see cref="GetUserDtoCachedAsync"/>).</description></item>
    ///   <item><description>For cache misses, batch-fetch from <see cref="Usuarios.Repository.IUserRepository.FindByIdsAsync"/>, map to <see cref="UserResponseDto"/> via <c>UserMapper.ToDto(cloudinary)</c>, and cache each for 10 minutes.</description></item>
    ///   <item><description>Map each order through <see cref="Mapper.PurchasedMapper.ToDto(Pedidos, UserResponseDto, UserResponseDto)"/>.</description></item>
    ///   <item><description>Skip orders whose player or encorder could not be resolved (logged as warning).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="filter">Pagination, sort, and filter parameters.</param>
    /// <returns>A <see cref="PageResponseDto{PurchasedResponseDto}"/> with resolved user DTOs.</returns>
    public async Task<PageResponseDto<PurchasedResponseDto>> FindAllAsync(FilterPurchasedDto filter)
    {
        logger.LogInformation("Obteniendo todos los pedidos con filtro: Página {Page}, Tamaño {Size}", filter.Page, filter.Size);
        var (paginatedItems, totalCount) = await repository.FindAllAsync(filter);
        int totalPages = filter.Size > 0 ? (int)Math.Ceiling(totalCount / (double)filter.Size) : 0;

        var allUserIds = paginatedItems
            .SelectMany(p => new[] { p.PlayerId, p.AssignedTo })
            .Distinct()
            .ToList();

        var userDict = new Dictionary<Ulid, UserResponseDto>();
        var missingIds = new List<Ulid>();

        foreach (var id in allUserIds)
        {
            var cached = await cache.GetAsync<UserResponseDto>(CacheKeys.UserDataKey + id);
            if (cached is not null)
                userDict[id] = cached;
            else
                missingIds.Add(id);
        }

        if (missingIds.Count != 0)
        {
            var dbUsers = await userRepository.FindByIdsAsync(missingIds);
            foreach (var dbUser in dbUsers)
            {
                var dto = dbUser.ToDto(cloudinary);
                await cache.SetAsync(CacheKeys.UserDataKey + dbUser.Id, dto, TimeSpan.FromMinutes(10));
                userDict[dbUser.Id] = dto;
            }
        }

        var items = new List<PurchasedResponseDto>();
        foreach (var item in paginatedItems)
        {
            if (!userDict.TryGetValue(item.PlayerId, out var playerDto) ||
                !userDict.TryGetValue(item.AssignedTo, out var encorderDto))
            {
                logger.LogWarning("Jugador o encordador no encontrado para el pedido {PurchasedId}", item.Id);
                continue;
            }
            items.Add(item.ToDto(playerDto, encorderDto));
        }

        logger.LogInformation("Se obtuvieron {ItemCount} pedidos de un total de {TotalCount}", items.Count, totalCount);
        return new PageResponseDto<PurchasedResponseDto>(
            Content: items,
            TotalPages: totalPages,
            TotalElements: totalCount,
            PageSize: filter.Size,
            PageNumber: filter.Page,
            TotalPageElements: items.Count,
            SortBy: filter.SortBy,
            Direction: filter.Direction
        );
    }

    /// <summary>
    /// Retrieves a single order by its <see cref="Ulid"/> with cache-first strategy.
    /// </summary>
    /// <remarks>
    /// <para>Processing flow:</para>
    /// <list type="number">
    ///   <item><description>Attempt cache lookup with key <c>CacheKeys.PurchasedCacheKey + id</c> (5-minute TTL).</description></item>
    ///   <item><description>On hit: return the cached DTO immediately.</description></item>
    ///   <item><description>On miss: load from repository, including all line items.</description></item>
    ///   <item><description>Resolve player and encorder via <see cref="GetUserDtoCachedAsync"/> (cache-first user lookup).</description></item>
    ///   <item><description>Assemble the full response DTO through <see cref="Mapper.PurchasedMapper.ToDto(Pedidos, UserResponseDto, UserResponseDto)"/>.</description></item>
    ///   <item><description>Cache the assembled DTO for 5 minutes.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">The order <see cref="Ulid"/>.</param>
    /// <returns>The order DTO on success, or a <see cref="PurchasedNotFoundError"/> / <see cref="Usuarios.Errors.UserNotFoundError"/> on failure.</returns>
    public async Task<Result<PurchasedResponseDto, DomainErrors>> FindByIdAsync(Ulid id)
    {
        logger.LogInformation("Buscando pedido con ID: {Id}", id);
        var purchasedCached = await cache.GetAsync<PurchasedResponseDto>(CacheKeys.PurchasedCacheKey + id);
        if (purchasedCached != null) return Result.Success<PurchasedResponseDto, DomainErrors>(purchasedCached)
            .Tap(() => logger.LogInformation("Pedido con ID {Id} obtenido de caché", id));

        var purchased = await repository.FindByIdAsync(id);
        if (purchased is null) return Result.Failure<PurchasedResponseDto, DomainErrors>(new PurchasedNotFoundError())
            .TapError(() => logger.LogWarning("Pedido con ID {Id} no encontrado en DB", id));

        var playerResult = await GetUserDtoCachedAsync(purchased.PlayerId);
        if (playerResult.IsFailure) return playerResult.Error;
        var encorderResult = await GetUserDtoCachedAsync(purchased.AssignedTo);
        if (encorderResult.IsFailure) return encorderResult.Error;

        var response = purchased.ToDto(playerResult.Value, encorderResult.Value);

        await cache.SetAsync(CacheKeys.PurchasedCacheKey + id, response, TimeSpan.FromMinutes(5));

        return response;
    }

    /// <summary>
    /// Resolves a user ID to a <see cref="UserResponseDto"/> using a cache-first strategy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cache key pattern: <c>CacheKeys.UserDataKey + userId</c> with 10-minute TTL.
    /// On cache miss, loads from <see cref="Usuarios.Repository.IUserRepository.FindByIdAsync"/>,
    /// verifies the user is not deleted, maps via <c>UserMapper.ToDto(cloudinary)</c>,
    /// caches, and returns. Returns <see cref="Usuarios.Errors.UserNotFoundError"/> if the user
    /// does not exist or has been soft-deleted.
    /// </para>
    /// </remarks>
    /// <param name="userId">The user <see cref="Ulid"/> to resolve.</param>
    /// <returns>The resolved <see cref="UserResponseDto"/> on success, or a <see cref="Usuarios.Errors.UserNotFoundError"/>.</returns>
    private async Task<Result<UserResponseDto, DomainErrors>> GetUserDtoCachedAsync(Ulid userId)
    {
        string key = CacheKeys.UserDataKey + userId;
        var cached = await cache.GetAsync<UserResponseDto>(key);
        if (cached != null) return Result.Success<UserResponseDto, DomainErrors>(cached);

        var user = await userRepository.FindByIdAsync(userId);
        if (user == null || user.IsDeleted) return Result.Failure<UserResponseDto, DomainErrors>(new UserNotFoundError("User no exists or was deleted"))
            .TapError(() => logger.LogWarning("Usuario con ID {Id} no encontrado o eliminado en DB", userId));

        var dto = user.ToDto(cloudinary);
        await cache.SetAsync(key, dto, TimeSpan.FromMinutes(10));
        return Result.Success<UserResponseDto, DomainErrors>(dto)
            .Tap(() => logger.LogInformation("Usuario con ID {Id} obtenido de DB y guardado en caché", userId));
    }

    /// <summary>
    /// Creates a new purchase order with automatic bonus payment logic and notifications.
    /// </summary>
    /// <remarks>
    /// <para>Detailed processing flow:</para>
    /// <list type="number">
    ///   <item><description><b>User resolution</b> — Lookup player and encorder by username (cache-first, then DB via <c>FindByUsernameAsync</c>). Cache resolved users using key <c>CacheKeys.UserKey + username</c>.</description></item>
    ///   <item><description><b>Validation</b> — Both users must exist; otherwise returns <see cref="Usuarios.Errors.UserNotFoundError"/>.</description></item>
    ///   <item><description><b>Entity mapping</b> — Map DTO to <see cref="Pedidos"/> aggregate via <see cref="Mapper.PurchasedMapper.ToEntity(PurchasedRequestDto, Ulid, Ulid)"/>.</description></item>
    ///   <item><description><b>Auto-payment</b> — If <c>player.Bonos >= Price</c>: set <c>PayStatus = PAID</c>, deduct bonus, and persist user change with <see cref="UpdateUserWithRetryAsync"/> (up to 3 retries for <c>DbUpdateConcurrencyException</c>).</description></item>
    ///   <item><description><b>Partial bonus</b> — If <c>0 &lt; player.Bonos &lt; Price</c>: append shortfall text to Comments ("Falta por pagar: ...").</description></item>
    ///   <item><description><b>Persistence</b> — Save the order aggregate (header + line items) via repository.</description></item>
    ///   <item><description><b>Caching</b> — Cache the resulting DTO (5-minute TTL).</description></item>
    ///   <item><description><b>Notifications</b> — Fire-and-forget: SignalR <c>PEDIDO_CREADO</c> event to tournament group + admin group. If auto-paid, enqueue payment confirmation email.</description></item>
    /// </list>
    /// <para>
    /// Requires <c>[Transactional(typeof(PedidosDbContext), typeof(UserDbContext))]</c> on the controller
    /// for distributed atomicity across two DbContexts.
    /// </para>
    /// </remarks>
    /// <param name="request">The order creation payload.</param>
    /// <returns>The created order DTO or a <see cref="Usuarios.Errors.UserNotFoundError"/>.</returns>
    public async Task<Result<PurchasedResponseDto, DomainErrors>> CreatePurchasedAsync(PurchasedRequestDto request)
    {
        logger.LogInformation("Creando pedido para jugador {PlayerName} asignado a {AssignedToName}", request.PlayerName, request.AssignedToName);
        var player = await cache.GetAsync<User>(CacheKeys.UserKey + request.PlayerName);
        var encorder = await cache.GetAsync<User>(CacheKeys.UserKey + request.AssignedToName);

        if (player == null) player = await userRepository.FindByUsernameAsync(request.PlayerName!);
        if (encorder == null) encorder = await userRepository.FindByUsernameAsync(request.AssignedToName!);

        if (player == null || encorder == null)
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new UserNotFoundError("Player or Encorder not found"))
                .TapError(() => logger.LogWarning("Jugador o encordador no encontrado. Player: {PlayerName}, Encorder: {AssignedToName}", request.PlayerName, request.AssignedToName));

        var entity = request.ToEntity(player.Id, encorder.Id);

        if (player.Bonos >= request.Price)
        {
            entity.PayStatus = PaymentStatus.PAID;
            player.Bonos -= request.Price;
            
            // Usar reintentos para actualizar el usuario
            var updateResult = await UpdateUserWithRetryAsync(player);
            if (updateResult.IsFailure)
                return Result.Failure<PurchasedResponseDto, DomainErrors>(updateResult.Error);
            
            logger.LogInformation("Pedido pagado con bonos. Nuevo saldo de bonos: {Bonos}", player.Bonos);
        }
        else if (player.Bonos > 0)
        {
            var falta = request.Price - player.Bonos;
            entity.Comments = string.IsNullOrEmpty(entity.Comments) 
                ? $"Falta por pagar: {falta:C2}" 
                : $"{entity.Comments} | Falta por pagar: {falta:C2}";
            logger.LogInformation("Bonos insuficientes. Falta: {Falta}", falta);
        }

        await repository.CreatePurchasedAsync(entity);

        var response = entity.ToDto(player.ToDto(cloudinary), encorder.ToDto(cloudinary));

        await cache.SetAsync(CacheKeys.PurchasedCacheKey + entity.Id, response, TimeSpan.FromMinutes(5));

        return await Result.Success<PurchasedResponseDto, DomainErrors>(response)
            .TapAsync(async _=> {
                logger.LogInformation("Pedido creado con ID {Id} y guardado en caché", entity.Id);
                SendCreatePurchased(response.Id,response.TournamentId,response);
                if (entity.PayStatus == PaymentStatus.PAID)
                {
                    var email = await GetValidUserWithEmailAsync(player.Username);
                    if (email.IsSuccess)
                    {
                        await SendPaidEmailAsync(entity.Id.ToString(), request.Price, email.Value);
                
                    }
                }
            });
    }

    /// <summary>
    /// Partially updates an existing order's Machine, Comments, and PayStatus fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Loads the existing order, applies the patch via repository (merge-patch), invalidates
    /// the old cache entry, re-caches the updated DTO (5-minute TTL), and sends a
    /// <c>PEDIDO_ACTUALIZADO</c> SignalR notification to the tournament and admin groups.
    /// </para>
    /// </remarks>
    /// <param name="id">The order <see cref="Ulid"/>.</param>
    /// <param name="request">The patch payload with optional fields.</param>
    /// <returns>The updated order DTO or a <see cref="PurchasedNotFoundError"/>.</returns>
    public async Task<Result<PurchasedResponseDto, DomainErrors>> UpdatePurchasedAsync(Ulid id, PurchasedPatchDto request)
    {
        logger.LogInformation("Actualizando pedido con ID {Id}", id);

        var existingPurchased = await repository.FindByIdAsync(id);
        if (existingPurchased is null)
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new PurchasedNotFoundError())
                .TapError(() => logger.LogWarning("Pedido con ID {Id} no encontrado para actualizar", id));

        var patchEntity = new Pedidos
        {
            Machine = request.Machine ?? existingPurchased.Machine,
            Comments = request.Comments ?? existingPurchased.Comments,
            PayStatus = !string.IsNullOrEmpty(request.PayStatus)
                ? Enum.Parse<PaymentStatus>(request.PayStatus, true)
                : existingPurchased.PayStatus
        };

        var updated = await repository.UpdatePurchasedAsync(patchEntity, id);
        if (updated is null)
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new PurchasedNotFoundError())
                .TapError(() => logger.LogWarning("No se pudo actualizar el pedido con ID {Id}", id));

        var playerResult = await GetUserDtoCachedAsync(updated.PlayerId);
        if (playerResult.IsFailure)
            return playerResult.Error;

        var encorderResult = await GetUserDtoCachedAsync(updated.AssignedTo);
        if (encorderResult.IsFailure)
            return encorderResult.Error;

        var response = updated.ToDto(playerResult.Value, encorderResult.Value);

        await cache.SetAsync(CacheKeys.PurchasedCacheKey + id, response, TimeSpan.FromMinutes(5));

        return Result.Success<PurchasedResponseDto, DomainErrors>(response).Tap(() => {
            logger.LogInformation("Pedido con ID {Id} actualizado exitosamente", id);
            SendUpdatedPurchased(response.Id, response.TournamentId, response);
        });
    }

    /// <summary>
    /// Cancels an order (sets PayStatus to CANCELED, all line statuses to CANCELED) with role-based ownership verification.
    /// </summary>
    /// <remarks>
    /// <para>Flow:</para>
    /// <list type="number">
    ///   <item><description>Call <see cref="Repository.IPuchasedRepository.CancelPurchasedAsync"/> — idempotent, no-op if already canceled.</description></item>
    ///   <item><description>If <paramref name="isUser"/> is <c>true</c> and <c>purchasedCanceled.PlayerId != parsed(idUser)</c>: return <see cref="Usuarios.Errors.UnauthorizedError"/> (403).</description></item>
    ///   <item><description>Resolve player and encorder user data via <see cref="GetUserDtoCachedAsync"/>.</description></item>
    ///   <item><description>Re-cache the updated DTO (5-minute TTL).</description></item>
    ///   <item><description>Fire-and-forget: send cancellation email via <see cref="SendCancelEmailAsync"/>, WhatsApp cancellation via <see cref="Common.Service.WhatsApp.IWhatsAppService.SendPedidoCanceledMessageAsync"/>, and SignalR <c>PEDIDO_CANCELADO</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <inheritdoc />
    public async Task<Result<PurchasedResponseDto, DomainErrors>> CancelPurchasedAsync(Ulid id, bool isUser, string? idUser)
    {
        logger.LogInformation("Cancelando pedido con ID {Id}", id);
        var purchasedCanceled = await repository.CancelPurchasedAsync(id);
        if (purchasedCanceled is null) return Result.Failure<PurchasedResponseDto, DomainErrors>(new PurchasedNotFoundError())
            .TapError(() => logger.LogWarning("Pedido con ID {Id} no encontrado para cancelar", id));

        if (isUser && Ulid.TryParse(idUser, out Ulid ulid) && purchasedCanceled.PlayerId != ulid)
        {
            logger.LogWarning("Usuario con ID {UserId} no autorizado para cancelar el pedido con ID {Id}", idUser, id);
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new UnauthorizedError("User not authorized to cancel this purchase"));
        }

        var playerResult = await GetUserDtoCachedAsync(purchasedCanceled.PlayerId);
        if (playerResult.IsFailure) return playerResult.Error;

        var encorderResult = await GetUserDtoCachedAsync(purchasedCanceled.AssignedTo);
        if (encorderResult.IsFailure) return encorderResult.Error;

        var response = purchasedCanceled.ToDto(playerResult.Value, encorderResult.Value);
        await cache.SetAsync(CacheKeys.PurchasedCacheKey + id, response, TimeSpan.FromMinutes(5));
        logger.LogInformation("Pedido con ID {Id} cancelado exitosamente", id);
        return await Result.Success<PurchasedResponseDto, DomainErrors>(response)
            .TapAsync(async response => {
                logger.LogInformation(
                    "Correo de cancelación enviado al jugador con ID {PlayerId} para el pedido con ID {Id}",
                    response.Id, id);
                var email= await GetValidUserWithEmailAsync(response.Player.Username);
                if (email.IsSuccess) await SendCancelEmailAsync(id.ToString(), email.Value);
                
                var player = await userRepository.FindByIdAsync(purchasedCanceled.PlayerId);
                if (player != null && !string.IsNullOrWhiteSpace(player.Phone))
                {
                    logger.LogInformation("WhatsApp de pedido cancelado enviado al jugador con ID {PlayerId}", player.Id);
                    await whatsAppService.SendPedidoCanceledMessageAsync(
                        player.Phone,
                        player.Name,
                        id.ToString(),
                        response.Lineas.Count);
                }
                
                SendCancelPurchased(response.Id, response.TournamentId, response);
            });
    }
    
    /// <summary>
    /// Enqueues a cancellation email via the email service.
    /// </summary>
    /// <param name="orderId">The order ULID string for the email template.</param>
    /// <param name="email">The recipient email address.</param>
    private async Task SendCancelEmailAsync(string orderId, string email) {
        var message = new EmailMessage {
            To = email,
            Subject = "Pedido cancelado",
            Body = EmailTemplates.OrderCancelled(orderId),
            IsHtml = true
        };
        await emailService.EnqueueEmailAsync(message);
    }

    /// <summary>
    /// Retrieves a valid email address for a given username, with validation checks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Validates that:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>The user exists in the database.</description></item>
    ///   <item><description>The user has not been soft-deleted (<c>IsDeleted == false</c>).</description></item>
    ///   <item><description>The user's email is not null or whitespace and contains an '@' character (basic format check).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="username">The username to look up.</param>
    /// <returns>The validated email string, or a <see cref="Usuarios.Errors.UserNotFoundError"/> / <see cref="Usuarios.Errors.ValidationError"/>.</returns>
    private async Task<Result<string ,DomainErrors>> GetValidUserWithEmailAsync(string username)
    {
        var user = await userRepository.FindByUsernameAsync(username);
    
        if (user == null)
            return Result.Failure< string , DomainErrors>(
                new UserNotFoundError($"Usuario '{username}' no encontrado"));
    
        if (user.IsDeleted)
            return Result.Failure<string , DomainErrors>(
                new UserNotFoundError($"Usuario '{username}' ha sido eliminado"));
    
        // Validar que sea un usuario real (con email válido de usuario, no contacto)
        if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains("@"))
            return Result.Failure<string , DomainErrors>(
                new Usuarios.Errors.ValidationError($"El usuario '{username}' no tiene un email válido"));
    
        return Result.Success<string , DomainErrors>((user.Email));
    }

    /// <summary>
    /// Changes the payment status of an existing order and optionally sends a confirmation email.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Validates the status string against <see cref="PaymentStatus"/> enum values.
    /// If the new status is <c>PAID</c>, enqueues a payment confirmation email to the player
    /// (via <see cref="SendPaidEmailAsync"/>).
    /// </para>
    /// </remarks>
    /// <inheritdoc />
    public async Task<Result<PurchasedResponseDto, DomainErrors>> ChangePaymentStatusPurchasedAsync(Ulid id, string payStatus)
    {
        logger.LogInformation("Cambiando el estatus de pago al pedido con ID {Id}", id);
        if (!Enum.TryParse<PaymentStatus>(payStatus, true, out var payStatusEnum))
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new InvalidStatusError("Invalid payment status value"));

        var purchased = await repository.ChangeStatusPurchasedAsync(id, payStatus);
        if (purchased is null) return Result.Failure<PurchasedResponseDto, DomainErrors>(new PurchasedNotFoundError())
            .TapError(() => logger.LogWarning("Pedido con ID {Id} no encontrado para cambiar estatus de pago", id));

        var playerResult = await GetUserDtoCachedAsync(purchased.PlayerId);
        if (playerResult.IsFailure) return playerResult.Error;

        var encorderResult = await GetUserDtoCachedAsync(purchased.AssignedTo);
        if (encorderResult.IsFailure) return encorderResult.Error;

        var response = purchased.ToDto(playerResult.Value, encorderResult.Value);
        await cache.SetAsync(CacheKeys.PurchasedCacheKey + id, response, TimeSpan.FromMinutes(5));
        logger.LogInformation("Estatus de pago del pedido con ID {Id} cambiado exitosamente a {PayStatusEnum}", id, payStatusEnum);
        return await Result.Success<PurchasedResponseDto, DomainErrors>(response).TapAsync(async response => {
            if (payStatusEnum == PaymentStatus.PAID)
            {
                logger.LogInformation(
                    "Correo de confirmación de pago enviado al jugador con ID {PlayerId} para el pedido con ID {Id}",
                    response.Id, id);
                var email = await GetValidUserWithEmailAsync(response.Player.Username);
                if (email.IsSuccess) await SendPaidEmailAsync(id.ToString(), response.Price, email.Value);
            }
        });
    }

    /// <summary>
    /// Enqueues a payment confirmation email via the email service.
    /// </summary>
    /// <param name="orderId">The order ULID string for the template.</param>
    /// <param name="price">The order total price.</param>
    /// <param name="email">The recipient email address.</param>
    private Task SendPaidEmailAsync(string orderId,double price, string email) {
        var message = new EmailMessage {
            To = email,
            Subject = "Pago confirmado",
            Body = EmailTemplates.PaymentConfirmed(orderId,price),
            IsHtml = true
        };
        return emailService.EnqueueEmailAsync(message);
    }
    
    

    /// <summary>
    /// Applies a partial patch to a single line item and invalidates the parent order's cache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Loads the existing line, applies the merge-patch via <see cref="Mapper.PurchasedMapper.ToEntity(PedidoLineaPatchDto, PedidoLinea)"/>,
    /// persists via repository, invalidates the parent order cache key, and sends a <c>LINEA_PEDIDO_ACTUALIZADA</c>
    /// SignalR notification.
    /// </para>
    /// </remarks>
    /// <inheritdoc />
    public async Task<Result<PedidoLineaResponseDto, DomainErrors>> UpdateLineaAsync(Ulid lineaId, PedidoLineaPatchDto request)
    {
        logger.LogInformation("Actualizando línea con ID {LineaId}", lineaId);
        var existingLinea = await repository.FindLineaByIdAsync(lineaId);
        if (existingLinea is null)
            return Result.Failure<PedidoLineaResponseDto, DomainErrors>(new PurchasedNotFoundError())
                .TapError(() => logger.LogWarning("Línea con ID {LineaId} no encontrada", lineaId));

        var updatedLinea = request.ToEntity(existingLinea);
        var savedLinea = await repository.UpdateLineaAsync(updatedLinea, lineaId);
        if (savedLinea is null)
            return Result.Failure<PedidoLineaResponseDto, DomainErrors>(new PurchasedNotFoundError())
                .TapError(() => logger.LogWarning("No se pudo actualizar la línea con ID {LineaId}", lineaId));

        await cache.RemoveAsync(CacheKeys.PurchasedCacheKey + existingLinea.PedidoId);

        return Result.Success<PedidoLineaResponseDto, DomainErrors>(savedLinea.ToDto())
            .Tap(() => {
                logger.LogInformation("Línea con ID {LineaId} actualizada exitosamente", lineaId);
                SendUpdatePurchasedLine(savedLinea.Id, savedLinea.PedidoId, savedLinea.Pedido.TournamentId, savedLinea.ToDto());
            });
    }

    /// <summary>
    /// Cancels a single line item with role-based protection and WhatsApp notification.
    /// </summary>
    /// <remarks>
    /// <para>Flow:</para>
    /// <list type="number">
    ///   <item><description>Load the existing line from repository.</description></item>
    ///   <item><description>If the caller has <c>USER</c> role and the line is <c>COMPLETED</c> or <c>DELIVERED_TOpLAYER</c>, block with <see cref="InvalidStatusError"/>.</description></item>
    ///   <item><description>Persist status change to <c>CANCELED</c>.</description></item>
    ///   <item><description>Invalidate the parent order cache.</description></item>
    ///   <item><description>Fire-and-forget: send WhatsApp cancellation to the player and <c>ESTATUS_LINEA_PEDIDO_ACTUALIZADA</c> SignalR notification.</description></item>
    /// </list>
    /// </remarks>
    /// <inheritdoc />
    public async Task<Result<PedidoLineaResponseDto, DomainErrors>> CancelLineaAsync(Ulid lineaId, string? userId, string? userRole)
    {
        logger.LogInformation("Cancelando línea con ID {LineaId}", lineaId);
        var existingLinea = await repository.FindLineaByIdAsync(lineaId);
        if (existingLinea is null)
            return Result.Failure<PedidoLineaResponseDto, DomainErrors>(new PurchasedNotFoundError())
                .TapError(() => logger.LogWarning("Línea con ID {LineaId} no encontrada", lineaId));

        if (userRole == Usuarios.Model.User.UserRoles.USER)
        {
            if (existingLinea.Status == Status.COMPLETED || existingLinea.Status == Status.DELIVERED_TOpLAYER)
            {
                logger.LogWarning("Usuario {UserId} intentó cancelar línea en estado {Status}", userId, existingLinea.Status);
                return Result.Failure<PedidoLineaResponseDto, DomainErrors>(new InvalidStatusError("No puedes cancelar una línea que ya ha sido completada"));
            }
        }

        var canceledLinea = await repository.ChangeLineaStatusAsync(lineaId, Status.CANCELED);
        if (canceledLinea is null)
            return Result.Failure<PedidoLineaResponseDto, DomainErrors>(new PurchasedNotFoundError())
                .TapError(() => logger.LogWarning("No se pudo cancelar la línea con ID {LineaId}", lineaId));

        await cache.RemoveAsync(CacheKeys.PurchasedCacheKey + canceledLinea.PedidoId);

        return await Result.Success<PedidoLineaResponseDto, DomainErrors>(canceledLinea.ToDto())
            .TapAsync(async _ => {
                logger.LogInformation("Línea con ID {LineaId} cancelada exitosamente", lineaId);
                SendChangeStatusPurchasedLine(canceledLinea.Id, canceledLinea.PedidoId, canceledLinea.Pedido.TournamentId, canceledLinea.ToDto());
                
                var pedido = await repository.FindByIdAsync(canceledLinea.PedidoId);
                if (pedido != null)
                {
                    var player = await userRepository.FindByIdAsync(pedido.PlayerId);
                    if (player != null && !string.IsNullOrWhiteSpace(player.Phone))
                    {
                        logger.LogInformation("WhatsApp de línea cancelada enviado al jugador para la línea con ID {LineaId}", lineaId);
                        await whatsAppService.SendLineaCanceledMessageAsync(
                            player.Phone,
                            player.Name,
                            canceledLinea.RaquetModel,
                            pedido.Id.ToString());
                    }
                }
            });
    }

    /// <summary>
    /// Changes a single line item's status with full notification dispatch.
    /// </summary>
    /// <remarks>
    /// <para>Flow:</para>
    /// <list type="number">
    ///   <item><description>Parse and validate the status string against <see cref="Status"/> enum.</description></item>
    ///   <item><description>Load the existing line from repository.</description></item>
    ///   <item><description>Persist the status change.</description></item>
    ///   <item><description>Invalidate the parent order cache.</description></item>
    ///   <item><description>Depending on the new status, send appropriate notifications:
    ///     <list type="bullet">
    ///       <item><description><c>COMPLETED</c> — completion email (<see cref="SendLineaCompletedEmailAsync"/>) + WhatsApp + SignalR.</description></item>
    ///       <item><description><c>DELIVERED_TOpLAYER</c> — delivery email (<see cref="SendLineaDeliveredEmailAsync"/>) + WhatsApp + SignalR.</description></item>
    ///       <item><description><c>CANCELED</c> — WhatsApp cancellation + SignalR.</description></item>
    ///       <item><description>Other statuses — SignalR only.</description></item>
    ///     </list>
    ///   </description></item>
    /// </list>
    /// </remarks>
    /// <inheritdoc />
    public async Task<Result<PedidoLineaResponseDto, DomainErrors>> ChangeLineaStatusAsync(Ulid lineaId, string status)
    {
        logger.LogInformation("Cambiando estado de línea con ID {LineaId} a {Status}", lineaId, status);
        if (!Enum.TryParse<Status>(status, true, out var statusEnum))
            return Result.Failure<PedidoLineaResponseDto, DomainErrors>(new InvalidStatusError("Invalid status value"));

        var existingLinea = await repository.FindLineaByIdAsync(lineaId);
        if (existingLinea is null)
            return Result.Failure<PedidoLineaResponseDto, DomainErrors>(new PurchasedNotFoundError())
                .TapError(() => logger.LogWarning("Línea con ID {LineaId} no encontrada", lineaId));

        var updatedLinea = await repository.ChangeLineaStatusAsync(lineaId, statusEnum);
        if (updatedLinea is null)
            return Result.Failure<PedidoLineaResponseDto, DomainErrors>(new PurchasedNotFoundError())
                .TapError(() => logger.LogWarning("No se pudo cambiar el estado de la línea con ID {LineaId}", lineaId));

        await cache.RemoveAsync(CacheKeys.PurchasedCacheKey + updatedLinea.PedidoId);

        return await Result.Success<PedidoLineaResponseDto, DomainErrors>(updatedLinea.ToDto())
            .TapAsync(async _ => {
                var pedido = await repository.FindByIdAsync(updatedLinea.PedidoId);
                if (pedido != null)
                {
                    var player = await userRepository.FindByIdAsync(pedido.PlayerId);
                    if (player != null && !string.IsNullOrWhiteSpace(player.Email) && player.Email.Contains("@"))
                    {
                        if (statusEnum == Status.COMPLETED)
                        {
                            logger.LogInformation("Correo de línea completada enviado al jugador para la línea con ID {LineaId}", lineaId);
                            await SendLineaCompletedEmailAsync(lineaId.ToString(), pedido.Id.ToString(), player.Email,updatedLinea.RaquetModel);
                        }
                        else if (statusEnum == Status.DELIVERED_TOpLAYER)
                        {
                            logger.LogInformation("Correo de línea entregada enviado al jugador para la línea con ID {LineaId}", lineaId);
                            await SendLineaDeliveredEmailAsync(lineaId.ToString(), pedido.Id.ToString(), player.Email,updatedLinea.RaquetModel);
                        }
                    }
                    
                    if (player != null && !string.IsNullOrWhiteSpace(player.Phone))
                    {
                        if (statusEnum == Status.COMPLETED)
                        {
                            logger.LogInformation("WhatsApp de línea completada enviado al jugador para la línea con ID {LineaId}", lineaId);
                            await whatsAppService.SendLineaCompletedMessageAsync(player.Phone, player.Name, updatedLinea.RaquetModel, pedido.Id.ToString());
                        }
                        else if (statusEnum == Status.CANCELED)
                        {
                            logger.LogInformation("WhatsApp de línea cancelada enviado al jugador para la línea con ID {LineaId}", lineaId);
                            await whatsAppService.SendLineaCanceledMessageAsync(player.Phone, player.Name, updatedLinea.RaquetModel, pedido.Id.ToString());
                        }
                    }
                }
            })
            .Tap(() => {
                logger.LogInformation("Estado de línea con ID {LineaId} cambiado exitosamente a {Status}", lineaId,
                    statusEnum);
                SendChangeStatusPurchasedLine(updatedLinea.Id, updatedLinea.PedidoId, updatedLinea.Pedido.TournamentId, updatedLinea.ToDto());
            });
    }

    /// <summary>
    /// Changes the status of every non-conflicting line item in an order at once.
    /// </summary>
    /// <remarks>
    /// <para>Conflict rules:</para>
    /// <list type="bullet">
    ///   <item><description>When target is <c>CANCELED</c>: lines already <c>COMPLETED</c> or <c>DELIVERED_TOpLAYER</c> are skipped (cannot cancel completed work).</description></item>
    ///   <item><description>When target is any non-canceled status (e.g. <c>COMPLETED</c>): lines already <c>CANCELED</c> are skipped (cannot revive a canceled line).</description></item>
    /// </list>
    /// <para>
    /// After filtering eligible lines and persisting changes, the parent order cache is invalidated.
    /// No email or WhatsApp notifications are sent for bulk operations
    /// (individual line notifications are handled by <see cref="ChangeLineaStatusAsync"/>).
    /// </para>
    /// </remarks>
    /// <inheritdoc />
    public async Task<Result<PurchasedResponseDto, DomainErrors>> ChangeAllLineasStatusAsync(Ulid purchasedId, string status)
    {
        logger.LogInformation("Cambiando estado de todas las líneas del pedido {PurchasedId} a {Status}", purchasedId, status);
        
        if (!Enum.TryParse<Status>(status, true, out var statusEnum))
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new InvalidStatusError("Invalid status value"));

        var purchased = await repository.FindByIdAsync(purchasedId);
        if (purchased is null)
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new PurchasedNotFoundError())
                .TapError(() => logger.LogWarning("Pedido con ID {PurchasedId} no encontrado", purchasedId));

        if (purchased.Lineas == null || !purchased.Lineas.Any())
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new PurchasedNotFoundError())
                .TapError(() => logger.LogWarning("El pedido {PurchasedId} no tiene líneas", purchasedId));

        var linesToUpdate = purchased.Lineas.Where(l => 
        {
            if (statusEnum == Status.CANCELED)
                return l.Status != Status.COMPLETED && l.Status != Status.DELIVERED_TOpLAYER;
            else
                return l.Status != Status.CANCELED;
        }).ToList();

        if (!linesToUpdate.Any())
        {
            logger.LogInformation("No hay líneas para actualizar en el pedido {PurchasedId}", purchasedId);
            var playerResult = await GetUserDtoCachedAsync(purchased.PlayerId);
            if (playerResult.IsFailure) return playerResult.Error;
            var encorderResult = await GetUserDtoCachedAsync(purchased.AssignedTo);
            if (encorderResult.IsFailure) return encorderResult.Error;
            return purchased.ToDto(playerResult.Value, encorderResult.Value);
        }

        foreach (var linea in linesToUpdate)
        {
            linea.Status = statusEnum;
            linea.UpdatedAt = DateTime.UtcNow;
        }

        await repository.SaveChangesAsync();
        await cache.RemoveAsync(CacheKeys.PurchasedCacheKey + purchasedId);

        var playerResultFinal = await GetUserDtoCachedAsync(purchased.PlayerId);
        if (playerResultFinal.IsFailure) return playerResultFinal.Error;
        var encorderResultFinal = await GetUserDtoCachedAsync(purchased.AssignedTo);
        if (encorderResultFinal.IsFailure) return encorderResultFinal.Error;

        logger.LogInformation("Se actualizaron {Count} líneas del pedido {PurchasedId} a estado {Status}", 
            linesToUpdate.Count, purchasedId, statusEnum);
        
        return purchased.ToDto(playerResultFinal.Value, encorderResultFinal.Value);
    }

    /// <summary>
    /// Enqueues a line-completed email notification to the player.
    /// </summary>
    /// <param name="lineaId">The line ULID string.</param>
    /// <param name="pedidoId">The parent order ULID string.</param>
    /// <param name="email">The recipient email address.</param>
    /// <param name="productName">The raquet model name for the email template.</param>
    private async Task SendLineaCompletedEmailAsync(string lineaId, string pedidoId, string email,string productName)
    {
        var message = new EmailMessage
        {
            To = email,
            Subject = "Línea completada",
            Body = EmailTemplates.LineaCompleted(lineaId, pedidoId,productName),
            IsHtml = true
        };
        await emailService.EnqueueEmailAsync(message);
    }

    /// <summary>
    /// Enqueues a line-delivered email notification to the player.
    /// </summary>
    /// <param name="lineaId">The line ULID string.</param>
    /// <param name="pedidoId">The parent order ULID string.</param>
    /// <param name="email">The recipient email address.</param>
    /// <param name="productName">The raquet model name for the email template.</param>
    private async Task SendLineaDeliveredEmailAsync(string lineaId, string pedidoId, string email,string productName)
    {
        var message = new EmailMessage
        {
            To = email,
            Subject = "Línea entregada",
            Body = EmailTemplates.LineaDelivered(lineaId, pedidoId,productName),
            IsHtml = true
        };
        await emailService.EnqueueEmailAsync(message);
    }

    /// <summary>
    /// Updates the player's user record with retry logic for concurrency conflicts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is used when auto-paying an order with player bonuses.
    /// The bonus deduction can fail with <see cref="DbUpdateConcurrencyException"/> if another
    /// concurrent request modifies the same user record. The retry strategy:
    /// </para>
    /// <list type="number">
    ///   <item><description>Attempt to save the updated player (with deducted bonus).</description></item>
    ///   <item><description>If <c>DbUpdateConcurrencyException</c> is thrown and retries remain, reload the player entity from DB.</description></item>
    ///   <item><description>Re-apply the bonus deduction on the fresh entity.</description></item>
    ///   <item><description>Retry the save (up to <paramref name="maxRetries"/> attempts).</description></item>
    ///   <item><description>If all retries are exhausted, return <see cref="ConcurrencyError"/>.</description></item>
    /// </list>
    /// <para>If the player is not found after a concurrency conflict, return <see cref="UserNotFoundError"/>.</para>
    /// </remarks>
    /// <param name="player">The player entity with bonus already deducted.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default 3).</param>
    /// <returns><see cref="Unit"/> on success, or a <see cref="ConcurrencyError"/> / <see cref="UserNotFoundError"/>.</returns>
    private async Task<Result<Unit, DomainErrors>> UpdateUserWithRetryAsync(
        User player, 
        int maxRetries = 3)
    {
        int attempt = 0;
        
        while (attempt < maxRetries)
        {
            try
            {
                await userRepository.UpdateAsync(player);
                logger.LogInformation("Usuario actualizado exitosamente en intento {Attempt}", attempt + 1);
                return Result.Success<Unit, DomainErrors>(Unit.Value);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                attempt++;
                logger.LogWarning("Conflicto de concurrencia al actualizar usuario. Intento {Attempt} de {MaxRetries}", 
                    attempt, maxRetries);

                if (attempt >= maxRetries)
                {
                    logger.LogError("Fallaron todos los reintentos para actualizar usuario. Error: {Error}", ex.Message);
                    return Result.Failure<Unit, DomainErrors>(
                        new ConcurrencyError("El usuario fue modificado por otra operación después de 3 reintentos. Intente de nuevo."));
                }

                // Recargar el usuario desde la base de datos para el siguiente intento
                var reloadedPlayer = await userRepository.FindByIdAsync(player.Id);
                if (reloadedPlayer is null)
                {
                    logger.LogError("No se pudo recargar el usuario {PlayerId} para reintento", player.Id);
                    return Result.Failure<Unit, DomainErrors>(
                        new UserNotFoundError("Usuario no encontrado después de conflicto de concurrencia"));
                }

                // Aplicar los cambios de bonos al usuario recargado
                reloadedPlayer.Bonos = player.Bonos;
                player = reloadedPlayer;
            }
        }

        return Result.Failure<Unit, DomainErrors>(
            new ConcurrencyError("No se pudo actualizar el usuario después de varios intentos."));
    }

    /// <summary>
    /// Sends a SignalR <c>PEDIDO_CREADO</c> notification to the tournament group and admin group.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dispatched as fire-and-forget via <c>Task.Run</c>. In case of exception, it is logged
    /// and re-thrown. The payload includes tournamentId, pedidoId, tipo, total, numberOfLines,
    /// the full purchased DTO, and a UTC timestamp.
    /// </para>
    /// <para>Target SignalR groups: <c>Tournament_{tournamentId}</c> and <c>Tournament_All_Admin</c>.</para>
    /// </remarks>
    /// <param name="purchasedId">The new order ID.</param>
    /// <param name="tournamentId">The tournament ID for group targeting.</param>
    /// <param name="response">The full order response DTO.</param>
    private void SendCreatePurchased(Ulid purchasedId, Ulid tournamentId, PurchasedResponseDto response) {
        _ = Task.Run(async () => {
            try {
                var message = new {
                    tournamentId,
                    pedidoId = purchasedId,
                    tipo = "PEDIDO_CREADO",
                    total = response.Price,
                    numberOfLines = response.Lineas.Count,
                    purchased = response,
                    timestamp = DateTime.UtcNow
                };
                var gruposDestino = new List<string> {
                    $"Tournament_{tournamentId}",
                    "Tournament_All_Admin"
                };
                await signal.Clients.Groups(gruposDestino).SendAsync("ReceiveTournamentNotification", message);
                logger.LogInformation(
                    "Notificación de pedido creado enviada para el pedido con ID {PurchasedId} en el torneo {TournamentId}",
                    purchasedId, tournamentId);
            }
            catch (Exception e) {
                Console.WriteLine(e);
                throw;
            }
        });
    }

    /// <summary>
    /// Sends a SignalR <c>PEDIDO_ACTUALIZADO</c> notification to the tournament group and admin group.
    /// </summary>
    /// <remarks>
    /// <para>Dispatched as fire-and-forget via <c>Task.Run</c>.</para>
    /// <para>Target SignalR groups: <c>Tournament_{tournamentId}</c> and <c>Tournament_All_Admin</c>.</para>
    /// </remarks>
    /// <param name="purchasedId">The updated order ID.</param>
    /// <param name="tournamentId">The tournament ID for group targeting.</param>
    /// <param name="response">The full order response DTO after the update.</param>
    private void SendUpdatedPurchased(Ulid purchasedId, Ulid tournamentId, PurchasedResponseDto response) {
        _ = Task.Run(async () => {
            try {
                var message = new {
                    tournamentId,
                    pedidoId = purchasedId,
                    tipo = "PEDIDO_ACTUALIZADO",
                    total = response.Price,
                    numberOfLines = response.Lineas.Count,
                    purchased = response,
                    timestamp = DateTime.UtcNow
                };
                var gruposDestino = new List<string> {
                    $"Tournament_{tournamentId}",
                    "Tournament_All_Admin"
                };
                await signal.Clients.Groups(gruposDestino).SendAsync("ReceiveTournamentNotification", message);
                logger.LogInformation(
                    "Notificación de pedido actualizado enviada para el pedido con ID {PurchasedId} en el torneo {TournamentId}",
                    purchasedId, tournamentId);
            }
            catch (Exception e) {
                Console.WriteLine(e);
                throw;
            }
        });
    }

    /// <summary>
    /// Sends a SignalR <c>PEDIDO_CANCELADO</c> notification to the tournament group and admin group.
    /// </summary>
    /// <remarks>
    /// <para>Dispatched as fire-and-forget via <c>Task.Run</c>.</para>
    /// <para>Target SignalR groups: <c>Tournament_{tournamentId}</c> and <c>Tournament_All_Admin</c>.</para>
    /// </remarks>
    /// <param name="purchasedId">The canceled order ID.</param>
    /// <param name="tournamentId">The tournament ID for group targeting.</param>
    /// <param name="response">The full order response DTO after cancellation.</param>
    private void SendCancelPurchased(Ulid purchasedId, Ulid tournamentId, PurchasedResponseDto response) {
        _ = Task.Run(async () => {
            try {
                var message = new {
                    tournamentId,
                    pedidoId = purchasedId,
                    tipo = "PEDIDO_CANCELADO",
                    total = response.Price,
                    numberOfLines = response.Lineas.Count,
                    purchased = response,
                    timestamp = DateTime.UtcNow
                };
                var gruposDestino = new List<string> {
                    $"Tournament_{tournamentId}",
                    "Tournament_All_Admin"
                };
                await signal.Clients.Groups(gruposDestino).SendAsync("ReceiveTournamentNotification", message);
                logger.LogInformation(
                    "Notificación de pedido cancelado enviada para el pedido con ID {PurchasedId} en el torneo {TournamentId}",
                    purchasedId, tournamentId);
            }
            catch (Exception e) {
                Console.WriteLine(e);
                throw;
            }
        });
    }

    /// <summary>
    /// Sends a SignalR <c>ESTATUS_LINEA_PEDIDO_ACTUALIZADA</c> notification (for status changes on a line).
    /// </summary>
    /// <remarks>
    /// <para>Dispatched as fire-and-forget via <c>Task.Run</c>.</para>
    /// <para>Target SignalR groups: <c>Tournament_{tournamentId}</c> and <c>Tournament_All_Admin</c>.</para>
    /// </remarks>
    /// <param name="lineaId">The line ID whose status changed.</param>
    /// <param name="pedidoId">The parent order ID.</param>
    /// <param name="tournamentId">The tournament ID for group targeting.</param>
    /// <param name="response">The line item response DTO after the change.</param>
    private void SendChangeStatusPurchasedLine(Ulid lineaId, Ulid pedidoId, Ulid tournamentId, PedidoLineaResponseDto response) {
        _ = Task.Run(async () => {
            try {
                var message = new {
                    tournamentId,
                    pedidoId = pedidoId,
                    lineaId = lineaId,
                    tipo = "ESTATUS_LINEA_PEDIDO_ACTUALIZADA",
                    status = response.Status.ToString(),
                    purchased = response,
                    timestamp = DateTime.UtcNow
                };
                var gruposDestino = new List<string> {
                    $"Tournament_{tournamentId}",
                    "Tournament_All_Admin"
                };
                await signal.Clients.Groups(gruposDestino).SendAsync("ReceiveTournamentNotification", message);
                logger.LogInformation(
                    "Notificación de linea de pedido de cambio de estado enviada para el pedido con ID {PurchasedId} en el torneo {TournamentId}",
                    pedidoId, tournamentId);
            }
            catch (Exception e) {
                Console.WriteLine(e);
                throw;
            }
        });
    }

    /// <summary>
    /// Sends a SignalR <c>LINEA_PEDIDO_ACTUALIZADA</c> notification (for partial patch updates to a line).
    /// </summary>
    /// <remarks>
    /// <para>Dispatched as fire-and-forget via <c>Task.Run</c>.</para>
    /// <para>Target SignalR groups: <c>Tournament_{tournamentId}</c> and <c>Tournament_All_Admin</c>.</para>
    /// </remarks>
    /// <param name="lineaId">The line ID that was updated.</param>
    /// <param name="pedidoId">The parent order ID.</param>
    /// <param name="tournamentId">The tournament ID for group targeting.</param>
    /// <param name="response">The line item response DTO after the update.</param>
    private void SendUpdatePurchasedLine(Ulid lineaId, Ulid pedidoId, Ulid tournamentId, PedidoLineaResponseDto response) {
        _ = Task.Run(async () => {
            try {
                var message = new {
                    tournamentId,
                    pedidoId = pedidoId,
                    lineaId = lineaId,
                    tipo = "LINEA_PEDIDO_ACTUALIZADA",
                    status = response.Status.ToString(),
                    purchased = response,
                    timestamp = DateTime.UtcNow
                };
                var gruposDestino = new List<string> {
                    $"Tournament_{tournamentId}",
                    "Tournament_All_Admin"
                };
                await signal.Clients.Groups(gruposDestino).SendAsync("ReceiveTournamentNotification", message);
                logger.LogInformation(
                    "Notificación de linea de pedido de cambio de estado enviada para el pedido con ID {PurchasedId} en el torneo {TournamentId}",
                    pedidoId, tournamentId);
            }
            catch (Exception e) {
                Console.WriteLine(e);
                throw;
            }
        });
    }
}
