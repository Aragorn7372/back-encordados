using BackEncordados.Common.Database.Config;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Purchased.Repository;

public class PurchasedReposirtory(PedidosDbContext context, ILogger<PurchasedReposirtory> logger) : IPuchasedRepository
{
    public async Task<(IEnumerable<Pedidos> Items, int TotalCount)> FindAllAsync(FilterPurchasedDto filter)
    {
        var query = context.Pedidos
            .Include(p => p.Lineas)
            .AsQueryable();

        if (filter.IsEncorder == true && filter.UserId != null && Ulid.TryParse(filter.UserId, out var encorderId))
            query = query.Where(p => p.AssignedTo == encorderId);

        if (filter.IsUser == true && filter.UserId != null && Ulid.TryParse(filter.UserId, out var userId))
            query = query.Where(p => p.PlayerId == userId);

        if (filter.TournamentId.HasValue)
            query = query.Where(p => p.TournamentId == filter.TournamentId.Value);

        if (!string.IsNullOrEmpty(filter.Search))
        {
            query = query.Where(p => EF.Functions.Like(p.Comments, $"%{filter.Search}%")
                          || EF.Functions.Like(p.Machine, $"%{filter.Search}%")
                          || p.Lineas.Any(l => EF.Functions.Like(l.RaquetModel, $"%{filter.Search}%")));
        }

        var totalCount = await query.CountAsync();

        bool isDesc = filter.Direction.ToLower().Equals("desc");
        query = filter.SortBy.ToLower() switch
        {
            "createdAt" => isDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "machine" => isDesc ? query.OrderByDescending(p => p.Machine) : query.OrderBy(p => p.Machine),
            "playerId" => isDesc ? query.OrderByDescending(p => p.PlayerId) : query.OrderBy(p => p.PlayerId),
            "encorder" => isDesc ? query.OrderByDescending(p => p.AssignedTo) : query.OrderBy(p => p.AssignedTo),
            _ => isDesc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id)
        };

        var items = await query.Skip(filter.Page * filter.Size).Take(filter.Size).ToListAsync();
        return (items, totalCount);
    }

    public async Task<Pedidos?> FindByIdAsync(Ulid id)
    {
        logger.LogInformation("Buscando pedido con ID {Id}", id);
        return await context.Pedidos
            .Include(p => p.Lineas)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Pedidos> CreatePurchasedAsync(Pedidos pedidos)
    {
        logger.LogInformation("Creando pedido");
        var saved = await context.Pedidos.AddAsync(pedidos);
        await context.SaveChangesAsync();
        return saved.Entity;
    }

    public async Task<Pedidos?> UpdatePurchasedAsync(Pedidos pedidos, Ulid id)
    {
        logger.LogInformation("Actualizando pedido con ID {Id}", id);

        var existingPurchased = await context.Pedidos
            .Include(p => p.Lineas)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existingPurchased == null)
        {
            logger.LogWarning("Pedido no encontrado. ID {Id}", id);
            return null;
        }

        if (!string.IsNullOrEmpty(pedidos.Machine))
            existingPurchased.Machine = pedidos.Machine;

        if (!string.IsNullOrEmpty(pedidos.Comments))
            existingPurchased.Comments = pedidos.Comments;

        if (pedidos.PayStatus != default)
            existingPurchased.PayStatus = pedidos.PayStatus;

        existingPurchased.UpdatedAt = DateTime.UtcNow;

        var saved = context.Pedidos.Update(existingPurchased);
        await context.SaveChangesAsync();

        logger.LogInformation("Pedido actualizado exitosamente. ID {Id}", id);
        return saved.Entity;
    }

    public async Task<Pedidos?> CancelPurchasedAsync(Ulid id)
    {
        logger.LogInformation("Cancelando pedido con ID {Id}", id);
        var existingPurchased = await context.Pedidos
            .Include(p => p.Lineas)
            .FirstOrDefaultAsync(p => p.Id == id && p.PayStatus != PaymentStatus.CANCELED);

        if (existingPurchased == null)
            return null;

        existingPurchased.PayStatus = PaymentStatus.CANCELED;
        foreach (var linea in existingPurchased.Lineas)
        {
            linea.Status = Status.CANCELED;
            linea.UpdatedAt = DateTime.UtcNow;
        }

        var saved = context.Pedidos.Update(existingPurchased);
        await context.SaveChangesAsync();
        logger.LogInformation("Pedido cancelado exitosamente. ID {Id}", id);
        return saved.Entity;
    }

    public async Task<Pedidos?> ChangeStatusPurchasedAsync(Ulid id, string payStatus)
    {
        logger.LogInformation("Cambiando estado de pago del pedido con ID {Id}", id);
        var existingPurchased = await context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
        if (existingPurchased == null)
            return null;

        existingPurchased.PayStatus = Enum.TryParse<PaymentStatus>(payStatus, true, out var newPayStatus)
            ? newPayStatus
            : existingPurchased.PayStatus;

        existingPurchased.UpdatedAt = DateTime.UtcNow;

        var saved = context.Pedidos.Update(existingPurchased);
        await context.SaveChangesAsync();
        logger.LogInformation("Estado de pago del pedido actualizado exitosamente. ID {Id}", id);
        return saved.Entity;
    }

    public async Task<PedidoLinea?> FindLineaByIdAsync(Ulid lineaId)
    {
        logger.LogInformation("Buscando línea con ID {Id}", lineaId);
        return await context.PedidoLineas.FirstOrDefaultAsync(l => l.Id == lineaId);
    }

    public async Task<PedidoLinea> CreateLineaAsync(PedidoLinea linea)
    {
        logger.LogInformation("Creando línea para pedido {PedidoId}", linea.PedidoId);
        linea.Id = Ulid.NewUlid();
        var saved = await context.PedidoLineas.AddAsync(linea);
        await context.SaveChangesAsync();
        return saved.Entity;
    }

    public async Task<PedidoLinea?> UpdateLineaAsync(PedidoLinea linea, Ulid lineaId)
    {
        logger.LogInformation("Actualizando línea con ID {Id}", lineaId);

        var existingLinea = await context.PedidoLineas.FirstOrDefaultAsync(l => l.Id == lineaId);
        if (existingLinea == null)
        {
            logger.LogWarning("Línea no encontrada. ID {Id}", lineaId);
            return null;
        }

        existingLinea.RaquetModel = linea.RaquetModel;
        existingLinea.Nudos = linea.Nudos;
        existingLinea.DateString = linea.DateString;
        existingLinea.Logotype = linea.Logotype;
        existingLinea.Color = linea.Color;
        existingLinea.Status = linea.Status;
        existingLinea.StringSetup = linea.StringSetup;
        existingLinea.UpdatedAt = DateTime.UtcNow;

        var saved = context.PedidoLineas.Update(existingLinea);
        await context.SaveChangesAsync();

        logger.LogInformation("Línea actualizada exitosamente. ID {Id}", lineaId);
        return saved.Entity;
    }

    public async Task<PedidoLinea?> ChangeLineaStatusAsync(Ulid lineaId, Status status)
    {
        logger.LogInformation("Cambiando estado de línea con ID {Id} a {Status}", lineaId, status);
        var linea = await context.PedidoLineas.FirstOrDefaultAsync(l => l.Id == lineaId);
        if (linea == null)
            return null;

        linea.Status = status;
        linea.UpdatedAt = DateTime.UtcNow;

        var saved = context.PedidoLineas.Update(linea);
        await context.SaveChangesAsync();

        logger.LogInformation("Estado de línea cambiado exitosamente. ID {Id}, Status {Status}", lineaId, status);
        return saved.Entity;
    }
}