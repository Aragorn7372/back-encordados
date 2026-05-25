using BackEncordados.Common.Database.Config;
using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Export.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Export.Controller;

/// <summary>
/// Controlador para la exportación e importación completa de la base de datos
/// en formato ZIP con archivos JSON por módulo.
/// </summary>
/// <remarks>
/// <para>Proporciona tres endpoints para administradores del sistema,
/// permitiendo respaldar y restaurar la base de datos completa:</para>
/// <list type="table">
///   <listheader>
///     <term>Endpoint</term>
///     <description>Método</description>
///     <description>Policy</description>
///     <description>Uso</description>
///   </listheader>
///   <item>
///     <term><c>GET api/export</c></term>
///     <description><c>Export</c></description>
///     <description>RequireAdminRole</description>
///     <description>Exporta toda la base de datos como archivo ZIP con archivos JSON
///     por módulo (users, tournaments, materials, cuerdas, orders) más un manifest
///     con metadatos.</description>
///   </item>
///   <item>
///     <term><c>POST api/export/import</c></term>
///     <description><c>Import</c></description>
///     <description>RequireAdminRole</description>
///     <description>Importa datos desde un archivo ZIP subido. Operación transaccional
///     sobre los cuatro DbContexts del sistema.</description>
///   </item>
///   <item>
///     <term><c>POST api/export/manifest</c></term>
///     <description><c>GetManifest</c></description>
///     <description>RequireAdminRole</description>
///     <description>Previsualiza el contenido de un archivo ZIP de exportación
///     sin realizar la importación (solo lee el manifest.json interno).</description>
///   </item>
/// </list>
/// <para><b>Códigos de respuesta HTTP:</b></para>
/// <list type="bullet">
///   <item><description>200 OK — Operación exitosa.</description></item>
///   <item><description>400 Bad Request — Archivo no proporcionado o formato inválido.</description></item>
///   <item><description>500 Internal Server Error — Error interno durante la operación.</description></item>
/// </list>
/// </remarks>
/// <param name="logger">Logger para seguimiento de operaciones de exportación/importación.</param>
/// <param name="exportService">Servicio de lógica de negocio para exportación e importación de la base de datos.</param>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExportController(
    ILogger<ExportController> logger,
    IExportService exportService
) : ControllerBase
{
    /// <summary>
    /// Exporta toda la base de datos como un archivo ZIP.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Registra el inicio de la operación de exportación.</description></item>
    ///   <item><description>Llama a <c>IExportService.ExportDatabaseAsync</c> que serializa todos
    ///   los módulos de datos a JSON y los comprime en un ZIP con manifest.</description></item>
    ///   <item><description>Retorna el archivo ZIP como <c>FileContentResult</c> con MIME type
    ///   <c>application/zip</c> y nombre <c>database_export_{yyyy-MM-dd}.zip</c>.</description></item>
    /// </list>
    /// <para><b>Contenido del ZIP:</b> Incluye archivos JSON para usuarios, torneos, materiales,
    /// cuerdas y pedidos, más un archivo <c>manifest.json</c> con metadatos de exportación.</para>
    /// <para>Requiere policy <c>RequireAdminRole</c>.</para>
    /// </remarks>
    /// <returns>Archivo ZIP con la base de datos completa.</returns>
    /// <response code="200">Archivo ZIP generado correctamente.</response>
    /// <response code="500">Error interno durante la exportación.</response>
    [HttpGet]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> Export()
    {
        logger.LogInformation("Starting database export");
        try
        {
            var zipData = await exportService.ExportDatabaseAsync();
            var fileName = $"database_export_{DateTime.UtcNow:yyyy-MM-dd}.zip";

            return File(zipData, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database export");
            return StatusCode(500, new { message = "Error exporting database", error = ex.Message });
        }
    }

    /// <summary>
    /// Importa datos a la base de datos desde un archivo ZIP subido.
    /// </summary>
    /// <remarks>
    /// <para><b>Validaciones:</b></para>
    /// <list type="bullet">
    ///   <item><description>El archivo no debe ser nulo ni vacío.</description></item>
    ///   <item><description>La extensión del archivo debe ser <c>.zip</c>.</description></item>
    /// </list>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Valida el archivo subido (no nulo, extensión .zip).</description></item>
    ///   <item><description>Abre el stream del archivo y llama a <c>IExportService.ImportDatabaseAsync</c>.</description></item>
    ///   <item><description>Retorna 200 OK con mensaje de éxito.</description></item>
    /// </list>
    /// <para><b>Transaccionalidad:</b> El método está decorado con <c>[Transactional]</c>
    /// que coordina una transacción manual a través de los cuatro DbContexts
    /// (<c>UserDbContext</c>, <c>MaterialsDbContext</c>, <c>PedidosDbContext</c>, <c>TalleresDbContext</c>).
    /// Si ocurre un <c>DbUpdateException</c>, esta se relanza para que el filtro
    /// <c>TransactionalAttribute</c> ejecute el rollback de la transacción.</para>
    /// <para>Requiere policy <c>RequireAdminRole</c>.</para>
    /// </remarks>
    /// <param name="file">Archivo ZIP (.zip) que contiene los datos JSON a importar (multipart/form-data).</param>
    /// <returns>Mensaje de confirmación de importación exitosa.</returns>
    /// <response code="200">Importación completada exitosamente.</response>
    /// <response code="400">Archivo no proporcionado o formato inválido (debe ser .zip).</response>
    /// <response code="500">Error interno durante la importación.</response>
    [HttpPost("import")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    [Transactional(
        typeof(UserDbContext),
        typeof(MaterialsDbContext),
        typeof(PedidosDbContext),
        typeof(TalleresDbContext)
    )]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "File must be a .zip file" });

        logger.LogInformation("Starting database import from file: {FileName}", file.FileName);

        try
        {
            using var stream = file.OpenReadStream();
            await exportService.ImportDatabaseAsync(stream);

            return Ok(new { message = "Database imported successfully" });
        }
        catch (DbUpdateException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database import");
            return StatusCode(500, new { message = "Error importing database", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene el manifest de un archivo ZIP de exportación sin importar los datos.
    /// </summary>
    /// <remarks>
    /// <para><b>Validaciones:</b></para>
    /// <list type="bullet">
    ///   <item><description>El archivo no debe ser nulo ni vacío.</description></item>
    /// </list>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Valida que el archivo no sea nulo ni vacío.</description></item>
    ///   <item><description>Copia el stream del archivo a un <c>MemoryStream</c> para obtener
    ///   el arreglo de bytes (necesario para <c>IExportService.GetManifestAsync</c>).</description></item>
    ///   <item><description>Llama al servicio que abre el ZIP y extrae solo el <c>manifest.json</c>
    ///   sin deserializar los datos completos.</description></item>
    ///   <item><description>Retorna el <see cref="ExportManifestDto"/> con la fecha de exportación
    ///   y la lista de entidades incluidas.</description></item>
    /// </list>
    /// <para>Útil para previsualizar el contenido de un archivo ZIP antes de decidir
    /// si importarlo. No modifica la base de datos.</para>
    /// <para>Requiere policy <c>RequireAdminRole</c>.</para>
    /// </remarks>
    /// <param name="file">Archivo ZIP (.zip) del cual leer el manifest (multipart/form-data).</param>
    /// <returns>Manifest con metadatos de la exportación.</returns>
    /// <response code="200">Manifest leído exitosamente.</response>
    /// <response code="400">Archivo no proporcionado.</response>
    /// <response code="500">Error interno al leer el manifest.</response>
    [HttpPost("manifest")]
    [ProducesResponseType(typeof(ExportManifestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> GetManifest(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        try
        {
            using var stream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var manifest = await exportService.GetManifestAsync(memoryStream.ToArray());

            return Ok(manifest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading manifest");
            return StatusCode(500, new { message = "Error reading manifest", error = ex.Message });
        }
    }
}