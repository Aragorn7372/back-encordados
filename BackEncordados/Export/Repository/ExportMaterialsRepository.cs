using BackEncordados.Common.Database.Config;
using BackEncordados.Materials.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Export.Repository;

public class ExportMaterialsRepository(
    MaterialsDbContext materialsDbContext,
    ILogger<ExportMaterialsRepository> logger
) : IExportMaterialsRepository
{
    public async Task<List<Material>> GetAllMaterialsAsync()
    {
        return await materialsDbContext.Materiales.AsNoTracking().IgnoreQueryFilters().ToListAsync();
    }

    public async Task<List<Cuerdas>> GetAllCuerdasAsync()
    {
        return await materialsDbContext.Cuerdas.AsNoTracking().IgnoreQueryFilters().ToListAsync();
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
        }
        else
        {
            await materialsDbContext.Cuerdas.ExecuteDeleteAsync();
            await materialsDbContext.Materiales.ExecuteDeleteAsync();
        }
        logger.LogInformation("Cleared materials");
    }

    public async Task ImportMaterialsAsync(List<Material> materials)
    {
        await materialsDbContext.Materiales.AddRangeAsync(materials);
        await materialsDbContext.SaveChangesAsync();
        logger.LogInformation("Imported {Count} materials", materials.Count);
    }

    public async Task ImportCuerdasAsync(List<Cuerdas> cuerdas)
    {
        await materialsDbContext.Cuerdas.AddRangeAsync(cuerdas);
        await materialsDbContext.SaveChangesAsync();
        logger.LogInformation("Imported {Count} cuerdas", cuerdas.Count);
    }
}
