using BackEncordados.Common.Database.Config;
using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Export.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEncordados.Export.Controller;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExportController(
    ILogger<ExportController> logger,
    IExportService exportService
) : ControllerBase
{
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database import");
            return StatusCode(500, new { message = "Error importing database", error = ex.Message });
        }
    }

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