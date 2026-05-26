using BackEncordados.Common.Database.Config;
using BackEncordados.Excel.Dto;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Excel.Repository;

public class ExcelUserRepository(UserDbContext userDbContext) : IExcelUserRepository
{
    public async Task<Dictionary<Ulid, (string Username, string Name)>> GetUsersDictByIdsAsync(List<Ulid> userIds)
    {
        return await userDbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => (u.Username, u.Name));
    }

    public async Task<List<ExcelUsersDto>> GetUsersByTournamentAsync(Ulid tournamentId)
    {
        var users = await userDbContext.Users
            .AsNoTracking()
            .Where(u => u.TournamentId == tournamentId)
            .ToListAsync();

        return users.Select(u => new ExcelUsersDto
        {
            Id = u.Id.ToString(),
            Username = u.Username,
            Name = u.Name,
            Email = u.Email,
            Phone = u.Phone,
            TournamentId = u.TournamentId?.ToString()
        }).ToList();
    }
}
