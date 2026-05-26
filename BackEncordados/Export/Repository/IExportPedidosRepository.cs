using BackEncordados.Purchased.Model;

namespace BackEncordados.Export.Repository;

public interface IExportPedidosRepository
{
    Task<List<Pedidos>> GetAllPedidosAsync();
    Task ClearPedidosAsync();
    Task ImportPedidosAsync(List<Pedidos> pedidos);
}
