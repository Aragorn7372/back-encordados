using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using CSharpFunctionalExtensions;

namespace BackEncordados.Usuarios.Service.CrudService;

/// <summary>
/// Interfaz del servicio CRUD de usuarios que define las operaciones de negocio
/// sobre la entidad <see cref="User"/>.
/// </summary>
/// <remarks>
/// <para>Define 8 métodos que cubren las operaciones fundamentales:</para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Propósito</description>
///     <description>Returns</description>
///   </listheader>
///   <item>
///     <term><c>FindByIdAsync</c></term>
///     <description>Obtiene un usuario por su ULID.</description>
///     <description><c>Result&lt;UserResponseDto, AuthError&gt;</c></description>
///   </item>
///   <item>
///     <term><c>DeleteUserAsync</c></term>
///     <description>Elimina un usuario y su avatar en Cloudinary.</description>
///     <description><c>Result&lt;Unit, AuthError&gt;</c></description>
///   </item>
///   <item>
///     <term><c>GiveRoleToUserAsync</c></term>
///     <description>Cambia el rol de un usuario.</description>
///     <description><c>Result&lt;bool, AuthError&gt;</c></description>
///   </item>
///   <item>
///     <term><c>GetAllUsersAsync</c></term>
///     <description>Consulta paginada con filtros.</description>
///     <description><c>PageResponseDto&lt;UserWithIdDto&gt;</c></description>
///   </item>
///   <item>
///     <term><c>PatchUserAsync</c></term>
///     <description>Actualización parcial de datos del usuario.</description>
///     <description><c>Result&lt;UserResponseDto, AuthError&gt;</c></description>
///   </item>
///   <item>
///     <term><c>CreateContacto</c></term>
///     <description>Crea un contacto visitante asociado a un torneo.</description>
///     <description><c>Result&lt;UserResponseDto, AuthError&gt;</c></description>
///   </item>
///   <item>
///     <term><c>CreateEncoderAsync</c></term>
///     <description>Promociona un usuario al rol ENCORDER.</description>
///     <description><c>Result&lt;Unit, AuthError&gt;</c></description>
///   </item>
///   <item>
///     <term><c>AddBonosAsync</c></term>
///     <description>Incrementa el saldo de bonos de un usuario.</description>
///     <description><c>Result&lt;UserResponseDto, AuthError&gt;</c></description>
///   </item>
/// </list>
/// <para>Todos los métodos que pueden fallar retornan <c>Result&lt;T, AuthError&gt;</c>
/// siguiendo el patrón de errores tipados de CSharpFunctionalExtensions.</para>
/// </remarks>
public interface IUserService
{
    /// <summary>Obtiene un usuario por su identificador ULID.</summary>
    /// <param name="id">Identificador ULID del usuario.</param>
    /// <returns>DTO público del usuario o error <see cref="UserNotFoundError"/> si no existe.</returns>
    Task<Result<UserResponseDto, AuthError>> FindByIdAsync(Ulid id);
    /// <summary>Elimina un usuario y su imagen de avatar de Cloudinary (si no es la imagen por defecto).</summary>
    /// <param name="id">Identificador ULID del usuario a eliminar.</param>
    /// <returns>Unit en éxito o error si no se encuentra el usuario.</returns>
    Task<Result<Unit, AuthError>> DeleteUserAsync(Ulid id);
    /// <summary>Asigna un nuevo rol a un usuario.</summary>
    /// <param name="id">Identificador ULID del usuario.</param>
    /// <param name="role">Nombre del rol (ADMIN, USER, OWNER, ENCORDER).</param>
    /// <returns><c>true</c> si el cambio fue exitoso, o error si el usuario ya tiene ese rol.</returns>
    Task<Result<bool,AuthError>> GiveRoleToUserAsync(Ulid id, string role);
    /// <summary>Obtiene una lista paginada de usuarios aplicando los filtros especificados.</summary>
    /// <param name="filter">DTO con filtros por tipo de usuario, torneo, búsqueda y paginación.</param>
    /// <returns>Página con DTOs que incluyen ID, username, imagen y nombre.</returns>
    Task<PageResponseDto<UserWithIdDto>> GetAllUsersAsync(FilterUserDto filter);
    /// <summary>Actualiza parcialmente los datos de un usuario (nombre, email, teléfono, username, avatar).</summary>
    /// <param name="id">Identificador ULID del usuario.</param>
    /// <param name="request">DTO con campos opcionales a actualizar.</param>
    /// <returns>DTO actualizado del usuario o error si no se encuentra.</returns>
    Task<Result<UserResponseDto, AuthError>> PatchUserAsync(Ulid id, UserRequestDto request);
    /// <summary>Crea un nuevo contacto visitante en el sistema asociado a un torneo.</summary>
    /// <param name="request">DTO con nombre, email/teléfono y TournamentId.</param>
    /// <returns>DTO del contacto creado o error de conflicto.</returns>
    Task<Result<UserResponseDto, AuthError>> CreateContacto(ContactoPostRequestDto request);
    /// <summary>Promociona un usuario existente al rol de Encordador (ENCORDER).</summary>
    /// <param name="userId">Identificador ULID del usuario a promocionar.</param>
    /// <returns>Unit en éxito, o error si el usuario no existe o ya es ENCORDER.</returns>
    Task<Result<Unit, AuthError>> CreateEncoderAsync(Ulid userId);
    /// <summary>Añade bonos al saldo de un usuario.</summary>
    /// <param name="userId">Identificador ULID del usuario.</param>
    /// <param name="cantidad">Cantidad positiva de bonos a añadir.</param>
    /// <returns>DTO actualizado del usuario con el nuevo saldo de bonos, o error si la cantidad es menor o igual a 0.</returns>
    Task<Result<UserResponseDto, AuthError>> AddBonosAsync(Ulid userId, double cantidad);
}