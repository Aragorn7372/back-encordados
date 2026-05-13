using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Talleres.Dto;

public class SupervisorAsignmentRequestDto {
    [Required]
    [Range(1, long.MaxValue)]
    public long TournamentId { get; set; }
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string SupervisorId { get; set; }= string.Empty;
    
}