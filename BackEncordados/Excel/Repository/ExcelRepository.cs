using BackEncordados.Materials.Model;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Model;

namespace BackEncordados.Excel.Repository;

public class ExcelRepository(
    IPedidosExcelRepository pedidosRepo,
    IUserExcelRepository userRepo,
    ITalleresExcelRepository talleresRepo,
    IMaterialsExcelRepository materialsRepo,
    ILogger<ExcelRepository> logger
) : IExcelRepository
{
    public async Task<List<Pedidos>> GetPedidosByTournamentAsync(Ulid tournamentId)
    {
        logger.LogInformation("Fetching pedidos for tournament {TournamentId}", tournamentId);
        return await pedidosRepo.GetPedidosByTournamentAsync(tournamentId);
    }

    public async Task<Dictionary<Ulid, (string Username, string Name)>> GetUsersByIdsAsync(List<Ulid> ids)
    {
        logger.LogInformation("Fetching {Count} users by ids", ids.Count);
        return await userRepo.GetUsersByIdsAsync(ids);
    }

    public async Task<List<User>> GetUsersByTournamentAsync(Ulid tournamentId)
    {
        logger.LogInformation("Fetching users for tournament {TournamentId}", tournamentId);
        return await userRepo.GetUsersByTournamentAsync(tournamentId);
    }

    public async Task<List<Material>> GetMaterialsByTournamentAsync(Ulid tournamentId)
    {
        logger.LogInformation("Fetching materials for tournament {TournamentId}", tournamentId);
        return await materialsRepo.GetMaterialsByTournamentAsync(tournamentId);
    }

    public async Task<List<Cuerdas>> GetCuerdasByTournamentAsync(Ulid tournamentId)
    {
        logger.LogInformation("Fetching cuerdas for tournament {TournamentId}", tournamentId);
        return await materialsRepo.GetCuerdasByTournamentAsync(tournamentId);
    }

    public async Task<Tournaments?> GetTournamentByIdAsync(Ulid tournamentId)
    {
        logger.LogInformation("Fetching tournament {TournamentId}", tournamentId);
        return await talleresRepo.GetTournamentByIdAsync(tournamentId);
    }

    public async Task<List<PedidoLinea>> GetPedidoLineasByPedidoIdsAsync(List<Ulid> pedidoIds)
    {
        logger.LogInformation("Fetching lineas for {Count} pedidos", pedidoIds.Count);
        return await pedidosRepo.GetPedidoLineasByPedidoIdsAsync(pedidoIds);
    }

    public async Task<bool> IsUserSupervisorOfTournamentAsync(Ulid userId, Ulid tournamentId)
    {
        return await talleresRepo.IsUserSupervisorOfTournamentAsync(userId, tournamentId);
    }

    public async Task<bool> IsUserOwnerOfTournamentAsync(Ulid userId, Ulid tournamentId)
    {
        return await talleresRepo.IsUserOwnerOfTournamentAsync(userId, tournamentId);
    }
}
