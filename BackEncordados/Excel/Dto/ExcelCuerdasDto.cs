namespace BackEncordados.Excel.Dto;

/// <summary>
/// DTO que representa una cuerda de encordado en la exportación a Excel.
/// </summary>
/// <remarks>
/// <para>Corresponde a la hoja "Cuerdas" en el archivo Excel de exportación avanzada.
/// A diferencia de <see cref="ExcelMaterialsDto"/>, las cuerdas tienen propiedades
/// técnicas adicionales: calibre, formato (bobina/juego) y tipo (tripa/poliéster/híbrida).</para>
/// <para><b>Propiedades técnicas:</b></para>
/// <list type="bullet">
///   <item><description><see cref="Calibre"/>: grosor de la cuerda en milímetros (ej: 1.25, 1.30).</description></item>
///   <item><description><see cref="StringFormat"/>: formato de venta ("Bobina" o "Juego").</description></item>
///   <item><description><see cref="StringsType"/>: tipo de material ("Tripa", "Poliester", "Hibrida", "Nylon").</description></item>
/// </list>
/// </remarks>
public class ExcelCuerdasDto
{
    /// <summary>Identificador numérico autoincremental de la cuerda.</summary>
    public long Id { get; set; }

    /// <summary>ID del torneo al que pertenece la cuerda (ULID en string).</summary>
    public string TournamentId { get; set; } = string.Empty;

    /// <summary>Marca de la cuerda (ej: "Luxilon", "Babolat", "Wilson").</summary>
    public string Marca { get; set; } = string.Empty;

    /// <summary>Modelo o nombre específico de la cuerda.</summary>
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Cantidad disponible en stock.</summary>
    public int Stock { get; set; }

    /// <summary>Precio unitario de la cuerda en la moneda local.</summary>
    public double Precio { get; set; }

    /// <summary>Calibre o grosor de la cuerda en milímetros (ej: 1.25).</summary>
    public double Calibre { get; set; }

    /// <summary>Formato de la cuerda: "Bobina" (rollo) o "Juego" (tramo individual).</summary>
    public string StringFormat { get; set; } = string.Empty;

    /// <summary>Tipo de cuerda: "Tripa", "Poliester", "Hibrida", "Nylon", etc.</summary>
    public string StringsType { get; set; } = string.Empty;
}