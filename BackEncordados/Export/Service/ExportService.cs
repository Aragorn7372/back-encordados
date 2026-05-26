using BackEncordados.Common.Dto;
using BackEncordados.Export.Archive;
using BackEncordados.Export.Dto;
using BackEncordados.Export.Repository;

namespace BackEncordados.Export.Service;

/// <summary>
/// Implementación de <see cref="IExportService"/> que orquesta la exportación
/// e importación completa de la base de datos en formato ZIP.
/// </summary>
/// <remarks>
/// <para>Actúa como fachada coordinando cuatro repositorios especializados
/// (uno por DbContext) y el administrador de archivos ZIP.</para>
/// <para><b>Repositorios inyectados (uno por DbContext):</b></para>
/// <list type="table">
///   <listheader>
///     <term>Repositorio</term>
///     <description>DbContext / Entidades</description>
///   </listheader>
///   <item>
///     <term><c>exportUserRepository</c></term>
///     <description><c>UserDbContext</c> → Users</description>
///   </item>
///   <item>
///     <term><c>exportPedidosRepository</c></term>
///     <description><c>PedidosDbContext</c> → Pedidos, PedidoLineas</description>
///   </item>
///   <item>
///     <term><c>exportTalleresRepository</c></term>
///     <description><c>TalleresDbContext</c> → Tournaments</description>
///   </item>
///   <item>
///     <term><c>exportMaterialsRepository</c></term>
///     <description><c>MaterialsDbContext</c> → Materiales, Cuerdas</description>
///   </item>
/// </list>
/// <para><b>Orden de operaciones:</b></para>
/// <list type="bullet">
///   <item><description>Exportación: tournaments → users → materials → cuerdas → pedidos.</description></item>
///   <item><description>Limpieza: pedidos → materials → users → tournaments (orden inverso de dependencias).</description></item>
///   <item><description>Importación: tournaments → users → materials → cuerdas → pedidos.</description></item>
/// </list>
/// </remarks>
/// <param name="exportUserRepository">Repositorio de usuarios.</param>
/// <param name="exportPedidosRepository">Repositorio de pedidos.</param>
/// <param name="exportTalleresRepository">Repositorio de torneos.</param>
/// <param name="exportMaterialsRepository">Repositorio de materiales y cuerdas.</param>
/// <param name="archiveManager">Administrador de archivos ZIP.</param>
/// <param name="logger">Logger para seguimiento de operaciones.</param>
public class ExportService(
    IExportUserRepository exportUserRepository,
    IExportPedidosRepository exportPedidosRepository,
    IExportTalleresRepository exportTalleresRepository,
    IExportMaterialsRepository exportMaterialsRepository,
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
    ///   <item><description>Obtiene torneos desde <c>ExportTalleresRepository</c>.</description></item>
    ///   <item><description>Obtiene usuarios desde <c>ExportUserRepository</c>.</description></item>
    ///   <item><description>Obtiene materiales y cuerdas desde <c>ExportMaterialsRepository</c>.</description></item>
    ///   <item><description>Obtiene pedidos desde <c>ExportPedidosRepository</c>.</description></item>
    ///   <item><description>Delega en <c>IExportArchiveManager.CreateZipAsync</c> la serialización y compresión.</description></item>
    /// </list>
    /// </remarks>
    /// <returns>Arreglo de bytes con el archivo ZIP de la base de datos completa.</returns>
    public async Task<byte[]> ExportDatabaseAsync()
    {
        logger.LogInformation("Starting database export");

        var data = new ExportDataDto
        {
            Tournaments = await exportTalleresRepository.GetAllTournamentsAsync(),
            Users = await exportUserRepository.GetAllUsersAsync(),
            Materials = await exportMaterialsRepository.GetAllMaterialsAsync(),
            Cuerdas = await exportMaterialsRepository.GetAllCuerdasAsync(),
            Pedidos = await exportPedidosRepository.GetAllPedidosAsync()
        };

        var zipData = await archiveManager.CreateZipAsync(data);

        logger.LogInformation("Export completed successfully");
        return zipData;
    }

    /// <summary>
    /// Obtiene el manifest de un archivo ZIP de exportación sin extraer los datos completos.
    /// </summary>
    /// <remarks>
    /// <para>Método ligero que delega directamente en <c>IExportArchiveManager.GetManifestAsync</c>.</para>
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
    ///   <item><description>Extrae el contenido del ZIP mediante <c>IExportArchiveManager.ExtractZipAsync</c>.</description></item>
    ///   <item><description>Limpia datos existentes en orden inverso de dependencias:
    ///     pedidos → materials → users → tournaments.</description></item>
    ///   <item><description>Importa nuevos datos en orden directo: tournaments → users → materials → cuerdas → pedidos.</description></item>
    /// </list>
    /// <para><b>Nota transaccional:</b> La transacción es manejada por el decorador
    /// <c>[Transactional]</c> en el controller.</para>
    /// </remarks>
    /// <param name="zipStream">Stream del archivo ZIP (.zip) que contiene los datos JSON a importar.</param>
    public async Task ImportDatabaseAsync(Stream zipStream)
    {
        logger.LogInformation("Starting database import");

        var data = await archiveManager.ExtractZipAsync(zipStream);

        await exportPedidosRepository.ClearPedidosAsync();
        await exportMaterialsRepository.ClearMaterialsAsync();
        await exportUserRepository.ClearUsersAsync();
        await exportTalleresRepository.ClearTournamentsAsync();

        await exportTalleresRepository.ImportTournamentsAsync(data.Tournaments);
        await exportUserRepository.ImportUsersAsync(data.Users);
        await exportMaterialsRepository.ImportMaterialsAsync(data.Materials);
        await exportMaterialsRepository.ImportCuerdasAsync(data.Cuerdas);
        await exportPedidosRepository.ImportPedidosAsync(data.Pedidos);

        logger.LogInformation("Import completed successfully");
    }
}
