using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Talleres.Dto;

/// <summary>
/// DTO de solicitud para asignar un supervisor a un torneo.
/// </summary>
/// <remarks>
/// <para>El supervisor se identifica mediante su ULID en formato string.
/// El torneo se identifica mediante su ULID nativo.</para>
/// <para>Ambos campos son obligatorios. El <see cref="SupervisorId"/> debe tener entre 1 y 100 caracteres.</para>
/// </remarks>
public class SupervisorAsignmentRequestDto {
    /// <summary>Identificador ULID del torneo al que se asignará el supervisor.</summary>
    [Required]
    public Ulid TournamentId { get; set; }
    /// <summary>Identificador del supervisor (ULID en formato string). Entre 1 y 100 caracteres.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string SupervisorId { get; set; }= string.Empty;
    
}