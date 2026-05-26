namespace BackEncordados.Excel.Dto;

/// <summary>
/// DTO que representa una fila del resumen simple de torneo en Excel.
/// </summary>
/// <remarks>
/// <para>Se utiliza en el endpoint <c>GET api/excel/export/{tournamentId}</c>
/// para generar el reporte básico de un torneo. Cada fila corresponde a un
/// jugador con el número de raquetas encordadas y el precio total acumulado.</para>
/// <para>Este reporte es más ligero que la exportación avanzada
/// (<see cref="ExcelAdvancedDataDto"/>) y no requiere selección de módulos.</para>
/// </remarks>
public class TournamentExcelRowDto
{
    /// <summary>Nombre de usuario del jugador.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Nombre completo del jugador.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Cantidad de raquetas encordadas para este jugador.</summary>
    public int RacketCount { get; set; }

    /// <summary>Precio total acumulado de los encordados de este jugador.</summary>
    public decimal TotalPrice { get; set; }
}