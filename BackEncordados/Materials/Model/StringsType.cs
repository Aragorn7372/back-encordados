namespace BackEncordados.Materials.Model;

/// <summary>
/// Enum que clasifica los tipos de cuerdas para raquetas según su material y construcción.
/// </summary>
/// <remarks>
/// <para>Se usa como propiedad <c>StringsType</c> en la entidad <see cref="Cuerdas"/>
/// para categorizar el tipo de cordaje.</para>
/// <para><b>Valores disponibles:</b></para>
/// <list type="bullet">
///   <item><description><c>Polyester</c> — Cuerda de poliéster (alta durabilidad, poco confort).</description></item>
///   <item><description><c>Multifilament</c> — Cuerda multifilamento (buen confort, potencia media).</description></item>
///   <item><description><c>SyntheticGut</c> — Cuerda sintética tipo tripa (económica, versátil).</description></item>
///   <item><description><c>NaturalGut</c> — Tripa natural (máximo confort y potencia, alto costo).</description></item>
///   <item><description><c>Hybrid</c> — Encordado híbrido (combinación de dos tipos diferentes).</description></item>
/// </list>
/// </remarks>
public enum StringsType
{
    /// <summary>Cuerda de poliéster: alta durabilidad, bajo confort, control.</summary>
    Polyester,

    /// <summary>Cuerda multifilamento: buen confort, potencia media.</summary>
    Multifilament,

    /// <summary>Cuerda sintética tipo tripa: económica y versátil.</summary>
    SyntheticGut,

    /// <summary>Tripa natural: máximo confort, mejor potencia, alto costo.</summary>
    NaturalGut,

    /// <summary>Encordado híbrido: combina dos tipos de cuerda (ej: poliéster + tripa).</summary>
    Hybrid
}