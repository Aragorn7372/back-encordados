namespace BackEncordados.Excel.Dto;

/// <summary>
/// DTO que representa una línea de pedido (raqueta individual) en la exportación a Excel.
/// </summary>
/// <remarks>
/// <para>Corresponde a la hoja "Líneas de Pedido" en el archivo Excel de exportación avanzada.</para>
/// <para>Cada línea corresponde a una raqueta específica dentro de un pedido
/// (<see cref="ExcelPedidosDto"/>). Incluye las tensiones independientes para
/// el cordaje vertical (V) y horizontal (H), el pre-stretch, el color, el
/// modelo de raqueta y si incluye logotipo personalizado.</para>
/// <para><b>Convención de nomenclatura:</b></para>
/// <list type="bullet">
///   <item><description>Sufijo <c>V</c>: cordaje vertical (main strings).</description></item>
///   <item><description>Sufijo <c>H</c>: cordaje horizontal (cross strings).</description></item>
/// </list>
/// </remarks>
public class ExcelPedidoLineasDto
{
    /// <summary>Identificador único de la línea de pedido (ULID en string).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ID del pedido al que pertenece esta línea (ULID en string).</summary>
    public string PedidoId { get; set; } = string.Empty;

    /// <summary>Modelo de la raqueta a encordar.</summary>
    public string RaquetModel { get; set; } = string.Empty;

    /// <summary>Número de nudos del encordado (generalmente 2).</summary>
    public byte Nudos { get; set; }

    /// <summary>Fecha programada para el encordado.</summary>
    public DateTime DateString { get; set; }

    /// <summary>Indica si la raqueta lleva logotipo personalizado.</summary>
    public bool Logotype { get; set; }

    /// <summary>Color del cordaje seleccionado.</summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>Modelo/nombre del cordaje vertical (main).</summary>
    public string StringV { get; set; } = string.Empty;

    /// <summary>Tensión del cordaje vertical en kilogramos o libras.</summary>
    public double TensionV { get; set; }

    /// <summary>Pre-stretch del cordaje vertical (estiramiento previo).</summary>
    public short PreStetchV { get; set; }

    /// <summary>Modelo/nombre del cordaje horizontal (cross).</summary>
    public string StringH { get; set; } = string.Empty;

    /// <summary>Tensión del cordaje horizontal en kilogramos o libras.</summary>
    public double TensionH { get; set; }

    /// <summary>Pre-stretch del cordaje horizontal (estiramiento previo).</summary>
    public short PreStetchH { get; set; }

    /// <summary>Estado actual de la línea de pedido ("Pendiente", "En proceso", "Completado", etc.).</summary>
    public string Status { get; set; } = string.Empty;
}