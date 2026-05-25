namespace BackEncordados.Common.Dto;

/// <summary>
/// DTO que representa el manifiesto de un archivo de exportación.
/// Contiene metadatos sobre la exportación: versión, fecha, descripción
/// y la lista de entidades incluidas con sus respectivos archivos.
/// </summary>
/// <remarks>
/// <para>Este DTO se utiliza como parte del proceso de exportación de datos,
/// permitiendo reconstruir el origen y contenido del archivo exportado.</para>
/// <para><b>Propiedades:</b></para>
/// <list type="bullet">
///     <item><description><c>Version</c>: versión del formato de exportación (semver).</description></item>
///     <item><description><c>ExportedAt</c>: marca temporal UTC de la exportación.</description></item>
///     <item><description><c>Description</c>: descripción textual del contenido del archivo.</description></item>
///     <item><description><c>Entities</c>: lista de <see cref="ExportEntityInfo"/> con detalles de cada entidad.</description></item>
/// </list>
/// </remarks>
public class ExportManifestDto
{
    /// <summary>Versión del formato de exportación. Sigue el esquema semver (por defecto "1.0").</summary>
    public string Version { get; set; } = "1.0";
    /// <summary>Fecha y hora UTC en que se realizó la exportación.</summary>
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Descripción textual del contenido del archivo exportado.</summary>
    public string Description { get; set; } = "Full database export";
    /// <summary>Lista de entidades incluidas en la exportación, con nombre, cantidad de registros y nombre de archivo.</summary>
    public List<ExportEntityInfo> Entities { get; set; } = new();
}

/// <summary>
/// DTO que representa la información de una entidad individual dentro de un manifiesto de exportación.
/// Contiene el nombre lógico de la entidad, la cantidad de registros exportados
/// y el nombre del archivo donde se almacenaron los datos.
/// </summary>
/// <remarks>
/// Cada instancia de <see cref="ExportEntityInfo"/> corresponde a una tabla o colección
/// exportada como parte del proceso de exportación de la base de datos.
/// </remarks>
public class ExportEntityInfo
{
    /// <summary>Nombre lógico de la entidad (ej: "Users", "Pedidos", "Cuerdas").</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Cantidad total de registros exportados de esta entidad.</summary>
    public int RecordCount { get; set; }
    /// <summary>Nombre del archivo donde se almacenan los datos de esta entidad.</summary>
    public string FileName { get; set; } = string.Empty;
}