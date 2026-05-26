using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Talleres.Dto;

/// <summary>
/// DTO de solicitud para creación de torneo por administrador.
/// </summary>
/// <remarks>
/// <para>Permite crear un torneo especificando el propietario (<see cref="OwnerId"/>),
/// fechas de inicio y fin, nombre y logotipo opcional.</para>
/// <para>El <see cref="OwnerId"/> es el ULID del usuario que actuará como propietario del torneo.</para>
/// </remarks>
public class TournamentAdminRequestDto
{
    /// <summary>Nombre del torneo. Entre 1 y 200 caracteres.</summary>
    [Required]
    [MaxLength(200)]
    [MinLength(1)]
    public string Name { get; set; }=string.Empty;
    /// <summary>ULID del usuario propietario del torneo.</summary>
    [Required]
    public Ulid OwnerId { get; set; }
    /// <summary>Fecha de finalización del torneo.</summary>
    [Required]
    public DateTime EndTournament { get; set; }
    /// <summary>Fecha de inicio del torneo.</summary>
    [Required]
    public DateTime StartTournament { get; set; }
    /// <summary>Archivo de imagen para el logotipo del torneo (opcional).</summary>
    public IFormFile? Logotype { get; set; }
}