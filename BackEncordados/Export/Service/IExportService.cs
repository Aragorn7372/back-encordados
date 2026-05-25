using BackEncordados.Common.Dto;

namespace BackEncordados.Export.Service;

/// <summary>
/// Contrato del servicio de lógica de negocio para la exportación e importación
/// completa de la base de datos del sistema en formato ZIP.
/// </summary>
/// <remarks>
/// <para>Actúa como fachada (Facade pattern) que orquesta las operaciones entre
/// el repositorio de datos (<see cref="IExportRepository"/>) y el administrador
/// de archivos ZIP (<see cref="IExportArchiveManager"/>).</para>
/// <para><b>Métodos del contrato:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Retorno</description>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>ExportDatabaseAsync</c></term>
///     <description><c>byte[]</c></description>
///     <description>Obtiene todas las entidades de la base de datos a través del repositorio
///     y las comprime en un archivo ZIP mediante el archive manager.</description>
///   </item>
///   <item>
///     <term><c>GetManifestAsync</c></term>
///     <description><c>ExportManifestDto</c></description>
///     <description>Lee únicamente el archivo <c>manifest.json</c> del interior de un ZIP
///     sin deserializar los datos completos de las entidades. Operación ligera
///     para previsualización.</description>
///   </item>
///   <item>
///     <term><c>ImportDatabaseAsync</c></term>
///     <description><c>Task</c></description>
///     <description>Extrae el contenido de un archivo ZIP, limpia todos los datos existentes
///     en la base de datos y luego importa los nuevos datos en el orden correcto
///     de dependencias. La transaccionalidad es manejada por el decorador
///     <c>[Transactional]</c> en el controlador.</description>
///   </item>
/// </list>
/// <para><b>Flujo de importación:</b></para>
/// <list type="number">
///   <item><description>Extrae ZIP → <c>ExportDataDto</c> con todas las entidades.</description></item>
///   <item><description>Limpia BD completa (orden inverso de dependencias).</description></item>
///   <item><description>Importa datos (orden directo: torneos → usuarios → materiales → cuerdas → pedidos).</description></item>
/// </list>
/// </remarks>
public interface IExportService
{
    /// <summary>
    /// Exporta toda la base de datos como un archivo ZIP.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>IExportRepository.GetAllDataAsync</c> para obtener todas
    ///   las entidades de los cuatro DbContexts.</description></item>
    ///   <item><description>Delega en <c>IExportArchiveManager.CreateZipAsync</c> la serialización
    ///   a JSON y compresión en ZIP.</description></item>
    ///   <item><description>Retorna el arreglo de bytes del ZIP generado.</description></item>
    /// </list>
    /// <para>El ZIP resultante contiene un archivo JSON por módulo (users, tournaments,
    /// materials, cuerdas, orders) más un manifest.json con metadatos.</para>
    /// </remarks>
    /// <returns>Arreglo de bytes con el archivo ZIP de la base de datos completa.</returns>
    Task<byte[]> ExportDatabaseAsync();

    /// <summary>
    /// Obtiene el manifest de un archivo ZIP de exportación sin extraer los datos completos.
    /// </summary>
    /// <remarks>
    /// <para>Método ligero que delega directamente en <c>IExportArchiveManager.GetManifestAsync</c>.
    /// Lee únicamente el archivo <c>manifest.json</c> del interior del ZIP, que contiene
    /// la fecha de exportación y la lista de entidades incluidas con sus cantidades.</para>
    /// <para>Útil para previsualizar el contenido de un archivo ZIP antes de decidir
    /// si importarlo. No modifica la base de datos ni consume tanta memoria como
    /// una importación completa.</para>
    /// </remarks>
    /// <param name="zipData">Arreglo de bytes del archivo ZIP a inspeccionar.</param>
    /// <returns>Manifest con metadatos de la exportación.</returns>
    Task<ExportManifestDto> GetManifestAsync(byte[] zipData);

    /// <summary>
    /// Importa datos desde un archivo ZIP a la base de datos.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Extrae el contenido del ZIP mediante <c>IExportArchiveManager.ExtractZipAsync</c>,
    ///   que descomprime y deserializa todos los archivos JSON a un <c>ExportDataDto</c>.</description></item>
    ///   <item><description>Limpia todos los datos existentes mediante <c>IExportRepository.ClearAllDataAsync</c>,
    ///   que selecciona automáticamente la estrategia de borrado según el proveedor
    ///   de base de datos (InMemory o producción).</description></item>
    ///   <item><description>Importa los nuevos datos mediante <c>IExportRepository.ImportDataAsync</c>,
    ///   que respeta el orden de dependencias: torneos (con manejo especial de
    ///   WorkerMachineAssignments), luego usuarios, materiales, cuerdas y finalmente pedidos.</description></item>
    /// </list>
    /// <para><b>Nota transaccional:</b> Este método no maneja transacciones por sí mismo.
    /// La transaccionalidad es proporcionada por el decorador <c>[Transactional]</c>
    /// en el método <c>ExportController.Import</c>, que coordina un commit/rollback
    /// manual sobre los cuatro DbContexts.</para>
    /// </remarks>
    /// <param name="zipStream">Stream del archivo ZIP (.zip) a importar.</param>
    Task ImportDatabaseAsync(Stream zipStream);
}