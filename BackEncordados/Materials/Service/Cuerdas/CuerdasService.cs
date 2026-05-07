using BackEncordados.Common.Dto;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Errors;
using CSharpFunctionalExtensions;

namespace BackEncordados.Materials.Service.Cuerdas;

public class CuerdasService:ICuerdasService
{
    public Task<PageResponseDto<CuerdaResposeDto>> FindAllAsync(CuerdaError filter)
    {
        throw new NotImplementedException();
    }

    public Task<Result<CuerdaResposeDto, CuerdaError>> FindByIdAsync(long id)
    {
        throw new NotImplementedException();
    }

    public Task<Result<CuerdaResposeDto, CuerdaError>> CreateAsync(CuerdaRequestDto request)
    {
        throw new NotImplementedException();
    }

    public Task<Result<CuerdaResposeDto, CuerdaError>> UpdateAsync(CuerdaPatchDto request)
    {
        throw new NotImplementedException();
    }

    public Task<Result<CuerdaResposeDto, CuerdaError>> DeleteAsync(long id)
    {
        throw new NotImplementedException();
    }
}