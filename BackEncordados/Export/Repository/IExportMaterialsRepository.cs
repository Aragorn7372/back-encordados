using BackEncordados.Materials.Model;

namespace BackEncordados.Export.Repository;

public interface IExportMaterialsRepository
{
    Task<List<Material>> GetAllMaterialsAsync();
    Task<List<Cuerdas>> GetAllCuerdasAsync();
    Task ClearMaterialsAsync();
    Task ImportMaterialsAsync(List<Material> materials);
    Task ImportCuerdasAsync(List<Cuerdas> cuerdas);
}
