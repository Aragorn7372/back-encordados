using BackEncordados.Common.Database.Config;
using BackEncordados.Materials.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Excel.Repository;

public class MaterialsExcelRepository(
    MaterialsDbContext materialsDbContext,
    ILogger<MaterialsExcelRepository> logger
) : IMaterialsExcelRepository
{
    public async Task<List<Material>> GetMaterialsByTournamentAsync(Ulid tournamentId)
    {
        var materials = await materialsDbContext.Materiales
            .AsNoTracking()
            .Where(m => m.TournamentId == tournamentId)
            .ToListAsync();

        logger.LogInformation("Fetched {Count} materials for tournament {TournamentId}", materials.Count, tournamentId);
        return materials;
    }

    public async Task<List<Cuerdas>> GetCuerdasByTournamentAsync(Ulid tournamentId)
    {
        var cuerdas = await materialsDbContext.Cuerdas
            .AsNoTracking()
            .Where(c => c.TournamentId == tournamentId)
            .ToListAsync();

        logger.LogInformation("Fetched {Count} cuerdas for tournament {TournamentId}", cuerdas.Count, tournamentId);
        return cuerdas;
    }
}
