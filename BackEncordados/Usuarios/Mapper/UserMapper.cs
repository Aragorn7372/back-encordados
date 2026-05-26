using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Model;

namespace BackEncordados.Usuarios.Mapper;

/// <summary>
/// Métodos de extensión para mapear entre la entidad <see cref="User"/> y sus DTOs.
/// </summary>
/// <remarks>
/// <para>Proporciona tres métodos de mapeo:</para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Origen → Destino</description>
///     <description>Uso</description>
///   </listheader>
///   <item>
///     <term><c>ToDto</c></term>
///     <description><see cref="User"/> → <see cref="UserResponseDto"/></description>
///     <description>Respuestas públicas sin ID (listas, perfiles).</description>
///   </item>
///   <item>
///     <term><c>ToDtoWithId</c></term>
///     <description><see cref="User"/> → <see cref="UserWithIdDto"/></description>
///     <description>Respuestas administrativas que requieren el ID.</description>
///   </item>
///   <item>
///     <term><c>ToModel</c></term>
///     <description><see cref="ContactoPostRequestDto"/> → <see cref="User"/></description>
///     <description>Creación de contactos desde formulario público.</description>
///   </item>
/// </list>
/// <para>Todas las rutas de imagen se resuelven mediante <see cref="ICloudinaryService.ResolveImageUrl"/>
/// usando la carpeta <c>CloudinaryConstants.FOLDER_USUARIOS</c>.</para>
/// </remarks>
public static class UserMapper
{
    /// <summary>
    /// Convierte un <see cref="User"/> a <see cref="UserResponseDto"/> resolviendo la URL de imagen.
    /// </summary>
    /// <param name="user">Entidad de usuario a mapear. No debe ser <c>null</c>.</param>
    /// <param name="cloudinary">Servicio de Cloudinary para resolución de URLs de imágenes.</param>
    /// <returns>DTO público con username, imagen, nombre y bonos.</returns>
    public static UserResponseDto ToDto(this User user, ICloudinaryService cloudinary)
    {
        return new UserResponseDto(
            user.Username,
            cloudinary.ResolveImageUrl(user.ImageUrl, CloudinaryConstants.FOLDER_USUARIOS),
            user.Name,
            user.Bonos
            );
    }
    /// <summary>
    /// Convierte un <see cref="User"/> a <see cref="UserWithIdDto"/> incluyendo el identificador.
    /// </summary>
    /// <param name="user">Entidad de usuario a mapear. No debe ser <c>null</c>.</param>
    /// <param name="cloudinary">Servicio de Cloudinary para resolución de URLs de imágenes.</param>
    /// <returns>DTO con ID en string, username, imagen y nombre.</returns>
    public static UserWithIdDto ToDtoWithId(this User user, ICloudinaryService cloudinary)
    {
        return new UserWithIdDto(
            user.Id.ToString(),
            user.Username,
            cloudinary.ResolveImageUrl(user.ImageUrl, CloudinaryConstants.FOLDER_USUARIOS),
            user.Name,
            user.Email,
            user.Role,
            user.TournamentId?.ToString()
            );
    }
    /// <summary>
    /// Convierte un <see cref="ContactoPostRequestDto"/> en una entidad <see cref="User"/> con rol USER.
    /// </summary>
    /// <remarks>
    /// <para>Genera un ULID aleatorio como username, un GUID como email de respaldo si no se proporciona,
    /// y un hash BCrypt con factor de trabajo 11 como contraseña temporal.</para>
    /// <para>El usuario creado tendrá rol <c>USER</c> por defecto.</para>
    /// </remarks>
    /// <param name="request">DTO de solicitud con nombre, email, teléfono y TournamentId.</param>
    /// <returns>Entidad <see cref="User"/> lista para persistir.</returns>
    public static User ToModel(this ContactoPostRequestDto request) {
        return new User {
            Name = request.Name,
            Email = request.Email ?? Guid.NewGuid().ToString(),
            Phone = request.Phone,
            Username = Ulid.NewUlid().ToString(),
            TournamentId = request.TournamentId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), workFactor: 11),
            Role = User.UserRoles.USER
        };
    }
    
}
