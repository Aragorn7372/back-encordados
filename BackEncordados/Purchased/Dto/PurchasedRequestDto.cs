using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Purchased.Dto;

/// <summary>
/// DTO de solicitud para crear un nuevo pedido de encordado.
/// </summary>
/// <remarks>
/// <para>Un pedido agrupa una o más líneas (<see cref="Lineas"/>) que representan raquetas a encordar
/// para un jugador (<see cref="PlayerName"/>) en un torneo específico (<see cref="TournamentId"/>).</para>
/// <para>Campos obligatorios: TournamentId, PlayerName, AssignedToName, Machine, PayStatus, Price y al menos una línea.</para>
/// <para><b>Validaciones:</b></para>
/// <list type="bullet">
///   <item><description><c>Machine</c>: entre 1 y 100 caracteres.</description></item>
///   <item><description><c>Comments</c>: máximo 500 caracteres (opcional).</description></item>
///   <item><description><c>Price</c>: debe ser un número positivo mayor a 0.01.</description></item>
///   <item><description><c>Lineas</c>: mínimo 1 elemento.</description></item>
/// </list>
/// </remarks>
public class PurchasedRequestDto
{
    /// <summary>ULID del torneo al que pertenece el pedido.</summary>
    [Required(ErrorMessage = "El ID del torneo es obligatorio")]
    public Ulid TournamentId { get; init; }

    /// <summary>Nombre del jugador que realiza el encargo.</summary>
    [Required(ErrorMessage = "El jugador que encarga es obligatorio")]
    public string? PlayerName { get; init; }

    /// <summary>Nombre del encordador asignado al pedido.</summary>
    [Required(ErrorMessage = "El Encordado a cargo es obligatorio")]
    public string? AssignedToName { get; init; }

    /// <summary>Máquina utilizada para el encordado. Entre 1 y 100 caracteres.</summary>
    [Required(ErrorMessage = "La maquina a usar es obligatorio")]
    [MinLength(1, ErrorMessage = "La maquina a usar debe tener entre 1 y 20 caracteres")]
    [MaxLength(100, ErrorMessage = "La maquina a usar debe tener entre 100 caracteres")]
    public string Machine { get; set; } = string.Empty;

    /// <summary>Comentarios adicionales del pedido. Máximo 500 caracteres.</summary>
    [MaxLength(500, ErrorMessage = "Los comentarios deben tener entre 500 caracteres")]
    public string Comments { get; set; } = string.Empty;

    /// <summary>Estado de pago del pedido (ej: Pendiente, Pagado).</summary>
    [Required(ErrorMessage = "El estado de pago es obligatorio")]
    public string PayStatus { get; init; } = string.Empty;

    /// <summary>Precio total del pedido. Debe ser un número positivo.</summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser un número positivo")]
    public double Price { get; set; } 

    /// <summary>Lista de líneas de pedido (raquetas a encordar). Mínimo 1 línea.</summary>
    [Required(ErrorMessage = "Al menos una línea de pedido es obligatoria")]
    [MinLength(1, ErrorMessage = "Debe haber al menos una línea de pedido")]
    public List<PedidoLineaRequestDto> Lineas { get; init; } = new();
}