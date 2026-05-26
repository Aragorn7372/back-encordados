using BackEncordados.Common.Database.Config;
using BackEncordados.Usuarios.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Export.Repository;

public class ExportUserRepository(
    UserDbContext userDbContext,
    ILogger<ExportUserRepository> logger
) : IExportUserRepository
{
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await userDbContext.Users.AsNoTracking().IgnoreQueryFilters().ToListAsync();
    }

    public async Task ClearUsersAsync()
    {
        if (userDbContext.Database.IsInMemory())
        {
            var users = await userDbContext.Users.IgnoreQueryFilters().ToListAsync();
            userDbContext.Users.RemoveRange(users);
            await userDbContext.SaveChangesAsync();
        }
        else
        {
            await userDbContext.Users.ExecuteDeleteAsync();
        }
        logger.LogInformation("Cleared users");
    }

    public async Task ImportUsersAsync(List<User> users)
    {
        await userDbContext.Users.AddRangeAsync(users);
        await userDbContext.SaveChangesAsync();
        logger.LogInformation("Imported {Count} users", users.Count);
    }
}
