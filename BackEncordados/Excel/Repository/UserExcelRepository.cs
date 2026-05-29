using BackEncordados.Common.Database.Config;
using BackEncordados.Usuarios.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Excel.Repository;

public class UserExcelRepository(
    UserDbContext userDbContext,
    ILogger<UserExcelRepository> logger
) : IUserExcelRepository
{
    public async Task<Dictionary<Ulid, (string Username, string Name)>> GetUsersByIdsAsync(List<Ulid> ids)
    {
        if (ids.Count == 0)
            return [];

        var users = await userDbContext.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Username, u.Name })
            .ToListAsync();

        var dict = users.ToDictionary(u => u.Id, u => (u.Username, u.Name));

        logger.LogInformation("Fetched {Count} users by {TotalIds} ids", dict.Count, ids.Count);
        return dict;
    }

    public async Task<List<User>> GetUsersByTournamentAsync(Ulid tournamentId)
    {
        var users = await userDbContext.Users
            .AsNoTracking()
            .Where(u => u.TournamentId == tournamentId)
            .ToListAsync();

        logger.LogInformation("Fetched {Count} users for tournament {TournamentId}", users.Count, tournamentId);
        return users;
    }
}
