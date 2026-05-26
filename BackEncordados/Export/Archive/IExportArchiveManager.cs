using BackEncordados.Common.Dto;
using BackEncordados.Export.Dto;

namespace BackEncordados.Export.Archive;

/// <summary>
/// Contrato del administrador de archivos ZIP para exportación e importación
/// completa de la base de datos del sistema.
/// </summary>
/// <remarks>
/// <para>Gestiona la creación y extracción de archivos ZIP que contienen
/// los datos completos del sistema en formato JSON, organizados por módulo.</para>
/// <para><b>Estructura del archivo ZIP:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Archivo</term>
///     <description>Contenido</description>
///     <description>Serializador</description>
///   </listheader>
///   <item>
///     <term><c>users.json</c></term>
///     <description>Usuarios del sistema (<c>List&lt;User&gt;</c>).</description>
///     <description>Newtonsoft.Json</description>
///   </item>
///   <item>
///     <term><c>tournaments.json</c></term>
///     <description>Torneos (<c>List&lt;Tournaments&gt;</c>).</description>
///     <description>Newtonsoft.Json</description>
///   </item>
///   <item>
///     <term><c>materials.json</c></term>
///     <description>Materiales de encordado (<c>List&lt;Material&gt;</c>).</description>
///     <description>Newtonsoft.Json</description>
///   </item>
///   <item>
///     <term><c>cuerdas.json</c></term>
///     <description>Cuerdas (<c>List&lt;Cuerdas&gt;</c>).</description>
///     <description>Newtonsoft.Json</description>
///   </item>
///   <item>
///     <term><c>orders.json</c></term>
///     <description>Pedidos (<c>List&lt;Pedidos&gt;</c>).</description>
///     <description>Newtonsoft.Json</description>
///   </item>
///   <item>
///     <term><c>manifest.json</c></term>
///     <description>Metadatos de exportación (<see cref="ExportManifestDto"/>).</description>
///     <description>System.Text.Json</description>
///   </item>
/// </list>
/// <para>Los datos de entidades se serializan con <c>Newtonsoft.Json</c> (con
/// <c>ReferenceLoopHandling.Ignore</c>) para manejar relaciones de navegación
/// circulares de Entity Framework. El manifest se serializa con
/// <c>System.Text.Json</c> (camelCase, indentado) por su simplicidad.</para>
/// <para>Todos los métodos trabajan con directorios temporales en <c>%TEMP%</c>
/// que se eliminan en bloques <c>finally</c>. Las excepciones se propagan
/// hacia arriba después de la limpieza.</para>
/// </remarks>
public interface IExportArchiveManager
{
    /// <summary>
    /// Crea un archivo ZIP con todos los datos de la base de datos en formato JSON.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Crea un directorio temporal único en <c>%TEMP%</c>.</description></item>
    ///   <item><description>Serializa cada lista de entidades a JSON con <c>Newtonsoft.Json</c>
    ///   y las escribe como archivos individuales (users.json, tournaments.json, etc.).</description></item>
    ///   <item><description>Construye un <see cref="ExportManifestDto"/> con la fecha de exportación
    ///   y metadatos de cada archivo (nombre, cantidad de registros).</description></item>
    ///   <item><description>Serializa el manifest con <c>System.Text.Json</c> y lo escribe como <c>manifest.json</c>.</description></item>
    ///   <item><description>Comprime todo el directorio temporal en un archivo ZIP mediante <c>ZipFile.CreateFromDirectory</c>.</description></item>
    ///   <item><description>Lee el ZIP como arreglo de bytes y lo retorna.</description></item>
    /// </list>
    /// <para>El directorio temporal se elimina en el bloque <c>finally</c>.</para>
    /// </remarks>
    /// <param name="data">DTO con todas las listas de entidades a exportar.</param>
    /// <returns>Arreglo de bytes con el archivo ZIP generado.</returns>
    Task<byte[]> CreateZipAsync(ExportDataDto data);

    /// <summary>
    /// Extrae los datos de un archivo ZIP y los deserializa en un <see cref="ExportDataDto"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Crea un directorio temporal único en <c>%TEMP%</c>.</description></item>
    ///   <item><description>Guarda el stream de entrada como <c>data.zip</c> en el directorio temporal.</description></item>
    ///   <item><description>Extrae el ZIP con <c>ZipFile.ExtractToDirectory</c> (sobrescribe archivos existentes).</description></item>
    ///   <item><description>Lee cada archivo JSON con verificación de existencia (<c>IOFile.Exists</c>)
    ///   y deserializa con <c>Newtonsoft.Json</c> a la lista correspondiente.</description></item>
    ///   <item><description>Retorna el DTO con todas las listas pobladas.</description></item>
    /// </list>
    /// <para>Si algún archivo no existe en el ZIP, la lista correspondiente queda vacía
    /// (no lanza error). El directorio temporal se elimina en el bloque <c>finally</c>.</para>
    /// </remarks>
    /// <param name="zipStream">Stream del archivo ZIP a extraer.</param>
    /// <returns>DTO con todas las entidades deserializadas.</returns>
    Task<ExportDataDto> ExtractZipAsync(Stream zipStream);

    /// <summary>
    /// Obtiene el manifest de un archivo ZIP sin extraer los datos completos.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Escribe el arreglo de bytes a un archivo ZIP temporal.</description></item>
    ///   <item><description>Abre el ZIP como <c>ZipArchive</c> en modo lectura.</description></item>
    ///   <item><description>Busca la entrada <c>manifest.json</c> dentro del archivo.</description></item>
    ///   <item><description>Si no existe → lanza <c>InvalidOperationException</c>.</description></item>
    ///   <item><description>Lee y deserializa el manifest con <c>System.Text.Json</c>.</description></item>
    /// </list>
    /// <para>Útil para previsualizar el contenido de un ZIP sin extraer todos los datos.
    /// El archivo temporal se elimina en el bloque <c>finally</c>.</para>
    /// </remarks>
    /// <param name="zipData">Arreglo de bytes del archivo ZIP.</param>
    /// <returns>Manifest con metadatos de la exportación.</returns>
    /// <exception cref="InvalidOperationException">El archivo ZIP no contiene <c>manifest.json</c>
    /// o el manifest no pudo deserializarse.</exception>
    Task<ExportManifestDto> GetManifestAsync(byte[] zipData);
}