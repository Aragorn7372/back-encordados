using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;

namespace BackEncordados.Purchased.Repository;

public interface IPuchasedRepository
{
    Task<(IEnumerable<Pedidos> Items, int TotalCount)> FindAllAsync(FilterPurchasedDto filter);
    Task<Pedidos?> FindByIdAsync(Ulid id);
    Task<Pedidos> CreatePurchasedAsync(Pedidos pedidos);
    Task<Pedidos?> UpdatePurchasedAsync(Pedidos pedidos, Ulid id);
    Task<Pedidos?> CancelPurchasedAsync(Ulid id);
    Task<Pedidos?> ChangeStatusPurchasedAsync(Ulid id, string payStatus);

    Task<PedidoLinea?> FindLineaByIdAsync(Ulid lineaId);
    Task<PedidoLinea> CreateLineaAsync(PedidoLinea linea);
    Task<PedidoLinea?> UpdateLineaAsync(PedidoLinea linea, Ulid lineaId);
    Task<PedidoLinea?> ChangeLineaStatusAsync(Ulid lineaId, Status status);
    Task SaveChangesAsync();
}