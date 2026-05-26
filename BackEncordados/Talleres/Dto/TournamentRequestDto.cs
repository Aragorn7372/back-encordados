using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Talleres.Dto;

/// <summary>
/// DTO de solicitud para creación de un torneo por parte de un supervisor o usuario estándar.
/// </summary>
/// <remarks>
/// <para>Similar a <see cref="TournamentAdminRequestDto"/> pero sin el campo <see cref="TournamentAdminRequestDto.OwnerId"/>,
/// ya que el propietario se determina automáticamente del usuario autenticado.</para>
/// <para>El nombre debe tener entre 1 y 200 caracteres. Las fechas de inicio y fin son obligatorias.</para>
/// </remarks>
public class TournamentRequestDto {
    /// <summary>Nombre del torneo. Entre 1 y 200 caracteres.</summary>
    [Required]
    [MaxLength(200)]
    [MinLength(1)]
    public string Name { get; set; }=string.Empty;
    /// <summary>Fecha de finalización del torneo.</summary>
    [Required]
    public DateTime EndTournament { get; set; }
    /// <summary>Fecha de inicio del torneo.</summary>
    [Required]
    public DateTime StartTournament { get; set; }
    /// <summary>Archivo de imagen para el logotipo del torneo (opcional).</summary>
    public IFormFile? Logotype { get; set; }
}