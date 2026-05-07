using BackEncordados.Common.Dto;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Errors;
using CSharpFunctionalExtensions;

namespace BackEncordados.Materials.Service.Materials;

public class MaterialsService:IMaterialsService
{
    public Task<PageResponseDto<MaterialResponseDto>> FindAllAsync(MaterialError filter)
    {
        throw new NotImplementedException();
    }

    public Task<Result<MaterialResponseDto, MaterialError>> FindByIdAsync(long id)
    {
        throw new NotImplementedException();
    }

    public Task<Result<MaterialResponseDto, MaterialError>> CreateAsync(MaterialRequestDto request)
    {
        throw new NotImplementedException();
    }

    public Task<Result<MaterialResponseDto, MaterialError>> UpdateAsync(MaterialPatchDto request)
    {
        throw new NotImplementedException();
    }

    public Task<Result<MaterialResponseDto, MaterialError>> DeleteAsync(long id)
    {
        throw new NotImplementedException();
    }
}