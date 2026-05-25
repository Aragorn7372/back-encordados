namespace BackEncordados.Excel.Dto;

/// <summary>
/// DTO que representa un usuario en la exportación a Excel del módulo de usuarios.
/// </summary>
/// <remarks>
/// <para>Corresponde a la hoja "Usuarios" en el archivo Excel de exportación avanzada.
/// Contiene los datos básicos del perfil de usuario más su asignación a un torneo.</para>
/// <para>Los campos <see cref="Phone"/> y <see cref="TournamentId"/> son opcionales
/// ya que no todos los usuarios registran teléfono ni están asignados a un torneo.</para>
/// </remarks>
public class ExcelUsersDto
{
    /// <summary>Identificador único del usuario (ULID en string).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nombre de usuario para inicio de sesión.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Nombre completo visible del usuario.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Correo electrónico del usuario.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto del usuario. Puede ser nulo si no se registró.</summary>
    public string? Phone { get; set; }

    /// <summary>ID del torneo al que está asignado el usuario. Nulo si no tiene torneo asignado.</summary>
    public string? TournamentId { get; set; }
}