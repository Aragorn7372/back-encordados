namespace BackEncordados.Purchased.Model;

/// <summary>
/// Enum que define los posibles estados de pago de un pedido.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description><c>FINNISH_TOURNAMENT</c> — Pago pendiente hasta finalización del torneo.</description></item>
///   <item><description><c>PENDING_PAYMENT</c> — Pago pendiente.</description></item>
///   <item><description><c>PAID</c> — Pagado.</description></item>
///   <item><description><c>CANCELED</c> — Cancelado.</description></item>
/// </list>
/// </remarks>
public enum PaymentStatus
{
    FINNISH_TOURNAMENT,
    PENDING_PAYMENT,
    PAID,
    CANCELED
}