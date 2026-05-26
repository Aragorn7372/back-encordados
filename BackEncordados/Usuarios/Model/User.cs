using System.ComponentModel.DataAnnotations;
using BackEncordados.Common.Database.Helpers;
using BackEncordados.Common.Service.Cloudinary;

namespace BackEncordados.Usuarios.Model;

/// <summary>
/// Entidad de dominio que representa un usuario.
/// Autenticación con email/password hasheado con BCrypt. Roles: USER, ADMIN.
/// </summary>
public class User : ITimestamped
{
    public Ulid Id { get; set; } = Ulid.NewUlid();

    public string Username { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    
    public string? Phone { get; set; } 
    
    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = UserRoles.USER;
    
    public Ulid? TournamentId { get; set; }
    

    public bool IsDeleted { get; set; }
    
    public string ImageUrl { get; set; }=CloudinaryConstants.DEFAULT_IMAGE_USUARIOS;
    
    public string? CloudinaryPublicId { get; set; }

    public double Bonos { get; set; }

    [ConcurrencyCheck]
    public long Version { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;


    /// <summary>
    /// Constantes para los roles de usuario.
    /// </summary>
    public class UserRoles
    {
        /// <summary>Rol de administrador con acceso total al sistema.</summary>
        public const string ADMIN = "ADMIN";

        /// <summary>Rol de usuario estándar con permisos básicos.</summary>
        public const string USER = "USER";
        
        public const string OWNER = "OWNER";
        
        public const string ENCORDER = "ENCORDER";
        public const string SUPERVISOR = "SUPERVISOR";
    }
}