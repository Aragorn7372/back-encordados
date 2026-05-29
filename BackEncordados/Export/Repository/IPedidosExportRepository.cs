using BackEncordados.Purchased.Model;

namespace BackEncordados.Export.Repository;

public interface IPedidosExportRepository
{
    Task<List<Pedidos>> GetPedidosDataAsync();
    Task ClearPedidosAsync();
    Task ImportPedidosAsync(List<Pedidos> pedidos);
}
