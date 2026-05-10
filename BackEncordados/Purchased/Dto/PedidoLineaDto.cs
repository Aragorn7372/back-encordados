using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Purchased.Dto;

public class PedidoLineaRequestDto
{
    [Required(ErrorMessage = "El modelo de raqueta es obligatorio")]
    [MinLength(1, ErrorMessage = "El modelo debe tener entre 1 y 200 caracteres")]
    [MaxLength(200, ErrorMessage = "El modelo debe tener entre 1 y 200 caracteres")]
    public string RaquetModel { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de nudos es obligatorio")]
    public byte Nudos { get; init; }

    [Required(ErrorMessage = "La fecha de encordado es obligatoria")]
    public DateTime DateString { get; set; }

    [Required(ErrorMessage = "Debes indicar si lleva logo")]
    public bool Logotype { get; set; }

    [MaxLength(100, ErrorMessage = "El color no puede superar los 100 caracteres")]
    public string Color { get; set; } = string.Empty;

    [Required(ErrorMessage = "La configuración de las cuerdas es obligatoria")]
    public StringSetupDto StringSetup { get; init; } = null!;
}

public class PedidoLineaPatchDto
{
    [MinLength(1, ErrorMessage = "El modelo debe tener entre 1 y 200 caracteres")]
    [MaxLength(200, ErrorMessage = "El modelo debe tener entre 1 y 200 caracteres")]
    public string? RaquetModel { get; set; }

    public byte? Nudos { get; set; }

    public DateTime? DateString { get; set; }

    public bool? Logotype { get; set; }

    [MaxLength(100, ErrorMessage = "El color no puede superar los 100 caracteres")]
    public string? Color { get; set; }

    public string? Status { get; set; }

    public StringSetupDto? StringSetup { get; set; }
}