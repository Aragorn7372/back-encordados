namespace BackEncordados.Purchased.Model;

/// <summary>
/// Enum que define los posibles estados de una línea de pedido (raqueta).
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description><c>PENDING</c> — Pendiente de comenzar.</description></item>
///   <item><description><c>IN_PROGRESS</c> — En proceso de encordado.</description></item>
///   <item><description><c>COMPLETED</c> — Encordado completado.</description></item>
///   <item><description><c>CANCELED</c> — Cancelado.</description></item>
///   <item><description><c>DELIVERED_TOpLAYER</c> — Entregado al jugador.</description></item>
/// </list>
/// </remarks>
public enum Status
{
    PENDING,
    IN_PROGRESS,
    COMPLETED,
    CANCELED,
    DELIVERED_TOpLAYER
}