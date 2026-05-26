using BackEncordados.Common.Database.Helpers;

namespace BackEncordados.Purchased.Model;

/// <summary>
/// Entidad que representa un pedido de encordado agrupando una o más líneas (raquetas).
/// </summary>
/// <remarks>
/// <para>Un <see cref="Pedidos"/> asocia un jugador (<see cref="PlayerId"/>) con un encordador (<see cref="AssignedTo"/>)
/// dentro de un torneo (<see cref="TournamentId"/>), especificando máquina, comentarios, precio y estado de pago.</para>
/// <para>La colección <see cref="Lineas"/> contiene las raquetas individuales a encordar.</para>
/// <para>Implementa <see cref="ITimestamped"/> para gestión automática de <see cref="CreatedAt"/> y <see cref="UpdatedAt"/>.</para>
/// <para>El estado de pago inicial es <see cref="PaymentStatus.PENDING_PAYMENT"/>.</para>
/// </remarks>
public class Pedidos: ITimestamped
{
    /// <summary>Identificador ULID del pedido.</summary>
    public Ulid Id { get; set; }
    /// <summary>ULID del torneo al que pertenece el pedido.</summary>
    public Ulid TournamentId { get; set; }
    /// <summary>ULID del jugador que realiza el encargo.</summary>
    public Ulid PlayerId { get; set; }
    /// <summary>ULID del encordador asignado.</summary>
    public Ulid AssignedTo { get; set; }
    /// <summary>Máquina utilizada para el encordado.</summary>
    public string Machine { get; set; } = string.Empty;
    /// <summary>Comentarios adicionales del pedido.</summary>
    public string Comments { get; set; } = string.Empty;
    /// <summary>Precio total del pedido.</summary>
    public double Price { get; set; }
    /// <summary>Estado de pago actual. Valor por defecto: <see cref="PaymentStatus.PENDING_PAYMENT"/>.</summary>
    public PaymentStatus PayStatus { get; set; } = PaymentStatus.PENDING_PAYMENT;
    /// <summary>Fecha de creación (UTC).</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    /// <summary>Fecha de última actualización (UTC).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Colección de líneas de pedido (raquetas a encordar).</summary>
    public ICollection<PedidoLinea> Lineas { get; set; } = new List<PedidoLinea>();
}