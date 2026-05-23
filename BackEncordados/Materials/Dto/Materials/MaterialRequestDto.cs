using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Materials.Dto.Materials;

public class MaterialRequestDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Marca { get; set; } = string.Empty;
    [Required]
    public Ulid TournamentId { get; set; }
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Modelo { get; set; } = string.Empty;
    [Required]
    [Range(0, int.MaxValue)]
    public int Stock { get; set; }
    [Required]
    [Range(0.1, double.MaxValue)]
    public double Precio { get; set; }
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;
    public IFormFile? Imagen { get; set; }
}