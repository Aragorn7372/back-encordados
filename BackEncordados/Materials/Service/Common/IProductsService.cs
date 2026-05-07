using BackEncordados.Common.Dto;
using CSharpFunctionalExtensions;

namespace BackEncordados.Materials.Service.Common;

public interface IProductsService<T,E,R,P>
{
    Task<PageResponseDto<T>> FindAllAsync(E filter);
    Task<Result<T,E>> FindByIdAsync(long id);
    Task<Result<T,E>> CreateAsync(R request);
    Task<Result<T,E>> UpdateAsync(P request);
    Task<Result<T,E>> DeleteAsync(long id);
}