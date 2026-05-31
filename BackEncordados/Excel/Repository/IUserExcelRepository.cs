using BackEncordados.Usuarios.Model;

namespace BackEncordados.Excel.Repository;

public interface IUserExcelRepository
{
    Task<Dictionary<Ulid, (string Username, string Name)>> GetUsersByIdsAsync(List<Ulid> ids);
    Task<List<User>> GetUsersByTournamentAsync(Ulid tournamentId);
}
