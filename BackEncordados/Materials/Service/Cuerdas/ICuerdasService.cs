using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Service.Common;

namespace BackEncordados.Materials.Service.Cuerdas;

public interface ICuerdasService : IProductsService<CuerdaResposeDto, CuerdaError, CuerdaRequestDto, CuerdaPatchDto,CuerdaFilterdto>;
