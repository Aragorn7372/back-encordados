using BackEncordados.Common.Database.Config;
using BackEncordados.Purchased.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Export.Repository;

public class PedidosExportRepository(
    PedidosDbContext pedidosDbContext,
    ILogger<PedidosExportRepository> logger
) : IPedidosExportRepository
{
    public async Task<List<Pedidos>> GetPedidosDataAsync()
    {
        var pedidos = await pedidosDbContext.Pedidos.ToListAsync();
        var lineasLookup = (await pedidosDbContext.PedidoLineas.ToListAsync())
            .ToLookup(x => x.PedidoId);

        foreach (var pedido in pedidos)
        {
            pedido.Lineas = lineasLookup[pedido.Id].ToList();
        }

        logger.LogInformation("Fetched {Count} pedidos", pedidos.Count);
        return pedidos;
    }

    public async Task ClearPedidosAsync()
    {
        if (pedidosDbContext.Database.IsInMemory())
        {
            var lineas = await pedidosDbContext.PedidoLineas.ToListAsync();
            pedidosDbContext.PedidoLineas.RemoveRange(lineas);

            var pedidos = await pedidosDbContext.Pedidos.ToListAsync();
            pedidosDbContext.Pedidos.RemoveRange(pedidos);

            await pedidosDbContext.SaveChangesAsync();
            logger.LogInformation("Cleared pedidos (in-memory)");
        }
        else
        {
            var lineas = await pedidosDbContext.PedidoLineas.ToListAsync();
            pedidosDbContext.PedidoLineas.RemoveRange(lineas);

            var pedidos = await pedidosDbContext.Pedidos.ToListAsync();
            pedidosDbContext.Pedidos.RemoveRange(pedidos);

            await pedidosDbContext.SaveChangesAsync();
            logger.LogInformation("Cleared pedidos (production)");
        }
    }

    public async Task ImportPedidosAsync(List<Pedidos> pedidos)
    {
        if (!pedidos.Any()) return;

        await pedidosDbContext.Pedidos.AddRangeAsync(pedidos);
        await pedidosDbContext.SaveChangesAsync();
        logger.LogInformation("Imported {Count} pedidos", pedidos.Count);
    }
}
