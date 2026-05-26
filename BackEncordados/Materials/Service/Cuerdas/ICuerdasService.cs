using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Service.Common;

namespace BackEncordados.Materials.Service.Cuerdas;

/// <summary>
/// Interfaz del servicio específico para <see cref="Cuerdas"/>.
/// </summary>
/// <remarks>
/// <para>Hereda de <see cref="IProductsService{T,E,R,P,F}"/> con tipos:
/// <list type="bullet">
///   <item><description>T = <see cref="CuerdaResponseDto"/></description></item>
///   <item><description>E = <see cref="CuerdaError"/></description></item>
///   <item><description>R = <see cref="CuerdaRequestDto"/></description></item>
///   <item><description>P = <see cref="CuerdaPatchDto"/></description></item>
///   <item><description>F = <see cref="CuerdaFilterdto"/></description></item>
/// </list>
/// </para>
/// </remarks>
public interface ICuerdasService : IProductsService<CuerdaResponseDto, CuerdaError, CuerdaRequestDto, CuerdaPatchDto,CuerdaFilterdto>;
