using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Model;

namespace BackEncordados.Materials.Repository.Strings;

/// <summary>
/// Interfaz del repositorio específico para <see cref="Cuerdas"/>.
/// </summary>
/// <remarks>
/// <para>Hereda de <see cref="IProductsRepository{T,F}"/> con tipos:
/// <list type="bullet">
///   <item><description>T = <see cref="Cuerdas"/></description></item>
///   <item><description>F = <see cref="CuerdaFilterdto"/></description></item>
/// </list>
/// </para>
/// <para>No agrega métodos adicionales; utiliza la interfaz genérica base.</para>
/// </remarks>
public interface ICuerdasRepository: IProductsRepository<Cuerdas,CuerdaFilterdto>
{
    
}