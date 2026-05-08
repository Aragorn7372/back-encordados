using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Purchased.Dto;

public class PurchasedRequestDto
{
    [Required(ErrorMessage = "El ID del torneo es obligatorio")]
    [Range(1, long.MaxValue, ErrorMessage = "El ID del torneo debe ser un número positivo")]
    public long TournamentId { get; init; }
    [Required(ErrorMessage = "el tipo de trabajo es obligatorio")]
    [MinLength(1,ErrorMessage ="El tipo de trabajo debe tener entre 1 y 100 caracteres"), MaxLength(100, ErrorMessage = "El tipo de trabajo debe tener entre 1 y 100 caracteres")]
    public string TypeString { get; init; }=string.Empty;
    [Required(ErrorMessage = "El tipo de trabajo es obligatorio")]
    public string TypeWork { get; init; } = string.Empty;
    [Required(ErrorMessage = "El dia de finalizacion es obligatorio")]
    public DateTime DateString { get; set; }
    [Required(ErrorMessage = "debes indicar si lleva logo")]
    public bool Logotype { get; set; }
    [Required(ErrorMessage = "El modelo de raqueta es obligatorio")]
    [MinLength(1,ErrorMessage ="El tipo de raqueta debe tener entre 1 y 200 caracteres"), MaxLength(200, ErrorMessage = "El tipo de raqueta debe tener entre 1 y 200 caracteres")]
    public string RaquetModel { get; set; } = string.Empty;
    [Required(ErrorMessage = "El precio es obligatorio")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser un número positivo")]
    public double Price { get; set; }
    [Required(ErrorMessage = "El número de nudos es obligatorio")]
    public byte Nudos { get; init; }
    [Required(ErrorMessage = "El jugador que encarga es obligatorio")]
    public string? PlayerName { get; init; }
    [Required(ErrorMessage = "El Encordado a cargo es obligatorio")]
    public string? AssignedToName { get; init; }
    [Required(ErrorMessage = "La maquina a usar es obligatorio")]
    [MinLength(1,ErrorMessage ="La maquina a usar debe tener entre 1 y 20 caracteres"), MaxLength(100, ErrorMessage = "La maquina a usar debe tener entre 1 y 20 caracteres")]
    public string Machine { get; set; } = string.Empty;
    [MinLength(1,ErrorMessage ="Los comentarios deben tener entre 1 y 500 caracteres"), MaxLength(500, ErrorMessage = "Los comentarios deben tener entre 1 y 500 caracteres")]
    public string Comments { get; set; } = string.Empty;
    [Required(ErrorMessage = "El estado de pago es obligatorio")]
    public string PayStatus { get; init; } = string.Empty;
    [Required(ErrorMessage = "El estado del pedido es obligatorio")]
    public string Status { get; init; } = string.Empty;
    [Required(ErrorMessage = "La configuración de las cuerdas es obligatoria")]
    public StringSetupDto StringSetup { get; init; } = null!;
}
public class StringSetupDto
{
    [Required(ErrorMessage = "El nombre del cordaje vertical es obligatorio")]
    [MinLength(1,ErrorMessage ="El nombre del cordaje vertical debe tener entre 1 y 100 caracteres"), MaxLength(100, ErrorMessage = "El nombre del cordaje vertical debe tener entre 1 y 100 caracteres")]
    public string StringV { get; init; } = string.Empty;
    [Required(ErrorMessage = "La tension es obligatoria")]
    [Range(5, 40, ErrorMessage = "La tensión vertical debe estar entre 5 y 40kg")]
    public double TensionV { get; init; }
    [Required(ErrorMessage = "El pre strench es obligatorio")]
    [Range(0, 20, ErrorMessage = "El pre-estirado no puede ser negativo ni mayor al 20%")]
    public short PreStetchV { get; init; }

    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
    public string StringH { get; init; } = string.Empty;
    [Range(5, 40, ErrorMessage = "La tensión vertical debe estar entre 5 y 40kg")]
    public double TensionH { get; init; }
    [Range(0, 20, ErrorMessage = "El pre-estirado no puede ser negativo ni mayor al 20%")]
    public short PreStetchH { get; init; }

    
}