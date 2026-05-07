using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Purchased.Dto;

public record PurchasedPatchDto
{
    [MinLength(1,ErrorMessage ="El tipo de trabajo debe tener entre 1 y 100 caracteres"), MaxLength(100, ErrorMessage = "El tipo de trabajo debe tener entre 1 y 100 caracteres")]
    public string? TypeString { get; init; }
    public string? TypeWork { get; init; } 
    public DateTime? DateString { get; init; }
    public bool? Logotype { get; init; }
    [MinLength(1,ErrorMessage ="El tipo de raqueta debe tener entre 1 y 200 caracteres"), MaxLength(200, ErrorMessage = "El tipo de raqueta debe tener entre 1 y 200 caracteres")]
    public string? RaquetModel { get; init; }
    public double? Price { get; init; }
    public byte? Nudos { get; init; }
    [MinLength(1,ErrorMessage ="La maquina a usar debe tener entre 1 y 20 caracteres"), MaxLength(100, ErrorMessage = "La maquina a usar debe tener entre 1 y 20 caracteres")]
    public string? Machine { get; init; }
    [MinLength(1,ErrorMessage ="Los comentarios deben tener entre 1 y 500 caracteres"), MaxLength(500, ErrorMessage = "Los comentarios deben tener entre 1 y 500 caracteres")]
    public string? Comments { get; init; }
    public StringSetupDto? StringSetup { get; init; }
}
public record StringSetupPatchDto
{
    public string? StringV { get; init; }
    public double? TensionV { get; init; }
    public short? PreStetchV { get; init; }
    public string? StringH { get; init; }
    public double? TensionH { get; init; }
    public short? PreStetchH { get; init; }
}