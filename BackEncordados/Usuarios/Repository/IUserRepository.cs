using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Model;

namespace BackEncordados.Usuarios.Repository;

/// <summary>
/// Contrato del repositorio de usuarios que define las operaciones de acceso a datos
/// sobre la entidad <see cref="User"/>.
/// </summary>
/// <remarks>
/// <para>Define 9 métodos de consulta y persistencia:</para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Propósito</description>
///   </listheader>
///   <item><term><c>FindByIdAsync</c></term><description>Busca un usuario por su ULID.</description></item>
///   <item><term><c>FindByUsernameAsync</c></term><description>Busca un usuario por su nombre de usuario.</description></item>
///   <item><term><c>FindByEmailAsync</c></term><description>Busca un usuario por su email.</description></item>
///   <item><term><c>FindAllAsync</c></term><description>Consulta paginada con filtros.</description></item>
///   <item><term><c>SaveAsync</c></term><description>Persiste un nuevo usuario.</description></item>
///   <item><term><c>UserChageRoleAsync</c></term><description>Cambia el rol de un usuario.</description></item>
///   <item><term><c>UpdateAsync</c></term><description>Actualiza un usuario existente.</description></item>
///   <item><term><c>DeleteAsync</c></term><description>Eliminación lógica (soft delete) de un usuario.</description></item>
///   <item><term><c>GetActiveUsersAsync</c></term><description>Obtiene todos los usuarios activos.</description></item>
///   <item><term><c>FindByIdsAsync</c></term><description>Busca múltiples usuarios por su lista de ULIDs.</description></item>
/// </list>
/// </remarks>
public interface IUserRepository
{
    /// <summary>Busca un usuario por su identificador ULID.</summary>
    /// <param name="id">ULID del usuario.</param>
    /// <returns>Usuario encontrado o <c>null</c> si no existe.</returns>
    Task<User?> FindByIdAsync(Ulid id);

    /// <summary>Busca un usuario por su nombre de usuario.</summary>
    /// <param name="username">Nombre de usuario a buscar.</param>
    /// <returns>Usuario encontrado o <c>null</c> si no existe.</returns>
    Task<User?> FindByUsernameAsync(string username);

    /// <summary>Busca un usuario por su dirección de email.</summary>
    /// <param name="email">Email a buscar.</param>
    /// <returns>Usuario encontrado o <c>null</c> si no existe.</returns>
    Task<User?> FindByEmailAsync(string email);

    /// <summary>Obtiene una colección paginada de usuarios aplicando filtros opcionales.</summary>
    /// <param name="filter">DTO con filtros de búsqueda, tipo de usuario y paginación.</param>
    /// <returns>Tupla con la lista de usuarios (<c>Items</c>) y el conteo total (<c>TotalCount</c>).</returns>
    Task<(IEnumerable<User> Items, int TotalCount)> FindAllAsync(FilterUserDto filter);

    /// <summary>Persiste un nuevo usuario en la base de datos.</summary>
    /// <param name="user">Entidad <see cref="User"/> a guardar.</param>
    /// <returns>Usuario guardado con su ULID generado.</returns>
    Task<User> SaveAsync(User user);

    /// <summary>Cambia el rol de un usuario en la base de datos.</summary>
    /// <param name="id">ULID del usuario.</param>
    /// <param name="role">Nuevo rol a asignar.</param>
    /// <returns><c>true</c> si el cambio fue exitoso, <c>false</c> si el usuario no existe o ya tenía ese rol.</returns>
    Task<bool> UserChageRoleAsync(Ulid id, string role);

    /// <summary>Actualiza un usuario existente en la base de datos.</summary>
    /// <param name="user">Entidad <see cref="User"/> con los datos actualizados.</param>
    /// <returns>Usuario actualizado.</returns>
    Task<User> UpdateAsync(User user);

    /// <summary>Elimina un usuario mediante soft delete (marca <c>IsDeleted=true</c> y reemplaza su username).</summary>
    /// <param name="id">ULID del usuario a eliminar.</param>
    Task DeleteAsync(Ulid id);

    /// <summary>Obtiene todos los usuarios activos no eliminados, ordenados por email.</summary>
    /// <returns>Colección de usuarios activos.</returns>
    Task<IEnumerable<User>> GetActiveUsersAsync();

    /// <summary>Busca múltiples usuarios por su lista de ULIDs.</summary>
    /// <param name="ids">Colección de ULIDs a buscar.</param>
    /// <returns>Colección de usuarios encontrados (los IDs no existentes se omiten).</returns>
    Task<IEnumerable<User>> FindByIdsAsync(IEnumerable<Ulid> ids);
}