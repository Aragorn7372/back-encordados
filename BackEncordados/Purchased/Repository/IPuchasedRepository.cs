using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;

namespace BackEncordados.Purchased.Repository;

public interface IPuchasedRepository
{
    Task<(IEnumerable<Pedidos> Items, int TotalCount)> FindAllAsync(FilterPurchasedDto filter);
    Task<Pedidos?> FindByIdAsync(Ulid id);
    Task<Pedidos> CreatePurchasedAsync(Pedidos pedidos);
    Task<Pedidos?> UpdatePurchasedAsync(Pedidos pedidos,  Ulid id);
    Task<Pedidos?> CancelPurchasedAsync(Ulid id);
    Task<Pedidos?> ChangeStatusPurchasedAsync(Ulid id, string status);
    Task<Pedidos?> ChangePaymentStatusPurchasedAsync(Ulid id, string payStatus);

}