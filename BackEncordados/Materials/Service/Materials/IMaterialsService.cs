using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Service.Common;

namespace BackEncordados.Materials.Service.Materials;

public interface
    IMaterialsService : IProductsService<MaterialResponseDto, MaterialError, MaterialRequestDto, MaterialPatchDto>;
