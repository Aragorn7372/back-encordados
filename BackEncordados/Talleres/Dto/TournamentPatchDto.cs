using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Talleres.Dto;

public class TournamentPatchDto
{
    [MinLength(1)]
    [MaxLength(200)]
    public string? Name { get; set; }
    public DateTime? EndTournament { get; set; }
    public DateTime? StartTournament { get; set; }
     public IFormFile? Logotype { get; set; }
}