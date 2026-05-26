using BackEncordados.Common.Dto;
using BackEncordados.Export.Archive;
using BackEncordados.Export.Repository;

namespace BackEncordados.Export.Service;

/// <summary>
/// Implementación de <see cref="IExportService"/> que orquesta la exportación
/// e importación completa de la base de datos en formato ZIP.
/// </summary>
/// <remarks>
/// <para>Actúa como fachada simple coordinando las operaciones entre el repositorio
/// de datos (<see cref="IExportRepository"/>) y el administrador de archivos
/// ZIP (<see cref="IExportArchiveManager"/>).</para>
/// <para><b>Dependencias inyectadas:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Parámetro</term>
///     <term>Tipo</term>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>repository</c></term>
///     <term><see cref="IExportRepository"/></term>
///     <description>Acceso a datos: obtener, limpiar e importar entidades.</description>
///   </item>
///   <item>
///     <term><c>archiveManager</c></term>
///     <term><see cref="IExportArchiveManager"/></term>
///     <description>Creación y extracción de archivos ZIP con serialización JSON.</description>
///   </item>
///   <item>
///     <term><c>logger</c></term>
///     <term><c>ILogger&lt;ExportService&gt;</c></term>
///     <description>Logging de operaciones de exportación e importación.</description>
///   </item>
/// </list>
/// <para><b>Relación con la transaccionalidad:</b> Este servicio no maneja transacciones
/// internamente. La operación de importación se ejecuta dentro de una transacción
/// coordinada por el decorador <c>[Transactional]</c> aplicado en el método
/// <c>ExportController.Import</c>. Si ocurre un error durante la limpieza o
/// importación, el filtro de transacción ejecuta rollback en todos los DbContexts.</para>
/// </remarks>
/// <param name="repository">Repositorio de acceso a datos globales.</param>
/// <param name="archiveManager">Administrador de archivos ZIP.</param>
/// <param name="logger">Logger para seguimiento de operaciones.</param>
public class ExportService(
    IExportRepository repository,
    IExportArchiveManager archiveManager,
    ILogger<ExportService> logger
) : IExportService
{
    /// <summary>
    /// Exporta toda la base de datos como un archivo ZIP.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Registra el inicio de la operación de exportación.</description></item>
    ///   <item><description>Obtiene todas las entidades de la base de datos mediante
    ///   <c>IExportRepository.GetAllDataAsync</c>, que consulta los cuatro DbContexts
    ///   usando <c>IgnoreQueryFilters()</c> para incluir registros soft-deleted.</description></item>
    ///   <item><description>Delega en <c>IExportArchiveManager.CreateZipAsync</c> la serialización
    ///   de cada lista de entidades a archivos JSON individuales y su compresión
    ///   en un archivo ZIP con manifest.</description></item>
    ///   <item><description>Registra la finalización exitosa y retorna el arreglo de bytes del ZIP.</description></item>
    /// </list>
    /// </remarks>
    /// <returns>Arreglo de bytes con el archivo ZIP de la base de datos completa.</returns>
    public async Task<byte[]> ExportDatabaseAsync()
    {
        logger.LogInformation("Starting database export");

        var data = await repository.GetAllDataAsync();
        var zipData = await archiveManager.CreateZipAsync(data);

        logger.LogInformation("Export completed successfully");
        return zipData;
    }

    /// <summary>
    /// Obtiene el manifest de un archivo ZIP de exportación sin extraer los datos completos.
    /// </summary>
    /// <remarks>
    /// <para>Método ligero que delega directamente en <c>IExportArchiveManager.GetManifestAsync</c>.</para>
    /// <para>Lee únicamente el archivo <c>manifest.json</c> del interior del ZIP,
    /// que contiene la fecha de exportación (<c>ExportedAt</c>) y la lista de
    /// entidades incluidas con sus nombres y cantidades de registros.</para>
    /// <para>No modifica la base de datos ni carga todas las entidades en memoria,
    /// por lo que es ideal para previsualización previa a una importación.</para>
    /// </remarks>
    /// <param name="zipData">Arreglo de bytes del archivo ZIP a inspeccionar.</param>
    /// <returns>Manifest con metadatos de la exportación.</returns>
    public async Task<ExportManifestDto> GetManifestAsync(byte[] zipData)
    {
        return await archiveManager.GetManifestAsync(zipData);
    }

    /// <summary>
    /// Importa datos desde un archivo ZIP a la base de datos.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Registra el inicio de la operación de importación.</description></item>
    ///   <item><description>Extrae el contenido del ZIP mediante <c>IExportArchiveManager.ExtractZipAsync</c>:
    ///   <list type="bullet">
    ///     <item><description>Guarda el stream en un archivo temporal en <c>%TEMP%</c>.</description></item>
    ///     <item><description>Descomprime el ZIP con <c>ZipFile.ExtractToDirectory</c>.</description></item>
    ///     <item><description>Lee y deserializa cada archivo JSON (users, tournaments, materials, cuerdas, orders).</description></item>
    ///     <item><description>Retorna un <c>ExportDataDto</c> con todas las listas pobladas.</description></item>
    ///     <item><description>Limpia el directorio temporal en <c>finally</c>.</description></item>
    ///   </list></description></item>
    ///   <item><description>Limpia todos los datos existentes mediante <c>IExportRepository.ClearAllDataAsync</c>:
    ///   <list type="bullet">
    ///     <item><description>Selecciona estrategia según <c>Database.IsInMemory()</c>.</description></item>
    ///     <item><description>InMemory: <c>RemoveRange</c> + <c>SaveChangesAsync</c> por módulo.</description></item>
    ///     <item><description>Producción: <c>ExecuteDeleteAsync</c> para tablas bulk, <c>RemoveRange</c> para OwnsMany.</description></item>
    ///     <item><description>Orden inverso: pedidos → materials/cuerdas → users → tournaments.</description></item>
    ///   </list></description></item>
    ///   <item><description>Importa los nuevos datos mediante <c>IExportRepository.ImportDataAsync</c>:
    ///   <list type="bullet">
    ///     <item><description>Orden directo: tournaments → users → materials → cuerdas → pedidos.</description></item>
    ///     <item><description>Tournaments requiere manejo especial de <c>WorkerMachineAssignments</c>.</description></item>
    ///   </list></description></item>
    ///   <item><description>Registra la finalización exitosa.</description></item>
    /// </list>
    /// <para><b>Nota transaccional:</b> La transaccionalidad de la operación completa
    /// (extracción + limpieza + importación) es manejada por el decorador
    /// <c>[Transactional]</c> aplicado en el método <c>ExportController.Import</c>,
    /// que coordina un commit/rollback manual sobre los cuatro DbContexts.</para>
    /// </remarks>
    /// <param name="zipStream">Stream del archivo ZIP (.zip) que contiene los datos JSON a importar.</param>
    public async Task ImportDatabaseAsync(Stream zipStream)
    {
        logger.LogInformation("Starting database import");

        var data = await archiveManager.ExtractZipAsync(zipStream);
        await repository.ClearAllDataAsync();
        await repository.ImportDataAsync(data);

        logger.LogInformation("Import completed successfully");
    }
}