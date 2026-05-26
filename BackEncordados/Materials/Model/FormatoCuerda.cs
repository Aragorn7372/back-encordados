namespace BackEncordados.Materials.Model;

/// <summary>
/// Enum que representa el formato o presentación de las cuerdas para la venta.
/// </summary>
/// <remarks>
/// <para>Se usa como propiedad <c>StringFormat</c> en la entidad <see cref="Cuerdas"/>.</para>
/// <para><b>Valores disponibles:</b></para>
/// <list type="bullet">
///   <item><description><c>Reel</c> — Bobina o rollo (larga longitud, múltiples encordados).</description></item>
///   <item><description><c>Set</c> — Juego individual (suficiente para una raqueta).</description></item>
/// </list>
/// </remarks>
public enum FormatoCuerda
{
    /// <summary>Bobina o rollo (200m / 660ft, múltiples encordados).</summary>
    Reel,

    /// <summary>Juego individual (12m / 40ft, suficiente para una raqueta).</summary>
    Set
}