using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Talleres.Dto;

/// <summary>
/// DTO de solicitud para actualización parcial de un torneo.
/// </summary>
/// <remarks>
/// <para>Todos los campos son opcionales, permitiendo actualizaciones parciales (PATCH).</para>
/// <para>Si <see cref="Name"/> se proporciona, debe tener entre 1 y 200 caracteres.</para>
/// </remarks>
public class TournamentPatchDto
{
    /// <summary>Nuevo nombre del torneo (opcional). Entre 1 y 200 caracteres si se proporciona.</summary>
    [MinLength(1)]
    [MaxLength(200)]
    public string? Name { get; set; }
    /// <summary>Nueva fecha de finalización del torneo (opcional).</summary>
    public DateTime? EndTournament { get; set; }
    /// <summary>Nueva fecha de inicio del torneo (opcional).</summary>
    public DateTime? StartTournament { get; set; }
    /// <summary>Nuevo logotipo del torneo (opcional).</summary>
    public IFormFile? Logotype { get; set; }
}