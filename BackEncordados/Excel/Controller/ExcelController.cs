using BackEncordados.Excel.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackEncordados.Excel.Controller;

/// <summary>
/// Controlador para la exportación e importación de datos de torneos en formato Excel.
/// </summary>
/// <remarks>
/// <para>Proporciona tres endpoints para gestión de datos Excel:</para>
/// <list type="table">
///   <listheader>
///     <term>Endpoint</term>
///     <description>Método</description>
///     <description>Policy requerida</description>
///     <description>Uso</description>
///   </listheader>
///   <item>
///     <term><c>GET api/excel/export/{tournamentId}</c></term>
///     <description><c>ExportTournament</c></description>
///     <description>RequireSupervisorRole</description>
///     <description>Exporta resumen simple del torneo (jugadores, raquetas, precios).</description>
///   </item>
///   <item>
///     <term><c>GET api/excel/advanced/export</c></term>
///     <description><c>ExportAdvanced</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Exporta datos multi-hoja según tipos seleccionados (query param <c>types</c> separado por coma).</description>
///   </item>
///   <item>
///     <term><c>POST api/excel/advanced/import</c></term>
///     <description><c>ImportAdvanced</c></description>
///     <description>RequireOwnerRole</description>
///     <description>Importa datos desde un archivo Excel subido (multipart/form-data).</description>
///   </item>
/// </list>
/// <para>Los errores de autorización se mapean a 403, los errores de formato de ID a 400,
/// y cualquier otro error a 500.</para>
/// </remarks>
/// <param name="logger">Logger para seguimiento de operaciones.</param>
/// <param name="excelService">Servicio de lógica de negocio para exportación/importación Excel.</param>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExcelController(
    ILogger<ExcelController> logger,
    IExcelService excelService
) : ControllerBase
{
    /// <summary>
    /// Exporta un resumen simple del torneo en formato Excel.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Extrae el <c>UserId</c> del claim <c>NameIdentifier</c> del JWT.</description></item>
    ///   <item><description>Si falta el claim → 401 Unauthorized.</description></item>
    ///   <item><description>Llama a <c>excelService.ExportTournamentAsync</c> con el userId y tournamentId.</description></item>
    ///   <item><description>Retorna el archivo Excel como <c>FileContentResult</c> con MIME type
    ///   <c>application/vnd.openxmlformats-officedocument.spreadsheetml.sheet</c>.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireSupervisorRole</c>.</para>
    /// </remarks>
    /// <param name="tournamentId">ID del torneo a exportar (ULID en la ruta).</param>
    /// <returns>Archivo Excel con resumen del torneo.</returns>
    /// <response code="200">Archivo Excel generado correctamente.</response>
    /// <response code="403">El usuario no tiene permisos para exportar este torneo.</response>
    /// <response code="500">Error interno durante la exportación.</response>
    [HttpGet("export/{tournamentId:ulid}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireSupervisorRole")]
    public async Task<IActionResult> ExportTournament(Ulid tournamentId)
    {
        logger.LogInformation("Starting tournament Excel export for tournamentId: {TournamentId}", tournamentId);

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "User ID not found in token" });

            var userId = Ulid.Parse(userIdClaim);
            var excelData = await excelService.ExportTournamentAsync(userId, tournamentId);

            var fileName = $"torneo_{tournamentId}_{DateTime.UtcNow:yyyy-MM-dd}.xlsx";

            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "User not authorized to export tournament {TournamentId}", tournamentId);
            return StatusCode(403, new { message = "No tienes permisos para exportar este torneo" });
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "Invalid user ID format");
            return BadRequest(new { message = "Invalid user ID format" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting tournament {TournamentId}", tournamentId);
            return StatusCode(500, new { message = "Error exporting tournament", error = ex.Message });
        }
    }

    /// <summary>
    /// Exporta datos avanzados del torneo en formato Excel multi-hoja.
    /// </summary>
    /// <remarks>
    /// <para>Permite seleccionar qué módulos incluir mediante el parámetro <paramref name="types"/>
    /// (lista separada por coma). Si está vacío, se exportan todos los módulos disponibles.</para>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Extrae <c>UserId</c> y <c>Role</c> de los claims del JWT.</description></item>
    ///   <item><description>Convierte el string <c>types</c> en una lista (split por coma).</description></item>
    ///   <item><description>Llama a <c>excelService.ExportAdvancedAsync</c> con userId, tournamentId, typeList y role.</description></item>
    ///   <item><description>Retorna el archivo Excel generado.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="tournamentId">ID del torneo (query param).</param>
    /// <param name="types">Tipos de datos a incluir, separados por coma (ej: <c>"users,materials,cuerdas"</c>). Opcional.</param>
    /// <returns>Archivo Excel multi-hoja con los datos solicitados.</returns>
    /// <response code="200">Archivo Excel generado correctamente.</response>
    /// <response code="400">Formato de ID de usuario inválido.</response>
    /// <response code="403">El usuario no tiene permisos para exportar este torneo.</response>
    /// <response code="500">Error interno durante la exportación.</response>
    [HttpGet("advanced/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> ExportAdvanced(
        [FromQuery] Ulid tournamentId,
        [FromQuery] string? types = null)
    {
        logger.LogInformation("Starting advanced Excel export for tournamentId: {TournamentId}, types: {Types}", tournamentId, types);

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "User ID not found in token" });

            if (string.IsNullOrEmpty(roleClaim))
                return Unauthorized(new { message = "Role not found in token" });

            var userId = Ulid.Parse(userIdClaim);
            var typeList = string.IsNullOrEmpty(types) 
                ? new List<string>() 
                : types.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var excelData = await excelService.ExportAdvancedAsync(userId, tournamentId, typeList, roleClaim);

            var fileName = $"torneo_advanced_{tournamentId}_{DateTime.UtcNow:yyyy-MM-dd}.xlsx";

            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "User not authorized to export tournament {TournamentId}", tournamentId);
            return StatusCode(403, new { message = "No tienes permisos para exportar este torneo" });
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "Invalid user ID format");
            return BadRequest(new { message = "Invalid user ID format" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting advanced data for tournament {TournamentId}", tournamentId);
            return StatusCode(500, new { message = "Error exporting data", error = ex.Message });
        }
    }

    /// <summary>
    /// Importa datos a un torneo desde un archivo Excel subido.
    /// </summary>
    /// <remarks>
    /// <para><b>Validaciones:</b></para>
    /// <list type="bullet">
    ///   <item><description>El archivo no debe ser nulo ni vacío.</description></item>
    ///   <item><description>La extensión del archivo debe ser <c>.xlsx</c> (OpenXML).</description></item>
    /// </list>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Valida el archivo subido (no nulo, extensión .xlsx).</description></item>
    ///   <item><description>Extrae <c>UserId</c> y <c>Role</c> de los claims del JWT.</description></item>
    ///   <item><description>Convierte <paramref name="types"/> en lista (split por coma).</description></item>
    ///   <item><summary>Abre el stream del archivo y llama a <c>excelService.ImportAsync</c>.</description></item>
    ///   <item><description>Retorna <c>ExcelImportResultDto</c> con el resultado de la importación.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="file">Archivo Excel en formato .xlsx (multipart/form-data).</param>
    /// <param name="tournamentId">ID del torneo destino (query param).</param>
    /// <param name="types">Tipos de datos a importar, separados por coma (opcional).</param>
    /// <returns>Resultado de la importación con resumen de datos procesados.</returns>
    /// <response code="200">Importación completada exitosamente.</response>
    /// <response code="400">Archivo no proporcionado, formato inválido, o ID de usuario inválido.</response>
    /// <response code="403">El usuario no tiene permisos para importar a este torneo.</response>
    /// <response code="500">Error interno durante la importación.</response>
    [HttpPost("advanced/import")]
    [ProducesResponseType(typeof(BackEncordados.Excel.Dto.ExcelImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireOwnerRole")]
    public async Task<IActionResult> ImportAdvanced(
        IFormFile file,
        [FromQuery] Ulid tournamentId,
        [FromQuery] string? types = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "File must be an .xlsx file" });

        logger.LogInformation("Starting advanced Excel import for tournamentId: {TournamentId}, types: {Types}", tournamentId, types);

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "User ID not found in token" });

            if (string.IsNullOrEmpty(roleClaim))
                return Unauthorized(new { message = "Role not found in token" });

            var userId = Ulid.Parse(userIdClaim);
            var typeList = string.IsNullOrEmpty(types) 
                ? new List<string>() 
                : types.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            using var stream = file.OpenReadStream();
            var result = await excelService.ImportAsync(userId, tournamentId, typeList, roleClaim, stream);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "User not authorized to import to tournament {TournamentId}", tournamentId);
            return StatusCode(403, new { message = "No tienes permisos para importar a este torneo" });
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "Invalid user ID format");
            return BadRequest(new { message = "Invalid user ID format" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error importing data to tournament {TournamentId}", tournamentId);
            return StatusCode(500, new { message = "Error importing data", error = ex.Message });
        }
    }
}