using BackEncordados.Common.Dto;
using BackEncordados.Common.Errors;
using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.keys;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Common.Service.Email;
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

namespace BackEncordados.Purchased.Service;

public class PurchasedService(
    IPuchasedRepository repository,
    IUserRepository userRepository, 
    ILogger<PurchasedService> logger, 
    ICacheService cache,
    ICloudinaryService cloudinary,
    IEmailService emailService
    ) : IPurchasedService
{
    public async Task<PageResponseDto<PurchasedResponseDto>> FindAllAsync(FilterPurchasedDto filter)
    {
        logger.LogInformation("Obteniendo todos los pedidos con filtro: Página {Page}, Tamaño {Size}", filter.Page, filter.Size);
        var (paginatedItems, totalCount) = await repository.FindAllAsync(filter);
        int totalPages = filter.Size > 0 ? (int)Math.Ceiling(totalCount / (double)filter.Size) : 0;
        var items = new List<PurchasedResponseDto>();

        foreach (var item in paginatedItems)
        {
            var playerResult = await GetUserDtoCachedAsync(item.PlayerId);
            if (playerResult.IsFailure)
            {
                logger.LogWarning("Jugador con ID {PlayerId} no encontrado para el pedido {PurchasedId}", item.PlayerId, item.Id);
                continue;
            }

            var encorderResult = await GetUserDtoCachedAsync(item.AssignedTo);
            if (encorderResult.IsFailure)
            {
                logger.LogWarning("Encordador con ID {EncorderId} no encontrado para el pedido {PurchasedId}", item.AssignedTo, item.Id);
                continue;
            }

            items.Add(item.ToDto(encorderResult.Value, playerResult.Value));
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

        var response = purchased.ToDto(encorderResult.Value, playerResult.Value);

        await cache.SetAsync(CacheKeys.PurchasedCacheKey + id, response, TimeSpan.FromMinutes(5));

        return response;
    }

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
        await repository.CreatePurchasedAsync(entity);

        var response = entity.ToDto(encorder.ToDto(cloudinary), player.ToDto(cloudinary));

        await cache.SetAsync(CacheKeys.PurchasedCacheKey + entity.Id, response, TimeSpan.FromMinutes(5));

        return Result.Success<PurchasedResponseDto, DomainErrors>(response)
            .Tap(() => logger.LogInformation("Pedido creado con ID {Id} y guardado en caché", entity.Id));
    }

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

        var response = updated.ToDto(encorderResult.Value, playerResult.Value);

        await cache.SetAsync(CacheKeys.PurchasedCacheKey + id, response, TimeSpan.FromMinutes(5));

        logger.LogInformation("Pedido con ID {Id} actualizado exitosamente", id);
        return Result.Success<PurchasedResponseDto, DomainErrors>(response);
    }

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

        var response = purchasedCanceled.ToDto(encorderResult.Value, playerResult.Value);
        await cache.SetAsync(CacheKeys.PurchasedCacheKey + id, response, TimeSpan.FromMinutes(5));
        logger.LogInformation("Pedido con ID {Id} cancelado exitosamente", id);
        return await Result.Success<PurchasedResponseDto, DomainErrors>(response)
            .TapAsync(async response => {
                logger.LogInformation(
                    "Correo de cancelación enviado al jugador con ID {PlayerId} para el pedido con ID {Id}",
                    response.Id, id);
                var email= await GetValidUserWithEmailAsync(response.Player.Username);
                if (email.IsSuccess) await SendCancelEmailAsync(id.ToString(), email.Value);
            });
    }
    
    private async Task SendCancelEmailAsync(string orderId, string email) {
        var message = new EmailMessage {
            To = email,
            Subject = "Pedido cancelado",
            Body = EmailTemplates.OrderCancelled(orderId),
            IsHtml = true
        };
        await emailService.EnqueueEmailAsync(message);
    }
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

        var response = purchased.ToDto(encorderResult.Value, playerResult.Value);
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
    Task SendPaidEmailAsync(string orderId,double price, string email) {
        var message = new EmailMessage {
            To = email,
            Subject = "Pago confirmado",
            Body = EmailTemplates.PaymentConfirmed(orderId,price),
            IsHtml = true
        };
        return emailService.EnqueueEmailAsync(message);
    }
    
    

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
            .Tap(() => logger.LogInformation("Línea con ID {LineaId} actualizada exitosamente", lineaId));
    }

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

        return Result.Success<PedidoLineaResponseDto, DomainErrors>(canceledLinea.ToDto())
            .Tap(() => logger.LogInformation("Línea con ID {LineaId} cancelada exitosamente", lineaId));
    }

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
                            await SendLineaCompletedEmailAsync(lineaId.ToString(), pedido.Id.ToString(), player.Email,existingLinea.RaquetModel);
                        }
                        else if (statusEnum == Status.DELIVERED_TOpLAYER)
                        {
                            logger.LogInformation("Correo de línea entregada enviado al jugador para la línea con ID {LineaId}", lineaId);
                            await SendLineaDeliveredEmailAsync(lineaId.ToString(), pedido.Id.ToString(), player.Email,existingLinea.RaquetModel);
                        }
                    }
                }
            })
            .Tap(() => logger.LogInformation("Estado de línea con ID {LineaId} cambiado exitosamente a {Status}", lineaId, statusEnum));
    }

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
    
}