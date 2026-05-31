using BackEncordados.Materials.Model;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Model;

namespace BackEncordados.Excel.Repository;

public interface IExcelRepository
{
    Task<List<Pedidos>> GetPedidosByTournamentAsync(Ulid tournamentId);
    Task<Dictionary<Ulid, (string Username, string Name)>> GetUsersByIdsAsync(List<Ulid> ids);
    Task<List<User>> GetUsersByTournamentAsync(Ulid tournamentId);
    Task<List<Material>> GetMaterialsByTournamentAsync(Ulid tournamentId);
    Task<List<Cuerdas>> GetCuerdasByTournamentAsync(Ulid tournamentId);
    Task<Tournaments?> GetTournamentByIdAsync(Ulid tournamentId);
    Task<List<PedidoLinea>> GetPedidoLineasByPedidoIdsAsync(List<Ulid> pedidoIds);
    Task<bool> IsUserSupervisorOfTournamentAsync(Ulid userId, Ulid tournamentId);
    Task<bool> IsUserOwnerOfTournamentAsync(Ulid userId, Ulid tournamentId);
}
