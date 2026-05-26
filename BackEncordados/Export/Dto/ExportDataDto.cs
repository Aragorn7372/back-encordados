using BackEncordados.Materials.Model;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Model;

namespace BackEncordados.Export.Dto;

/// <summary>
/// DTO contenedor que agrupa todas las entidades del sistema para
/// exportación e importación completa de la base de datos en formato ZIP.
/// </summary>
/// <remarks>
/// <para>Utilizado por <see cref="IExportArchiveManager"/> y <see cref="IExportService"/>
/// para serializar y deserializar la base de datos completa a/desde archivos JSON
/// organizados por módulo dentro de un archivo ZIP.</para>
/// <para><b>Correspondencia con archivos del ZIP:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Propiedad</term>
///     <description>Archivo JSON</description>
///     <description>Entidad</description>
///     <description>Módulo</description>
///   </listheader>
///   <item>
///     <term><see cref="Users"/></term>
///     <description>users.json</description>
///     <description><c>Usuarios.Model.User</c></description>
///     <description>Usuarios del sistema.</description>
///   </item>
///   <item>
///     <term><see cref="Tournaments"/></term>
///     <description>tournaments.json</description>
///     <description><c>Talleres.Model.Tournaments</c></description>
///     <description>Torneos con sus listas de supervisores y trabajadores.</description>
///   </item>
///   <item>
///     <term><see cref="Materials"/></term>
///     <description>materials.json</description>
///     <description><c>Materials.Model.Material</c></description>
///     <description>Materiales de encordado (grips, overgrips, etc.).</description>
///   </item>
///   <item>
///     <term><see cref="Cuerdas"/></term>
///     <description>cuerdas.json</description>
///     <description><c>Materials.Model.Cuerdas</c></description>
///     <description>Cuerdas con calibre, formato y tipo.</description>
///   </item>
///   <item>
///     <term><see cref="Pedidos"/></term>
///     <description>orders.json</description>
///     <description><c>Purchased.Model.Pedidos</c></description>
///     <description>Pedidos con líneas de pedido incluidas (relaciones de navegación de EF Core).</description>
///   </item>
/// </list>
/// <para><b>Nota técnica:</b> La propiedad <see cref="Pedidos"/> incluye las líneas de pedido
/// (<c>PedidoLineas</c>) a través de las relaciones de navegación de Entity Framework Core.
/// La serialización con Newtonsoft.Json (configurado con <c>ReferenceLoopHandling.Ignore</c>)
/// maneja las referencias circulares entre entidades.</para>
/// </remarks>
public class ExportDataDto
{
    /// <summary>Lista de usuarios del sistema para exportación/importación. Serializada como <c>users.json</c>.</summary>
    public List<User> Users { get; set; } = new();

    /// <summary>Lista de torneos para exportación/importación. Serializada como <c>tournaments.json</c>.</summary>
    public List<Tournaments> Tournaments { get; set; } = new();

    /// <summary>Lista de materiales de encordado para exportación/importación. Serializada como <c>materials.json</c>.</summary>
    public List<Material> Materials { get; set; } = new();

    /// <summary>Lista de cuerdas para exportación/importación. Serializada como <c>cuerdas.json</c>.</summary>
    public List<Cuerdas> Cuerdas { get; set; } = new();

    /// <summary>
    /// Lista de pedidos para exportación/importación. Serializada como <c>orders.json</c>.
    /// Incluye las líneas de pedido (<c>PedidoLineas</c>) mediante relaciones de navegación de EF Core.
    /// </summary>
    public List<Pedidos> Pedidos { get; set; } = new();
}