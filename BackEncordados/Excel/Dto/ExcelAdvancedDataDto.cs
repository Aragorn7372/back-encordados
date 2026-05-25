namespace BackEncordados.Excel.Dto;

/// <summary>
/// DTO contenedor que agrupa todos los datos exportables de un torneo
/// para la exportación avanzada a Excel multi-hoja.
/// </summary>
/// <remarks>
/// <para>Cada propiedad representa una hoja independiente en el archivo Excel generado.
/// El servicio <see cref="IExcelService"/> pobla únicamente las listas solicitadas
/// según el parámetro <c>types</c> de la request.</para>
/// <para><b>Hojas disponibles:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Propiedad</term>
///     <description>Hoja Excel</description>
///     <description>Contenido</description>
///   </listheader>
///   <item>
///     <term><see cref="Users"/></term>
///     <description>Usuarios</description>
///     <description>Jugadores y personal del torneo.</description>
///   </item>
///   <item>
///     <term><see cref="Materials"/></term>
///     <description>Materiales</description>
///     <description>Materiales de encordado (marca, modelo, stock, precio).</description>
///   </item>
///   <item>
///     <term><see cref="Cuerdas"/></term>
///     <description>Cuerdas</description>
///     <description>Cuerdas con calibre, formato y tipo.</description>
///   </item>
///   <item>
///     <term><see cref="Tournament"/></term>
///     <description>Torneo</description>
///     <description>Datos generales del torneo y sus responsables.</description>
///   </item>
///   <item>
///     <term><see cref="Pedidos"/></term>
///     <description>Pedidos</description>
///     <description>Órdenes de encordado por jugador.</description>
///   </item>
///   <item>
///     <term><see cref="PedidoLineas"/></term>
///     <description>Líneas de Pedido</description>
///     <description>Detalle de cada raqueta en un pedido.</description>
///   </item>
/// </list>
/// </remarks>
public class ExcelAdvancedDataDto
{
    /// <summary>Lista de usuarios del torneo para la hoja "Usuarios".</summary>
    public List<ExcelUsersDto> Users { get; set; } = new();

    /// <summary>Lista de materiales para la hoja "Materiales".</summary>
    public List<ExcelMaterialsDto> Materials { get; set; } = new();

    /// <summary>Lista de cuerdas para la hoja "Cuerdas".</summary>
    public List<ExcelCuerdasDto> Cuerdas { get; set; } = new();

    /// <summary>Lista de datos del torneo para la hoja "Torneo".</summary>
    public List<ExcelTournamentDto> Tournament { get; set; } = new();

    /// <summary>Lista de pedidos para la hoja "Pedidos".</summary>
    public List<ExcelPedidosDto> Pedidos { get; set; } = new();

    /// <summary>Lista de líneas de pedido para la hoja "Líneas de Pedido".</summary>
    public List<ExcelPedidoLineasDto> PedidoLineas { get; set; } = new();
}