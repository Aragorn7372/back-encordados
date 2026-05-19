using BackEncordados.Excel.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackEncordados.Excel.Controller;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExcelController(
    ILogger<ExcelController> logger,
    IExcelService excelService
) : ControllerBase
{
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