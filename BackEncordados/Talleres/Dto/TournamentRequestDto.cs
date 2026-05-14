using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Talleres.Dto;

public class TournamentRequestDto {
    [Required]
    [MaxLength(200)]
    [MinLength(1)]
    public string Name { get; set; }=string.Empty;
    [Required]
    public DateTime EndTournament { get; set; }
    [Required]
    public DateTime StartTournament { get; set; }
    public IFormFile? Logotype { get; set; }
}