using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;

namespace BackEncordados.Purchased.Repository;

public interface IPuchasedRepository
{
    Task<(IEnumerable<Pedidos> Items, int TotalCount)> FindAllAsync(FilterPurchasedDto filter);
    Task<Pedidos?> FindByIdAsync(Guid id);
    Task<Pedidos> CreatePurchasedAsync(Pedidos pedidos);
    Task<Pedidos?> UpdatePurchasedAsync(Pedidos pedidos,  Guid id);
    Task<Pedidos?> CancelPurchasedAsync(Guid id);
    Task<Pedidos?> ChangeStatusPurchasedAsync(Guid id, string? status);
    Task<Pedidos?> ChangePaymentStatusPurchasedAsync(Guid id, string? payStatus);
    
}