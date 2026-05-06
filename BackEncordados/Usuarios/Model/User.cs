using BackEncordados.Common.Database.Helpers;

namespace BackEncordados.Usuarios.Model;

/// <summary>
/// Entidad de dominio que representa un usuario.
/// Autenticación con email/password hasheado con BCrypt. Roles: USER, ADMIN.
/// </summary>
public class User : ITimestamped
{
    public const string DEFAULT_IMAGE="/images/users/default.jpg";
    /// <summary>ID único del usuario (PK en PostgreSQL).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Nombre de usuario público (3-50 caracteres, único).</summary>
    public string Username { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;

    /// <summary>Email del usuario (obligatorio, único).</summary>
    public string Email { get; set; } = string.Empty;
    ///
    public string Phone { get; set; } = string.Empty;
    
    /// <summary>Hash BCrypt de la contraseña (60 caracteres aprox).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Rol del usuario (USER o ADMIN).</summary>
    public string Role { get; set; } = UserRoles.USER;

    /// <summary>Indica si el usuario está eliminado (soft-delete).</summary>
    public bool IsDeleted { get; set; }
    
    public string ImageUrl { get; set; } = DEFAULT_IMAGE;

    /// <summary>Fecha de creación en UTC.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Fecha de última modificación en UTC.</summary>
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
        
    }
}