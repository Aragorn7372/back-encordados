using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Materials.Dto.Strings;

public class CuerdaPatchDto
{
    [MinLength(1)]
    [MaxLength(100)]
    public string Marca { get; set; } = string.Empty;
    [MinLength(1)]
    [MaxLength(100)]
    public string Modelo { get; set; } = string.Empty;
    public int Stock { get; set; } = -1;
    public double Precio { get; set; } = -1;
    [MinLength(1)]
    [MaxLength(100)]
    public string StringFormat { get; set; } = string.Empty;
    [MinLength(1)]
    [MaxLength(100)]
    public string StringsType { get; set; } =string.Empty;
    public IFormFile? Imagen { get; set; }
}