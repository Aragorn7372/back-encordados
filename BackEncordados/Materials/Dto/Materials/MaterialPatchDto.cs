using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Materials.Dto.Materials;

public class MaterialPatchDto
{
    [MinLength(1)]
    [MaxLength(100)]
    public string Marca { get; set; } = string.Empty;
    [MinLength(1)]
    [MaxLength(100)]
    public string Modelo { get; set; } = string.Empty;
    public int Stock { get; set; }
    public double Precio { get; set; }
    [MinLength(1)]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;
    public IFormFile? Imagen { get; set; }
}