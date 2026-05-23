using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Materials.Dto.Strings;

public class CuerdaRequestDto {
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
    public int Stock { get; set; } = -1;
    [Required]
    [Range(0.1, double.MaxValue)]
    public double Precio { get; set; } = -1;
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string StringFormat { get; set; } = string.Empty;
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string StringsType { get; set; } =string.Empty;
    public IFormFile? Imagen { get; set; }
}