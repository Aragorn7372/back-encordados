using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Purchased.Dto;

public class PurchasedRequestDto
{
    [Required(ErrorMessage = "El ID del torneo es obligatorio")]
    [Range(1, long.MaxValue, ErrorMessage = "El ID del torneo debe ser un número positivo")]
    public long TournamentId { get; init; }

    [Required(ErrorMessage = "El jugador que encarga es obligatorio")]
    public string? PlayerName { get; init; }

    [Required(ErrorMessage = "El Encordado a cargo es obligatorio")]
    public string? AssignedToName { get; init; }

    [Required(ErrorMessage = "La maquina a usar es obligatorio")]
    [MinLength(1, ErrorMessage = "La maquina a usar debe tener entre 1 y 20 caracteres")]
    [MaxLength(100, ErrorMessage = "La maquina a usar debe tener entre 100 caracteres")]
    public string Machine { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Los comentarios deben tener entre 500 caracteres")]
    public string Comments { get; set; } = string.Empty;

    [Required(ErrorMessage = "El estado de pago es obligatorio")]
    public string PayStatus { get; init; } = string.Empty;

    [Required(ErrorMessage = "Al menos una línea de pedido es obligatoria")]
    [MinLength(1, ErrorMessage = "Debe haber al menos una línea de pedido")]
    public List<PedidoLineaRequestDto> Lineas { get; init; } = new();
}