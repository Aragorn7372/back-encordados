using BackEncordados.Common.Dto;
using BackEncordados.Common.Errors;
using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.keys;
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

public class PurchasedService(IPuchasedRepository repository,IUserRepository userRepository, ILogger<PurchasedService> logger,ICacheService cache): IPurchasedService
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

    public async Task<Result<PurchasedResponseDto, DomainErrors>> FindByIdAsync(Guid id)
    {
        logger.LogInformation("Buscando pedido con ID: {Id}", id);
        // Intentar obtener el pedido de caché
        var purchasedCached = await cache.GetAsync<PurchasedResponseDto>(CacheKeys.PurchasedCacheKey + id);
        if (purchasedCached != null) return Result.Success<PurchasedResponseDto, DomainErrors>(purchasedCached)
            .Tap(()=>logger.LogInformation("Pedido con ID {Id} obtenido de caché",id));

        //  Si no está, buscar en DB
        var purchased = await repository.FindByIdAsync(id);
        if (purchased is null) return Result.Failure<PurchasedResponseDto,DomainErrors>(new PurchasedNotFoundError())
            .TapError(()=>logger.LogWarning("Pedido con ID {Id} no encontrado en DB",id));

        // Usamos el método que busca primero en caché de datos y luego en DB
        var playerResult = await GetUserDtoCachedAsync(purchased.PlayerId);
        if (playerResult.IsFailure) return playerResult.Error;
        var encorderResult = await GetUserDtoCachedAsync(purchased.AssignedTo);
        if (encorderResult.IsFailure) return encorderResult.Error;

        var response = purchased.ToDto(encorderResult.Value, playerResult.Value);

        //  Guardar en caché 
        await cache.SetAsync(CacheKeys.PurchasedCacheKey + id, response, TimeSpan.FromMinutes(5));
    
        return response;
    }
    
    private async Task<Result<UserResponseDto, DomainErrors>> GetUserDtoCachedAsync(Guid userId)
    {
        string key = CacheKeys.UserDataKey + userId;
        var cached = await cache.GetAsync<UserResponseDto>(key);
        if (cached != null) return Result.Success<UserResponseDto, DomainErrors>(cached);

        var user = await userRepository.FindByIdAsync(userId);
        if (user == null || user.IsDeleted) return Result.Failure<UserResponseDto,DomainErrors>(new  UserNotFoundError("User no exists or was deleted"))
            .TapError(()=>logger.LogWarning("Usuario con ID {Id} no encontrado o eliminado en DB",userId));

        var dto = user.ToDto();
        await cache.SetAsync(key, dto, TimeSpan.FromMinutes(10)); 
        return Result.Success<UserResponseDto, DomainErrors>(dto)
            .Tap(()=>logger.LogInformation("Usuario con ID {Id} obtenido de DB y guardado en caché",userId));
    }
    public async Task<Result<PurchasedResponseDto, DomainErrors>> CreatePurchasedAsync(PurchasedRequestDto request)
    {
        logger.LogInformation("Creando pedido para jugador {PlayerName} asignado a {AssignedToName}", request.PlayerName, request.AssignedToName);
        //  Obtener los IDs 
        var player = await cache.GetAsync<User>(CacheKeys.UserKey + request.PlayerName);
        var encorder = await cache.GetAsync<User>(CacheKeys.UserKey + request.AssignedToName);

        // Fallback por si la caché expiró entre validación y ejecución
        if (player == null) player = await userRepository.FindByUsernameAsync(request.PlayerName!);
        if (encorder == null) encorder = await userRepository.FindByUsernameAsync(request.AssignedToName!);

        if (player == null || encorder == null) 
            return Result.Failure<PurchasedResponseDto,DomainErrors>(new UserNotFoundError("Player or Encorder not found"))
                .TapError(()=>logger.LogWarning("Jugador o encordador no encontrado. Player: {PlayerName}, Encorder: {AssignedToName}",request.PlayerName, request.AssignedToName));

        //  Crear y Guardar
        var entity = request.ToEntity(player.Id, encorder.Id);
        await repository.CreatePurchasedAsync(entity);

        // Generar respuesta
        var response = entity.ToDto(encorder.ToDto(), player.ToDto());
    
        //  Invalida o refresca el caché del pedido específico
        await cache.SetAsync(CacheKeys.PurchasedCacheKey + entity.Id, response, TimeSpan.FromMinutes(5));
        
        return Result.Success<PurchasedResponseDto, DomainErrors>(response)
            .Tap(()=>logger.LogInformation("Pedido creado con ID {Id} y guardado en caché",entity.Id));
    }

    public async Task<Result<PurchasedResponseDto, DomainErrors>> UpdatePurchasedAsync(Guid id, PurchasedPatchDto request)
    {
        logger.LogInformation("Actualizando pedido con ID {Id}", id);
        
        // Obtener el pedido existente
        var existingPurchased = await repository.FindByIdAsync(id);
        if (existingPurchased is null) 
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new PurchasedNotFoundError())
                .TapError(() => logger.LogWarning("Pedido con ID {Id} no encontrado para actualizar", id));

        // Preparar StringSetup si es necesario
        StringSetup updatedStringSetup = existingPurchased.StringSetup;
        if (request.StringSetup != null)
        {
            updatedStringSetup=request.StringSetup.ToModel();
        }

        // Crear entidad con solo los campos del patch que no son nulos
        var patchEntity = new Pedidos
        {
            TypeString = request.TypeString ?? existingPurchased.TypeString,
            TypeWork = request.TypeWork != null ? Enum.Parse<TypePuchase>(request.TypeWork, true) : existingPurchased.TypeWork,
            DateString = request.DateString ?? existingPurchased.DateString,
            Logotype = request.Logotype ?? existingPurchased.Logotype,
            RaquetModel = request.RaquetModel ?? existingPurchased.RaquetModel,
            Price = request.Price ?? existingPurchased.Price,
            Nudos = request.Nudos ?? existingPurchased.Nudos,
            Machine = request.Machine ?? existingPurchased.Machine,
            Comments = request.Comments ?? existingPurchased.Comments,
            StringSetup = updatedStringSetup
        };

        // Actualizar en DB
        var updated = await repository.UpdatePurchasedAsync(patchEntity, id);
        if (updated is null)
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new PurchasedNotFoundError())
                .TapError(() => logger.LogWarning("No se pudo actualizar el pedido con ID {Id}", id));

        // Obtener datos de usuario para la respuesta
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
    
    public async Task<Result<PurchasedResponseDto, DomainErrors>> CancelPurchasedAsync(Guid id, bool isUser,
        string? idUser)
    {
        logger.LogInformation("Cancelando pedido con ID {Id}", id);
        var purchasedCaceled= await repository.CancelPurchasedAsync(id);
        if (purchasedCaceled is null) return Result.Failure<PurchasedResponseDto, DomainErrors>(new PurchasedNotFoundError())
            .TapError(()=>logger.LogWarning("Pedido con ID {Id} no encontrado para cancelar",id));
        if (isUser && Guid.TryParse(idUser, out Guid guid) && purchasedCaceled.PlayerId != guid) {
            logger.LogWarning("Usuario con ID {UserId} no autorizado para cancelar el pedido con ID {Id}", idUser, id);
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new UnauthorizedError("User not authorized to cancel this purchase"));
        }
        var playerResult = await GetUserDtoCachedAsync(purchasedCaceled.PlayerId);
        if (playerResult.IsFailure) return playerResult.Error;
        var encorderResult = await GetUserDtoCachedAsync(purchasedCaceled.AssignedTo);
        if (encorderResult.IsFailure) return encorderResult.Error;
        return await Result.Success<PurchasedResponseDto,DomainErrors>(purchasedCaceled.ToDto(encorderResult.Value, playerResult.Value))
            .TapAsync(async response=>
            {
                await cache.SetAsync(CacheKeys.PurchasedCacheKey + id, response, TimeSpan.FromMinutes(5));
                logger.LogInformation("Pedido con ID {Id} cancelado exitosamente", id);
            });
        
    }

    public async Task<Result<PurchasedResponseDto, DomainErrors>> ChangeStatusPurchasedAsync(Guid id, string status)
    {
        logger.LogInformation("Cambiando el estatus al pedido con ID {Id}", id);
        if (!Enum.TryParse<PaymentStatus>(status,true, out var statusEnum))
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new InvalidStatusError("Invalid status value"));
        
        var purchased= await repository.ChangeStatusPurchasedAsync(id, status);
        if (purchased is null) return Result.Failure<PurchasedResponseDto, DomainErrors>(new PurchasedNotFoundError())
            .TapError(()=>logger.LogWarning("Pedido con ID {Id} no encontrado para cambiar estatus",id));
        var playerResult = await GetUserDtoCachedAsync(purchased.PlayerId);
        if (playerResult.IsFailure) return playerResult.Error;
        var encorderResult = await GetUserDtoCachedAsync(purchased.AssignedTo);
        if (encorderResult.IsFailure) return encorderResult.Error;
        return await Result.Success<PurchasedResponseDto,DomainErrors>(purchased.ToDto(encorderResult.Value, playerResult.Value))
            .TapAsync(async response=>
            {
                await cache.SetAsync(CacheKeys.PurchasedCacheKey + id, response, TimeSpan.FromMinutes(5));
                logger.LogInformation("Estatus del pedido con ID {Id} cambiado exitosamente a {StatusEnum}", id, statusEnum);
            });
    }

    public async Task<Result<PurchasedResponseDto, DomainErrors>> ChangePaymentStatusPurchasedAsync(Guid id, string payStatus)
    {
        logger.LogInformation("Cambiando el estatus de pago al pedido con ID {Id}", id);
        if (!Enum.TryParse<PaymentStatus>(payStatus,true, out var payStatusEnum))
            return Result.Failure<PurchasedResponseDto, DomainErrors>(new InvalidStatusError("Invalid payment status value"));
        var purchased=await repository.ChangePaymentStatusPurchasedAsync(id, payStatus);
        if (purchased is null) return Result.Failure<PurchasedResponseDto, DomainErrors>(new PurchasedNotFoundError())
            .TapError(()=>logger.LogWarning("Pedido con ID {Id} no encontrado para cambiar estatus de pago",id));  
        var playerResult = await GetUserDtoCachedAsync(purchased.PlayerId);
        if (playerResult.IsFailure) return playerResult.Error;
        var encorderResult = await GetUserDtoCachedAsync(purchased.AssignedTo);
        if (encorderResult.IsFailure) return encorderResult.Error;
        return await Result.Success<PurchasedResponseDto,DomainErrors>(purchased.ToDto(encorderResult.Value, playerResult.Value))
            .TapAsync(async response=>
            {
                await cache.SetAsync(CacheKeys.PurchasedCacheKey + id, response, TimeSpan.FromMinutes(5));
                logger.LogInformation("Estatus de pago del pedido con ID {Id} cambiado exitosamente a {PayStatusEnum}", id, payStatusEnum);
            });
    }
}