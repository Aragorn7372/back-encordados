using BackEncordados.Common.Database.Config;
using BackEncordados.Purchased.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Excel.Repository;

public class PedidosExcelRepository(
    PedidosDbContext pedidosDbContext,
    ILogger<PedidosExcelRepository> logger
) : IPedidosExcelRepository
{
    public async Task<List<Pedidos>> GetPedidosByTournamentAsync(Ulid tournamentId)
    {
        var pedidos = await pedidosDbContext.Pedidos
            .Where(p => p.TournamentId == tournamentId)
            .ToListAsync();

        logger.LogInformation("Fetched {Count} pedidos for tournament {TournamentId}", pedidos.Count, tournamentId);
        return pedidos;
    }

    public async Task<List<PedidoLinea>> GetPedidoLineasByPedidoIdsAsync(List<Ulid> pedidoIds)
    {
        if (pedidoIds.Count == 0)
            return [];

        var lineas = await pedidosDbContext.PedidoLineas
            .Where(l => pedidoIds.Contains(l.PedidoId))
            .ToListAsync();

        logger.LogInformation("Fetched {Count} pedido lineas for {PedidoCount} pedidos", lineas.Count, pedidoIds.Count);
        return lineas;
    }
}
