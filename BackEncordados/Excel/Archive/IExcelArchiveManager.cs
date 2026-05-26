using BackEncordados.Excel.Dto;

namespace BackEncordados.Excel.Archive;

/// <summary>
/// Define el contrato para la generación y lectura de archivos Excel
/// utilizando la biblioteca ClosedXML.
/// </summary>
/// <remarks>
/// <para>Proporciona dos modos de exportación y un modo de importación:</para>
/// <list type="table">
///   <listheader>
///     <term>Operación</term>
///     <description>Método</description>
///     <description>Descripción</description>
///   </listheader>
///   <item>
///     <term>Exportación simple</term>
///     <description><c>CreateExcelAsync</c></description>
///     <description>Una sola hoja con resumen de raquetas encordadas por jugador.</description>
///   </item>
///   <item>
///     <term>Exportación avanzada</term>
///     <description><c>CreateAdvancedExcelAsync</c></description>
///     <description>Múltiples hojas seleccionables mediante lista de tipos (users, materials, cuerdas, tournament, pedidos).</description>
///   </item>
///   <item>
///     <term>Importación</term>
///     <description><c>ReadExcelAsync</c></description>
///     <description>Lee un archivo Excel y reconstruye el DTO completo con todas las hojas.</description>
///   </item>
/// </list>
/// <para>Todas las operaciones trabajan en memoria (<c>byte[]</c> o <c>Stream</c>)
/// y no requieren archivos temporales en disco.</para>
/// </remarks>
public interface IExcelArchiveManager
{
    /// <summary>
    /// Genera un archivo Excel con una hoja de resumen de raquetas encordadas por jugador.
    /// </summary>
    /// <remarks>
    /// <para>Columnas: Username, Name, Raquetas Encordadas, Precio Total.</para>
    /// <para>Encabezados en negrita con fondo gris claro. Ancho de columnas ajustado al contenido.</para>
    /// </remarks>
    /// <param name="data">Lista de filas con datos de jugadores.</param>
    /// <param name="tournamentName">Nombre del torneo (no se usa actualmente en el contenido).</param>
    /// <returns>Array de bytes del archivo Excel generado.</returns>
    Task<byte[]> CreateExcelAsync(IEnumerable<TournamentExcelRowDto> data, string tournamentName);

    /// <summary>
    /// Genera un archivo Excel con múltiples hojas seleccionables según los tipos solicitados.
    /// </summary>
    /// <remarks>
    /// <para>Tipos soportados: <c>"users"</c>, <c>"materials"</c>, <c>"cuerdas"</c>,
    /// <c>"tournament"</c>, <c>"pedidos"</c>.</para>
    /// <para>Si ningún tipo coincide o no hay datos, crea una hoja "Sin Datos" con fondo amarillo.</para>
    /// </remarks>
    /// <param name="data">DTO con todos los datos disponibles para exportar.</param>
    /// <param name="types">Lista de tipos de hojas a incluir.</param>
    /// <param name="tournamentName">Nombre del torneo (no se usa actualmente en el contenido).</param>
    /// <returns>Array de bytes del archivo Excel generado.</returns>
    Task<byte[]> CreateAdvancedExcelAsync(ExcelAdvancedDataDto data, List<string> types, string tournamentName);

    /// <summary>
    /// Lee un archivo Excel y reconstruye el <see cref="ExcelAdvancedDataDto"/> con todas las hojas.
    /// </summary>
    /// <remarks>
    /// <para>Busca las hojas por nombre exacto: "Usuarios", "Materiales", "Cuerdas",
    /// "Torneo", "Pedidos" y "PedidoLineas". Las hojas faltantes se omiten.</para>
    /// <para>Las filas con campos clave vacíos se ignoran.</para>
    /// </remarks>
    /// <param name="stream">Stream del archivo Excel a leer.</param>
    /// <returns>DTO con los datos extraídos del archivo.</returns>
    Task<ExcelAdvancedDataDto> ReadExcelAsync(Stream stream);
}