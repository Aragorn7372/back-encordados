namespace BackEncordados.Excel.Dto;

/// <summary>
/// DTO que representa un pedido de encordado en la exportación a Excel.
/// </summary>
/// <remarks>
/// <para>Corresponde a la hoja "Pedidos" en el archivo Excel de exportación avanzada.</para>
/// <para>Un pedido agrupa una o más líneas (<see cref="ExcelPedidoLineasDto"/>)
/// para un jugador (<see cref="PlayerId"/>) dentro de un torneo (<see cref="TournamentId"/>).
/// Incluye información de asignación a un trabajador (<see cref="AssignedTo"/>),
/// máquina utilizada (<see cref="Machine"/>), precio total y estado de pago.</para>
/// </remarks>
public class ExcelPedidosDto
{
    /// <summary>Identificador único del pedido (ULID en string).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ID del torneo al que pertenece el pedido (ULID en string).</summary>
    public string TournamentId { get; set; } = string.Empty;

    /// <summary>ID del jugador que realiza el pedido (ULID en string).</summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>Nombre del trabajador asignado para realizar el encordado.</summary>
    public string AssignedTo { get; set; } = string.Empty;

    /// <summary>Nombre o identificador de la máquina de encordar utilizada.</summary>
    public string Machine { get; set; } = string.Empty;

    /// <summary>Comentarios u observaciones adicionales sobre el pedido.</summary>
    public string? Comments { get; set; }

    /// <summary>Precio total del pedido en la moneda local.</summary>
    public double Price { get; set; }

    /// <summary>Estado del pago ("Pagado", "Pendiente", "Parcial", etc.).</summary>
    public string PayStatus { get; set; } = string.Empty;
}