using BackEncordados.Common.Database.Config;
using BackEncordados.Materials.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Export.Repository;

public class MaterialsExportRepository(
    MaterialsDbContext materialsDbContext,
    ILogger<MaterialsExportRepository> logger
) : IMaterialsExportRepository
{
    public async Task<(List<Material> Materials, List<Cuerdas> Cuerdas)> GetMaterialsDataAsync()
    {
        var materials = await materialsDbContext.Materiales.AsNoTracking().IgnoreQueryFilters().ToListAsync();
        var cuerdas = await materialsDbContext.Cuerdas.AsNoTracking().IgnoreQueryFilters().ToListAsync();
        logger.LogInformation("Fetched {Count} materials and {CuerdasCount} cuerdas", materials.Count, cuerdas.Count);
        return (materials, cuerdas);
    }

    public async Task ClearMaterialsAsync()
    {
        if (materialsDbContext.Database.IsInMemory())
        {
            var cuerdas = await materialsDbContext.Cuerdas.ToListAsync();
            materialsDbContext.Cuerdas.RemoveRange(cuerdas);

            var materials = await materialsDbContext.Materiales.ToListAsync();
            materialsDbContext.Materiales.RemoveRange(materials);

            await materialsDbContext.SaveChangesAsync();
            logger.LogInformation("Cleared materials (in-memory)");
        }
        else
        {
            await materialsDbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Cuerdas\" RESTART IDENTITY");
            await materialsDbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Materiales\" RESTART IDENTITY");
            logger.LogInformation("Cleared materials (production)");
        }
    }

    public async Task ImportMaterialsAsync(List<Material> materials)
    {
        if (!materials.Any()) return;

        await materialsDbContext.Materiales.AddRangeAsync(materials);
        await materialsDbContext.SaveChangesAsync();
        logger.LogInformation("Imported {Count} materials", materials.Count);
    }

    public async Task ImportCuerdasAsync(List<Cuerdas> cuerdas)
    {
        if (!cuerdas.Any()) return;

        await materialsDbContext.Cuerdas.AddRangeAsync(cuerdas);
        await materialsDbContext.SaveChangesAsync();
        logger.LogInformation("Imported {Count} cuerdas", cuerdas.Count);
    }

    public async Task ResyncSequencesAsync()
    {
        if (materialsDbContext.Database.IsInMemory()) return;

        logger.LogInformation("Resynchronizing identity sequences");

        await materialsDbContext.Database.ExecuteSqlRawAsync(
            "SELECT setval('\"Cuerdas_Id_seq\"', COALESCE((SELECT MAX(\"Id\") FROM \"Cuerdas\"), 0) + 1, false), " +
            "setval('\"Materiales_Id_seq\"', COALESCE((SELECT MAX(\"Id\") FROM \"Materiales\"), 0) + 1, false)");

        logger.LogInformation("Identity sequences resynchronized");
    }
}
