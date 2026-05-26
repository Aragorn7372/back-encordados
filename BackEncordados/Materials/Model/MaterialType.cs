namespace BackEncordados.Materials.Model;

/// <summary>
/// Enum que clasifica los tipos de materiales de encordado disponibles en el inventario.
/// </summary>
/// <remarks>
/// <para>Se usa como propiedad <c>Type</c> en la entidad <see cref="Material"/>
/// para categorizar cada producto dentro del inventario.</para>
/// <para><b>Valores disponibles:</b></para>
/// <list type="bullet">
///   <item><description><c>Grip</c> — Grips o empuñaduras para raquetas.</description></item>
///   <item><description><c>Overgrip</c> — Sobregrips (cinta fina sobre el grip base).</description></item>
///   <item><description><c>LeadTape</c> — Cinta de plomo para ajuste de peso/balance.</description></item>
///   <item><description><c>Silicone</c> — Amortiguadores de silicona para vibraciones.</description></item>
///   <item><description><c>Bumper</c> — Protectores de cabeza de raqueta.</description></item>
///   <item><description><c>Otro</c> — Cualquier otro material no categorizado.</description></item>
/// </list>
/// </remarks>
public enum MaterialType
{
    /// <summary>Grip o empuñadura para raquetas.</summary>
    Grip,

    /// <summary>Sobregrip (cinta fina sobre el grip base).</summary>
    Overgrip,

    /// <summary>Cinta de plomo para ajuste de peso y balance.</summary>
    LeadTape,

    /// <summary>Amortiguador de silicona para reducción de vibraciones.</summary>
    Silicone,

    /// <summary>Protector de cabeza (bumper) para raquetas.</summary>
    Bumper,

    /// <summary>Otros materiales no categorizados.</summary>
    Otro
}