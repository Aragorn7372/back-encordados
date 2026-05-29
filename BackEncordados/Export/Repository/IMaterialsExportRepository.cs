using BackEncordados.Materials.Model;

namespace BackEncordados.Export.Repository;

public interface IMaterialsExportRepository
{
    Task<(List<Material> Materials, List<Cuerdas> Cuerdas)> GetMaterialsDataAsync();
    Task ClearMaterialsAsync();
    Task ImportMaterialsAsync(List<Material> materials);
    Task ImportCuerdasAsync(List<Cuerdas> cuerdas);
    Task ResyncSequencesAsync();
}
