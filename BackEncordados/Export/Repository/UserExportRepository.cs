using BackEncordados.Common.Database.Config;
using BackEncordados.Usuarios.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Export.Repository;

public class UserExportRepository(
    UserDbContext userDbContext,
    ILogger<UserExportRepository> logger
) : IUserExportRepository
{
    public async Task<List<User>> GetUsersDataAsync()
    {
        var users = await userDbContext.Users.AsNoTracking().IgnoreQueryFilters().ToListAsync();
        logger.LogInformation("Fetched {Count} users", users.Count);
        return users;
    }

    public async Task ClearUsersAsync()
    {
        if (userDbContext.Database.IsInMemory())
        {
            var users = await userDbContext.Users.IgnoreQueryFilters().ToListAsync();
            userDbContext.Users.RemoveRange(users);
            await userDbContext.SaveChangesAsync();
            logger.LogInformation("Cleared users (in-memory)");
        }
        else
        {
            await userDbContext.Users.ExecuteDeleteAsync();
            logger.LogInformation("Cleared users (production)");
        }
    }

    public async Task ImportUsersAsync(List<User> users)
    {
        if (!users.Any()) return;

        await userDbContext.Users.AddRangeAsync(users);
        await userDbContext.SaveChangesAsync();
        logger.LogInformation("Imported {Count} users", users.Count);
    }
}
