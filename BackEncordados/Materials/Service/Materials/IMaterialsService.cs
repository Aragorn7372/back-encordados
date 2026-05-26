using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Service.Common;

namespace BackEncordados.Materials.Service.Materials;

/// <summary>
/// Interfaz del servicio específico para <see cref="Material"/>.
/// </summary>
/// <remarks>
/// <para>Hereda de <see cref="IProductsService{T,E,R,P,F}"/> con tipos:
/// <list type="bullet">
///   <item><description>T = <see cref="MaterialResponseDto"/></description></item>
///   <item><description>E = <see cref="MaterialError"/></description></item>
///   <item><description>R = <see cref="MaterialRequestDto"/></description></item>
///   <item><description>P = <see cref="MaterialPatchDto"/></description></item>
///   <item><description>F = <see cref="MaterialFilterDto"/></description></item>
/// </list>
/// </para>
/// </remarks>
public interface
    IMaterialsService : IProductsService<MaterialResponseDto, MaterialError, MaterialRequestDto, MaterialPatchDto,MaterialFilterDto>;
