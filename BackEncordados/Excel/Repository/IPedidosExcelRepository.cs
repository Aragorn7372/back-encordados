using BackEncordados.Purchased.Model;

namespace BackEncordados.Excel.Repository;

public interface IPedidosExcelRepository
{
    Task<List<Pedidos>> GetPedidosByTournamentAsync(Ulid tournamentId);
    Task<List<PedidoLinea>> GetPedidoLineasByPedidoIdsAsync(List<Ulid> pedidoIds);
}
