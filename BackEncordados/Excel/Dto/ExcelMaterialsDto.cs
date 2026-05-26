namespace BackEncordados.Excel.Dto;

/// <summary>
/// DTO que representa un material de encordado en la exportación a Excel.
/// </summary>
/// <remarks>
/// <para>Corresponde a la hoja "Materiales" en el archivo Excel de exportación avanzada.
/// Los materiales incluyen productos como grips, overgrips, amortiguadores, etc.,
/// diferenciados de las cuerdas (<see cref="ExcelCuerdasDto"/>).</para>
/// <para>El campo <see cref="Type"/> clasifica el material dentro del inventario
/// (ej: "Grip", "Overgrip", "Amortiguador").</para>
/// </remarks>
public class ExcelMaterialsDto
{
    /// <summary>Identificador numérico autoincremental del material.</summary>
    public long Id { get; set; }

    /// <summary>ID del torneo al que pertenece el material (ULID en string).</summary>
    public string TournamentId { get; set; } = string.Empty;

    /// <summary>Marca del material (ej: "Wilson", "Babolat", "Yonex").</summary>
    public string Marca { get; set; } = string.Empty;

    /// <summary>Modelo o nombre específico del material.</summary>
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Cantidad disponible en stock.</summary>
    public int Stock { get; set; }

    /// <summary>Precio unitario del material en la moneda local.</summary>
    public double Precio { get; set; }

    /// <summary>Tipo o categoría del material para clasificación en inventario.</summary>
    public string Type { get; set; } = string.Empty;
}