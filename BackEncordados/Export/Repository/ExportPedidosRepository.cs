using BackEncordados.Common.Database.Config;
using BackEncordados.Purchased.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Export.Repository;

public class ExportPedidosRepository(
    PedidosDbContext pedidosDbContext,
    ILogger<ExportPedidosRepository> logger
) : IExportPedidosRepository
{
    public async Task<List<Pedidos>> GetAllPedidosAsync()
    {
        var pedidos = await pedidosDbContext.Pedidos.ToListAsync();
        var lineasLookup = (await pedidosDbContext.PedidoLineas.ToListAsync())
            .ToLookup(x => x.PedidoId);

        foreach (var pedido in pedidos)
        {
            pedido.Lineas = lineasLookup[pedido.Id].ToList();
        }

        return pedidos;
    }

    public async Task ClearPedidosAsync()
    {
        var pedidoLineas = await pedidosDbContext.PedidoLineas.ToListAsync();
        pedidosDbContext.PedidoLineas.RemoveRange(pedidoLineas);

        var pedidos = await pedidosDbContext.Pedidos.ToListAsync();
        pedidosDbContext.Pedidos.RemoveRange(pedidos);

        await pedidosDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared pedidos");
    }

    public async Task ImportPedidosAsync(List<Pedidos> pedidos)
    {
        await pedidosDbContext.Pedidos.AddRangeAsync(pedidos);
        await pedidosDbContext.SaveChangesAsync();
        logger.LogInformation("Imported {Count} pedidos", pedidos.Count);
    }
}
