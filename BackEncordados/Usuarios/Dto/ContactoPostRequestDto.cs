using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Usuarios.Dto;

/// <summary>
/// DTO de solicitud para crear un contacto asociado a un torneo.
/// </summary>
/// <remarks>
/// <para>El contacto puede ser una persona de referencia para un torneo específico.</para>
/// <para>Al menos uno de los campos <see cref="Email"/> o <see cref="Phone"/> debe proporcionarse
/// para que el contacto sea válido.</para>
/// </remarks>
public class ContactoPostRequestDto {
    /// <summary>Nombre completo del contacto. Entre 3 y 200 caracteres.</summary>
    [Required]
    [MinLength(3, ErrorMessage = "Name must be at least 3 characters long.")]
    [MaxLength(200, ErrorMessage = "Name must be no more than 50 characters long.")]
    public string Name { get; set; } = string.Empty;
    /// <summary>Correo electrónico del contacto. Opcional si se proporciona <see cref="Phone"/>.</summary>
    public string? Email { get; set; } 
    /// <summary>Teléfono del contacto. Opcional si se proporciona <see cref="Email"/>.</summary>
    public string? Phone { get; set; } 
    /// <summary>Identificador ULID del torneo al que se asocia este contacto.</summary>
    [Required]
    public Ulid TournamentId { get; set; }
}