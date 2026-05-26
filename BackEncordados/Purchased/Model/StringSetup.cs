namespace BackEncordados.Purchased.Model;

/// <summary>
/// Entidad que almacena la configuración de cuerdas para una línea de pedido.
/// </summary>
/// <remarks>
/// <para>Owned entity dentro de <see cref="PedidoLinea"/>. Almacena tipo y tensión de cuerda
/// vertical (obligatorio) y horizontal (opcional), más el porcentaje de pre-estirado.</para>
/// <para>Los valores se almacenan en kg para tensiones y porcentaje (0-20) para pre-estirado.</para>
/// </remarks>
public class StringSetup
{
    /// <summary>Nombre o tipo de cuerda vertical.</summary>
    public string StringV { get; set; } = string.Empty;
    /// <summary>Tensión vertical en kg.</summary>
    public double TensionV { get; set; } 
    /// <summary>Pre-estirado vertical en porcentaje (0-20).</summary>
    public short PreStetchV  { get; set; }
    /// <summary>Nombre o tipo de cuerda horizontal (opcional, vacío si es same-as-vertical).</summary>
    public string StringH { get; set; } = string.Empty;
    /// <summary>Tensión horizontal en kg.</summary>
    public double TensionH { get; set; }
    /// <summary>Pre-estirado horizontal en porcentaje (0-20).</summary>
    public short PreStetchH  { get; set; }
}