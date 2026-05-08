using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Materials.Dto.Strings;
using CSharpFunctionalExtensions;

namespace BackEncordados.Materials.Service.Common;

public interface IProductsService<T,E,R,P, F>
{
    Task<PageResponseDto<T>> FindAllAsync(F filter);
    Task<Result<T,E>> FindByNameAsync(string name);
    Task<Result<T,E>> FindByIdAsync(long id);
    Task<Result<T,E>> CreateAsync(R request);
    Task<Result<T, E>> UpdateAsync(long id, P request);
    Task<Result<Unit,E>> DeleteAsync(long id);
}