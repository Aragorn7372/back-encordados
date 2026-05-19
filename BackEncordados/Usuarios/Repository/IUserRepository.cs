using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Model;

namespace BackEncordados.Usuarios.Repository;

/// <summary>
/// Contrato del repositorio de usuarios.
/// </summary>
public interface IUserRepository
{
    /// <summary>Busca un usuario por ID.</summary>
    /// <param name="id">ID del usuario.</param>
    /// <returns>Usuario o null.</returns>
    Task<User?> FindByIdAsync(Ulid id);

    /// <summary>Busca un usuario por username.</summary>
    /// <param name="username">Username a buscar.</param>
    /// <returns>Usuario o null.</returns>
    Task<User?> FindByUsernameAsync(string username);

    /// <summary>Busca un usuario por email.</summary>
    /// <param name="email">Email a buscar.</param>
    /// <returns>Usuario o null.</returns>
    Task<User?> FindByEmailAsync(string email);

    /// <summary>Obtiene todos los usuarios.</summary>
    /// <returns>Colección de usuarios.</returns>
    Task<(IEnumerable<User> Items, int TotalCount)> FindAllAsync(FilterUserDto filter);


    /// <summary>Guarda un nuevo usuario.</summary>
    /// <param name="user">Usuario a guardar.</param>
    /// <returns>Usuario guardado.</returns>
    Task<User> SaveAsync(User user);

    Task<bool> UserChageRoleAsync(Ulid id, string role);
    /// <summary>Actualiza un usuario existente.</summary>
    /// <param name="user">Usuario con datos actualizados.</param>
    /// <returns>Usuario actualizado.</returns>
    Task<User> UpdateAsync(User user);

    /// <summary>Elimina un usuario (soft delete).</summary>
    /// <param name="id">ID del usuario.</param>
    Task DeleteAsync(Ulid id);

    /// <summary>Obtiene solo usuarios activos.</summary>
    /// <returns>Colección de usuarios activos.</returns>
    Task<IEnumerable<User>> GetActiveUsersAsync();

    /// <summary>Busca múltiples usuarios por sus IDs.</summary>
    /// <param name="ids">Colección de IDs.</param>
    /// <returns>Colección de usuarios encontrados.</returns>
    Task<IEnumerable<User>> FindByIdsAsync(IEnumerable<Ulid> ids);
}