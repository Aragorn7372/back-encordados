using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Purchased.Dto;

public class StringSetupDto
{
    [Required(ErrorMessage = "El nombre del cordaje vertical es obligatorio")]
    [MinLength(1, ErrorMessage = "El nombre del cordaje vertical debe tener entre 1 y 100 caracteres")]
    [MaxLength(100, ErrorMessage = "El nombre del cordaje vertical no puede superar los 100 caracteres")]
    public string StringV { get; init; } = string.Empty;

    [Required(ErrorMessage = "La tension es obligatoria")]
    [Range(5, 40, ErrorMessage = "La tensión vertical debe estar entre 5 y 40kg")]
    public double TensionV { get; init; }

    [Required(ErrorMessage = "El pre strench es obligatorio")]
    [Range(0, 20, ErrorMessage = "El pre-estirado no puede ser negativo ni mayor al 20%")]
    public short PreStetchV { get; init; }

    [MaxLength(100, ErrorMessage = "El nombre horizontal no puede superar los 100 caracteres")]
    public string StringH { get; init; } = string.Empty;

    [Range(5, 40, ErrorMessage = "La tensión horizontal debe estar entre 5 y 40kg")]
    public double TensionH { get; init; }

    [Range(0, 20, ErrorMessage = "El pre-estirado horizontal debe estar entre 0 y 20%")]
    public short PreStetchH { get; init; }
}