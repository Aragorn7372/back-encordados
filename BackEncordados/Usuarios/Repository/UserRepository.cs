using BackEncordados.Common.Database.Config;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Usuarios.Repository;

/// <summary>
/// Implementación de <see cref="IUserRepository"/> que accede a la entidad <see cref="User"/>
/// a través de <see cref="UserDbContext"/> con Entity Framework Core.
/// </summary>
/// <remarks>
/// <para>Proporciona operaciones CRUD completas sobre usuarios:</para>
/// <list type="table">
///   <listheader>
///     <term>Operación</term>
///     <description>Comportamiento</description>
///   </listheader>
///   <item>
///     <term>Consultas</term>
///     <description>Todas las consultas usan <c>AsNoTracking()</c> para optimizar rendimiento de solo lectura.</description>
///   </item>
///   <item>
///     <term>Soft Delete</term>
///     <description><c>DeleteAsync</c> marca <c>IsDeleted=true</c> y reemplaza el username con un prefijo <c>deleted_</c> para liberar la constraint única.</description>
///   </item>
///   <item>
///     <term>Paginación</term>
///     <description><c>FindAllAsync</c> soporta ordenación por name, username, email, createdAt o id, en dirección asc/desc.</description>
///   </item>
///   <item>
///     <term>Búsqueda</term>
///     <description>Filtro textual aplicado a email, username, name y phone mediante <c>EF.Functions.Like</c>.</description>
///   </item>
/// </list>
/// <para>Los usuarios eliminados lógicamente se excluyen automáticamente de todas las consultas (<c>!u.IsDeleted</c>).</para>
/// </remarks>
public class UserRepository(
    UserDbContext context,
    ILogger<UserRepository> logger
) : IUserRepository
{

    /// <inheritdoc/>
    public async Task<User?> FindByIdAsync(Ulid id)
    {
        return await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <inheritdoc/>
    public async Task<User?> FindByUsernameAsync(string username)
    {
        return await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <inheritdoc/>
    public async Task<User?> FindByEmailAsync(string email)
    {
        return await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<User> Items, int TotalCount)> FindAllAsync(FilterUserDto filter)
    {
        var query = context.Users.AsNoTracking().AsQueryable();
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
             "name" => isDesc ? query.OrderByDescending(u => u.Name).ThenBy(u => u.Id) : query.OrderBy(u => u.Name).ThenBy(u => u.Id),
             "username" => isDesc ? query.OrderByDescending(u => u.Username).ThenBy(u => u.Id) : query.OrderBy(u => u.Username).ThenBy(u => u.Id),
             "email" => isDesc ? query.OrderByDescending(u => u.Email).ThenBy(u => u.Id) : query.OrderBy(u => u.Email).ThenBy(u => u.Id),
             "createdAt" => isDesc ? query.OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id) : query.OrderBy(u => u.CreatedAt).ThenBy(u => u.Id),
             _ => isDesc ? query.OrderByDescending(u => u.Id) : query.OrderBy(u => u.Id)
         };
        var items = await query.Skip(filter.Page * filter.Size).Take(filter.Size).ToListAsync();
        return (items, totalCount);
    }

    /// <summary>
    /// Cambia el rol de un usuario en la base de datos.
    /// </summary>
    /// <remarks>
    /// <para>Usa seguimiento de cambios (sin AsNoTracking) porque necesita persistir la modificación.</para>
    /// <para>Si el usuario ya tiene el rol especificado, retorna <c>false</c> sin realizar cambios.</para>
    /// </remarks>
    /// <param name="id">ULID del usuario.</param>
    /// <param name="role">Nuevo rol a asignar.</param>
    /// <returns><c>true</c> si el rol se actualizó, <c>false</c> si el usuario no existe o ya tenía ese rol.</returns>
    public async Task<bool> UserChageRoleAsync(Ulid id, string role)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is not null)
        {
            if (user.Role == role)
                return false;
            user.Role = role;
            await context.SaveChangesAsync();
            logger.LogInformation("Rol de usuario actualizado con ID: {Id}", user.Id);
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
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
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
        return await context.Users.AsNoTracking()
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Email)
            .ToListAsync();
    }

    /// <summary>
    /// Busca múltiples usuarios por sus ULIDs.
    /// </summary>
    /// <remarks>
    /// <para>Si la lista de IDs está vacía, retorna una colección vacía sin realizar consulta a la BD.</para>
    /// <para>Usa <c>AsNoTracking()</c> para optimizar rendimiento.</para>
    /// </remarks>
    /// <param name="ids">Colección de ULIDs a buscar.</param>
    /// <returns>Colección de usuarios encontrados (los IDs no existentes se omiten silenciosamente).</returns>
    public async Task<IEnumerable<User>> FindByIdsAsync(IEnumerable<Ulid> ids)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return [];
        return await context.Users.AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .ToListAsync();
    }
    
}