namespace BackEncordados.Materials.Repository;

/// <summary>
/// Interfaz genérica del repositorio base para productos (materiales y cuerdas).
/// </summary>
/// <typeparam name="T">Tipo de la entidad (Material o Cuerdas).</typeparam>
/// <typeparam name="F">Tipo del DTO de filtro para búsqueda paginada.</typeparam>
/// <remarks>
/// <para>Define las operaciones CRUD estándar para productos del inventario.</para>
/// <para><b>Métodos:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Descripción</description>
///   </listheader>
///   <item>
///     <term><c>FindAllAsync</c></term>
///     <description>Búsqueda paginada con filtros y ordenamiento.</description>
///   </item>
///   <item>
///     <term><c>FindByIdAsync</c></term>
///     <description>Búsqueda por ID (incluye soft-delete check).</description>
///   </item>
///   <item>
///     <term><c>FindByNameAsync</c></term>
///     <description>Búsqueda por nombre/modelo exacto.</description>
///   </item>
///   <item>
///     <term><c>UpdateAsync</c></term>
///     <description>Actualización parcial de campos.</description>
///   </item>
///   <item>
///     <term><c>DeleteAsync</c></term>
///     <description>Eliminación lógica (soft-delete).</description>
///   </item>
///   <item>
///     <term><c>CreateAsync</c></term>
///     <description>Creación de nueva entidad.</description>
///   </item>
/// </list>
/// </remarks>
public interface IProductsRepository<T,F>
{
    /// <summary>Búsqueda paginada con filtros. Retorna items y total de registros.</summary>
    Task<(IEnumerable<T> Items, int TotalCount)> FindAllAsync(F filter);

    /// <summary>Búsqueda por ID. Retorna null si no existe o fue eliminado.</summary>
    Task<T?> FindByIdAsync(long id);

    /// <summary>Búsqueda por nombre/modelo exacto. Retorna null si no encuentra.</summary>
    Task<T?> FindByNameAsync(string name);

    /// <summary>Actualización parcial de la entidad. Retorna null si no existe.</summary>
    Task<T?> UpdateAsync(T item,long id);

    /// <summary>Eliminación lógica (soft-delete). Retorna true si se eliminó.</summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>Creación de nueva entidad. Retorna la entidad persistida.</summary>
    Task<T> CreateAsync(T item);
}