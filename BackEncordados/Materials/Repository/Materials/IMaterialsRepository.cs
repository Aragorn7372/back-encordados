using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Model;

namespace BackEncordados.Materials.Repository.Materials;

public interface IMaterialsRepository : IProductsRepository<Material, MaterialFilterDto>;
