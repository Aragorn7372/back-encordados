using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Usuarios.Dto;

public class ContactoPostRequestDto {
    [Required]
    [MinLength(3, ErrorMessage = "Name must be at least 3 characters long.")]
    [MaxLength(200, ErrorMessage = "Name must be no more than 50 characters long.")]
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; } 
    public string? Phone { get; set; } 
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "TournamentId must be a positive number.")]
    public long TournamentId { get; set; }
}