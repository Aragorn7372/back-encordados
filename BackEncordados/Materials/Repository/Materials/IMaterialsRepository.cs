using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Model;

namespace BackEncordados.Materials.Repository.Materials;

/// <summary>
/// Interfaz del repositorio específico para <see cref="Material"/>.
/// </summary>
/// <remarks>
/// <para>Hereda de <see cref="IProductsRepository{T,F}"/> con tipos:
/// <list type="bullet">
///   <item><description>T = <see cref="Material"/></description></item>
///   <item><description>F = <see cref="MaterialFilterDto"/></description></item>
/// </list>
/// </para>
/// <para>No agrega métodos adicionales; utiliza la interfaz genérica base.</para>
/// </remarks>
public interface IMaterialsRepository : IProductsRepository<Material, MaterialFilterDto>;
