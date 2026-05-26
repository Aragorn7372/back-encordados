using BackEncordados.Excel.Dto;
using BackEncordados.Purchased.Model;

namespace BackEncordados.Excel.Repository;

public interface IExcelPedidosRepository
{
    Task<List<Pedidos>> GetPedidosByTournamentAsync(Ulid tournamentId);
    Task<(List<ExcelPedidosDto> Pedidos, List<ExcelPedidoLineasDto> Lineas)> GetPedidosWithLineasAsync(Ulid tournamentId);
}
