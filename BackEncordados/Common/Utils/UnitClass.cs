namespace BackEncordados.Common.Utils;

/// <summary>
/// Tipo funcional que representa la ausencia de un valor (unidad).
/// </summary>
/// <remarks>
/// <para>Análogo a <c>void</c> pero como tipo de primera clase. Utilizado en programación funcional
/// para representar operaciones que no producen un valor significativo, permitiendo
/// su uso en genéricos donde <c>void</c> no es válido (ej: <c>Task&lt;Unit&gt;</c>, <c>Result&lt;Unit, Error&gt;</c>).</para>
/// <para>Inspirado en el tipo <c>unit</c> de F# y Haskell.</para>
/// </remarks>
public readonly struct Unit
{
    /// <summary>Instancia singleton de <see cref="Unit"/>.</summary>
    public static readonly Unit Value = new();
}