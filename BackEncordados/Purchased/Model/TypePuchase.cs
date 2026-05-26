namespace BackEncordados.Purchased.Model;

/// <summary>
/// Enum que define los tipos de pedido disponibles.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description><c>ENCORDADO</c> — Pedido de encordado de raqueta.</description></item>
///   <item><description><c>ORDEN_DE_TALLER</c> — Orden de taller (otro tipo de servicio).</description></item>
/// </list>
/// <para>Nota: El nombre del tipo contiene un error tipográfico ("Puchase" en lugar de "Purchase")
/// que se mantiene por compatibilidad.</para>
/// </remarks>
public enum TypePuchase
{
    ENCORDADO,
    ORDEN_DE_TALLER
}