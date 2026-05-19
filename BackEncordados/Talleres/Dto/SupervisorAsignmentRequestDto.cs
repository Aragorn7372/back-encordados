using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Talleres.Dto;

public class SupervisorAsignmentRequestDto {
    [Required]
    public Ulid TournamentId { get; set; }
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string SupervisorId { get; set; }= string.Empty;
    
}