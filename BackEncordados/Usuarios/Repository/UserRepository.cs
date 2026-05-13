using BackEncordados.Common.Database.Config;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Usuarios.Repository;

/// <summary>
/// Implementación del repositorio de usuarios.
/// </summary>
public class UserRepository(
    UserDbContext context,
    ILogger<UserRepository> logger
) : IUserRepository
{

    /// <inheritdoc/>
    public async Task<User?> FindByIdAsync(Ulid id)
    {
        return await context.Users.FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task<User?> FindByUsernameAsync(string username)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <inheritdoc/>
    public async Task<User?> FindByEmailAsync(string email)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<User> Items, int TotalCount)> FindAllAsync(FilterUserDto filter)
    {
        var query = context.Users.AsQueryable();
        query = query.Where(u => !u.IsDeleted);
        
        if (filter.TournamentId.HasValue)
            query = query.Where(u => u.TournamentId == filter.TournamentId);
        
        if (filter.FindUsers == true)
            query = query.Where(u => u.Role == User.UserRoles.USER);
        else if (filter.FindEncorders == true)
            query = query.Where(u => u.Role == User.UserRoles.ENCORDER);
        else if (filter.FindSupervisors == true) 
            query = query.Where(u => u.Role == User.UserRoles.SUPERVISOR);
        
        if (filter.Search.Length > 0)
        {
            query = query.Where(u => EF.Functions.Like(u.Email, $"{filter.Search}%") 
            || EF.Functions.Like(u.Username, $"{filter.Search}%")
            || EF.Functions.Like(u.Name, $"{filter.Search}%")
            || EF.Functions.Like(u.Phone, $"{filter.Search}%"));
        }
        
        var totalCount = await query.CountAsync();
        bool isDesc = filter.Direction.ToLower().Equals("desc");
        query = filter.SortBy.ToLower() switch
        {
            "name" => isDesc ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
            "username" => isDesc ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
            "email" => isDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "createdAt" => isDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            _ => isDesc ? query.OrderByDescending(u => u.Id) : query.OrderBy(u => u.Id)
        };
        var items = await query.Skip(filter.Page * filter.Size).Take(filter.Size).ToListAsync();
        return (items, totalCount);
    }

    public async Task<bool> UserChageRoleAsync(Ulid id, string role)
    {
        var user = await FindByIdAsync(id);
        if (user is not null)
        {
            if (user.Role == role)
                return false;
            user.Role = role;
            context.Users.Update(user);
            await context.SaveChangesAsync();
            logger.LogInformation("Usuario creado con ID: {Id}", user.Id);
            return true;
        }
        return false;
    }
    /// <inheritdoc/>
    public async Task<User> SaveAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
        logger.LogInformation("Usuario creado con ID: {Id}", user.Id);
        return user;
    }

    /// <inheritdoc/>
    public async Task<User> UpdateAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
        logger.LogInformation("Usuario actualizado con ID: {Id}", user.Id);
        return user;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Ulid id)
    {
        var user = await FindByIdAsync(id);
        if (user is not null)
        {
            user.IsDeleted = true;
            // Reemplazar username con UUID para liberar el username único
            user.Username = $"deleted_{Ulid.NewUlid().ToString()[..8]}";
            await context.SaveChangesAsync();
            logger.LogInformation("Usuario eliminado con ID: {Id}. Username reemplazado con: {NewUsername}", id, user.Username);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        logger.LogDebug("Obteniendo usuarios activos");
        return await context.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Email)
            .ToListAsync();
    }
    
}