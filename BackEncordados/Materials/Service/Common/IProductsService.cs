using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Materials.Dto.Strings;
using CSharpFunctionalExtensions;

namespace BackEncordados.Materials.Service.Common;

/// <summary>
/// Interfaz genérica del servicio base para productos (materiales y cuerdas).
/// </summary>
/// <typeparam name="T">Tipo del DTO de respuesta.</typeparam>
/// <typeparam name="E">Tipo del error (hereda de <c>DomainErrors</c>).</typeparam>
/// <typeparam name="R">Tipo del DTO de request para creación.</typeparam>
/// <typeparam name="P">Tipo del DTO de request para actualización.</typeparam>
/// <typeparam name="F">Tipo del DTO de filtro.</typeparam>
/// <remarks>
/// <para>Define las operaciones de negocio estándar para productos del inventario,
/// utilizando el patrón Result de CSharpFunctionalExtensions para manejo de errores.</para>
/// <para><b>Métodos:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Retorno</description>
///     <description>Descripción</description>
///   </listheader>
///   <item>
///     <term><c>FindAllAsync</c></term>
///     <description><see cref="PageResponseDto{T}"/></description>
///     <description>Búsqueda paginada con filtros.</description>
///   </item>
///   <item>
///     <term><c>FindByNameAsync</c></term>
///     <description>Result&lt;T, E&gt;</description>
///     <description>Búsqueda por nombre exacto.</description>
///   </item>
///   <item>
///     <term><c>FindByIdAsync</c></term>
///     <description>Result&lt;T, E&gt;</description>
///     <description>Búsqueda por ID.</description>
///   </item>
///   <item>
///     <term><c>CreateAsync</c></term>
///     <description>Result&lt;T, E&gt;</description>
///     <description>Creación de nuevo producto.</description>
///   </item>
///   <item>
///     <term><c>UpdateAsync</c></term>
///     <description>Result&lt;T, E&gt;</description>
///     <description>Actualización de producto existente.</description>
///   </item>
///   <item>
///     <term><c>DeleteAsync</c></term>
///     <description>Result&lt;Unit, E&gt;</description>
///     <description>Eliminación lógica (soft-delete).</description>
///   </item>
/// </list>
/// </remarks>
public interface IProductsService<T,E,R,P, F>
{
    /// <summary>Búsqueda paginada con filtros y ordenamiento.</summary>
    Task<PageResponseDto<T>> FindAllAsync(F filter);

    /// <summary>Búsqueda por nombre exacto.</summary>
    Task<Result<T,E>> FindByNameAsync(string name);

    /// <summary>Búsqueda por ID numérico.</summary>
    Task<Result<T,E>> FindByIdAsync(long id);

    /// <summary>Creación de un nuevo producto con datos del request.</summary>
    Task<Result<T,E>> CreateAsync(R request);

    /// <summary>Actualización de un producto existente por ID.</summary>
    Task<Result<T, E>> UpdateAsync(long id, P request);

    /// <summary>Eliminación lógica de un producto por ID.</summary>
    Task<Result<Unit,E>> DeleteAsync(long id);
}