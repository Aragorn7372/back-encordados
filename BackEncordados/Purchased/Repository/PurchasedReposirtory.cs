using BackEncordados.Common.Database.Config;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Purchased.Repository;

public class PurchasedReposirtory(PedidosDbContext context, ILogger<PurchasedReposirtory> logger): IPuchasedRepository
{
    public async Task<(IEnumerable<Pedidos> Items, int TotalCount)> FindAllAsync(FilterPurchasedDto filter)
    {
        var query = context.Pedidos.AsQueryable();
        if(filter.IsEncorder == true && filter.UserId != null && Ulid.TryParse(filter.UserId, out var encorderId)) query = query.Where(p => p.AssignedTo == encorderId);

        if (filter.IsUser == true && filter.UserId != null && Ulid.TryParse(filter.UserId, out var userId)) query = query.Where(p => p.PlayerId == userId);

        if (filter.Search.Length > 0) {
            query = query.Where(p => EF.Functions.Like(p.RaquetModel, $"%{filter.Search}%") 
                                     || EF.Functions.Like(p.Comments, $"%{filter.Search}%") 
                                     || EF.Functions.Like(p.Machine, $"%{filter.Search}%"));
        }
        var totalCount = await query.CountAsync();
        bool isDesc = filter.Direction.ToLower().Equals("desc");
        query = filter.SortBy.ToLower() switch
        {
            "raquetmodel" => isDesc ? query.OrderByDescending(p => p.RaquetModel) : query.OrderBy(p => p.RaquetModel),
            "dateEnd" => isDesc ? query.OrderByDescending(p => p.DateString) : query.OrderBy(p => p.DateString),
            "machine" => isDesc ? query.OrderByDescending(p => p.Machine) : query.OrderBy(p => p.Machine),
            "createdAt" => isDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
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
        return await context.Pedidos.Include(p => p.StringSetup ).FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Pedidos> CreatePurchasedAsync(Pedidos pedidos)
    {
        logger.LogInformation("Creando pedido");
        pedidos.Id = Ulid.NewUlid();
        var saved =await context.Pedidos.AddAsync(pedidos);
        await context.SaveChangesAsync();
        return saved.Entity;
    }

    public async Task<Pedidos?> UpdatePurchasedAsync(Pedidos pedidos, Ulid id)
    {
        logger.LogInformation("Atualizando pedido con ID {Id}", id);
        
        var existingPurchased = await context.Pedidos
            .Include(p => p.StringSetup)
            .FirstOrDefaultAsync(p => p.Id == id && p.Status != Status.CANCELED);

        if (existingPurchased == null)
        {
            logger.LogWarning("Pedido no encontrado o está cancelado. ID {Id}", id);
            return null;
        }

        if (!string.IsNullOrEmpty(pedidos.TypeString))
            existingPurchased.TypeString = pedidos.TypeString;

        if (pedidos.TypeWork != default)
            existingPurchased.TypeWork = pedidos.TypeWork;

        if (pedidos.DateString != default)
            existingPurchased.DateString = pedidos.DateString;

        if (pedidos.Logotype != default)
            existingPurchased.Logotype = pedidos.Logotype;

        if (!string.IsNullOrEmpty(pedidos.RaquetModel))
            existingPurchased.RaquetModel = pedidos.RaquetModel;

        if (pedidos.Price > 0)
            existingPurchased.Price = pedidos.Price;

        if (pedidos.Nudos > 0)
            existingPurchased.Nudos = pedidos.Nudos;

        if (!string.IsNullOrEmpty(pedidos.Machine))
            existingPurchased.Machine = pedidos.Machine;

        if (!string.IsNullOrEmpty(pedidos.Comments))
            existingPurchased.Comments = pedidos.Comments;

        // Actualizar StringSetup si fue proporcionado
        if (!string.IsNullOrEmpty(pedidos.StringSetup.StringV))
            existingPurchased.StringSetup.StringV = pedidos.StringSetup.StringV;

        if (pedidos.StringSetup.TensionV > 0)
            existingPurchased.StringSetup.TensionV = pedidos.StringSetup.TensionV;

        if (pedidos.StringSetup.PreStetchV > 0)
            existingPurchased.StringSetup.PreStetchV = pedidos.StringSetup.PreStetchV;

        if (!string.IsNullOrEmpty(pedidos.StringSetup.StringH))
            existingPurchased.StringSetup.StringH = pedidos.StringSetup.StringH;

        if (pedidos.StringSetup.TensionH > 0)
            existingPurchased.StringSetup.TensionH = pedidos.StringSetup.TensionH;

        if (pedidos.StringSetup.PreStetchH > 0)
            existingPurchased.StringSetup.PreStetchH = pedidos.StringSetup.PreStetchH;

        var saved=context.Pedidos.Update(existingPurchased);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Pedido actualizado exitosamente. ID {Id}", id);
        return saved.Entity;
    }

    public async Task<Pedidos?> CancelPurchasedAsync(Ulid id)
    {
        logger.LogInformation("Cancelando pedido con ID {Id}", id);
        var existingPurchased = await context.Pedidos.FirstOrDefaultAsync(p => p.Id == id && p.Status != Status.CANCELED && p.PayStatus != PaymentStatus.CANCELED);
        if (existingPurchased == null)
            return null;
        existingPurchased.Status = Status.CANCELED;
        existingPurchased.PayStatus = PaymentStatus.CANCELED;
        var saved=context.Pedidos.Update(existingPurchased);
        await context.SaveChangesAsync();
        logger.LogInformation("Pedido cancelado exitosamente. ID {Id}", id);
        return saved.Entity;
    }

    public async Task<Pedidos?> ChangeStatusPurchasedAsync(Ulid id, string status)
    {
        logger.LogInformation("Atualizando estado de pedido con ID {Id}", id);
        var existingPurchased = await  context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
        if (existingPurchased == null)
            return null;
        existingPurchased.Status = Enum.TryParse<Status>(status, true, out var newStatus) ? newStatus : existingPurchased.Status;
        var saved=context.Pedidos.Update(existingPurchased);
        await context.SaveChangesAsync();
        logger.LogInformation("Pedido actualizado exitosamente. ID {Id}", id);
        return saved.Entity;
    }

    public async Task<Pedidos?> ChangePaymentStatusPurchasedAsync(Ulid id, string payStatus)
    {
        logger.LogInformation("Atualizando estado de pago del pedido con ID {Id}", id);
        var existingPurchased = await  context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
        if (existingPurchased == null)
            return null;
        existingPurchased.PayStatus = Enum.TryParse<PaymentStatus>(payStatus, true, out var newPayStatus) ? newPayStatus : existingPurchased.PayStatus;
        var saved=context.Pedidos.Update(existingPurchased);
        await context.SaveChangesAsync();
        logger.LogInformation("Estado de pago del pedido actualizado exitosamente. ID {Id}", id);
        return saved.Entity;
    }
}