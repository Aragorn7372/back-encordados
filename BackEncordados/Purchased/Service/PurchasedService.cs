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
    private Task SendPaidEmailAsync(string orderId,double price, string email) {
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
            .Tap(() => {
                logger.LogInformation("Línea con ID {LineaId} actualizada exitosamente", lineaId);
                SendUpdatePurchasedLine(savedLinea.Id, savedLinea.PedidoId, savedLinea.Pedido.TournamentId, savedLinea.ToDto());
            });
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

    /// <summary>
    /// Actualiza el usuario con reintentos en caso de conflicto de concurrencia.
    /// Intenta hasta 3 veces; si falla después, retorna error.
    /// </summary>
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