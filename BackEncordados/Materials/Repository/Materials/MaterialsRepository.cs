using BackEncordados.Common.Database.Config;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Materials.Repository.Materials;

/// <summary>
/// Repositorio de <see cref="Material"/> con operaciones CRUD y búsqueda paginada.
/// </summary>
/// <remarks>
/// <para>Implementa <see cref="IMaterialsRepository"/> usando <see cref="MaterialsDbContext"/>.</para>
/// <para><b>Funcionalidades:</b></para>
/// <list type="bullet">
///   <item><description>Búsqueda paginada con filtros por Marca, Modelo, Type, Stock, Precio.</description></item>
///   <item><description>Ordenamiento dinámico por cualquier campo (asc/desc).</description></item>
///   <item><description>Soft-delete: todas las consultas excluyen <c>IsDeleted=true</c>.</description></item>
///   <item><description>Actualización parcial: solo sobrescribe campos no vacíos/positivos.</description></item>
/// </list>
/// </remarks>
/// <param name="logger">Logger para seguimiento de operaciones de base de datos.</param>
/// <param name="context">DbContext del módulo Materials que contiene las tablas Materiales y Cuerdas.</param>
public class MaterialsRepository(ILogger<MaterialsRepository>logger,MaterialsDbContext context):IMaterialsRepository
{
    /// <summary>
    /// Busca materiales con filtros, paginación y ordenamiento dinámico.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Inicia consulta sobre <c>context.Materiales</c> con <c>AsNoTracking()</c>.</description></item>
    ///   <item><description>Filtra registros no eliminados (<c>IsDeleted == false</c>).</description></item>
    ///   <item><description>Si <c>TournamentId</c> está presente, filtra por torneo específico.</description></item>
    ///   <item><description>Si hay texto de búsqueda (<c>Search</c>), aplica <c>LIKE</c> sobre Marca, Modelo, Type (string) e Id.</description></item>
    ///   <item><description>Cuenta total de registros sin paginar para el paginador.</description></item>
    ///   <item><description>Aplica ordenamiento dinámico según <c>SortBy</c> y <c>Direction</c> (soporta Marca, Modelo, Stock, Precio e Id como default).</description></item>
    ///   <item><description>Aplica paginación (<c>Skip</c> + <c>Take</c>) y ejecuta la consulta.</description></item>
    /// </list>
    /// <para><b>Casos borde:</b></para>
    /// <list type="bullet">
    ///   <item><description>Si no hay materiales activos → retorna lista vacía y TotalCount = 0.</description></item>
    ///   <item><description>Si <c>Search</c> no coincide con ningún campo → mismo resultado que sin filtro.</description></item>
    ///   <item><description>Si <c>SortBy</c> no coincide con ningún campo conocido → ordena por Id (default).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="filter">DTO con filtros (TournamentId, Search, Page, Size, SortBy, Direction).</param>
    /// <returns>Tupla con la lista de materiales (<c>Items</c>) y el total de registros sin paginar (<c>TotalCount</c>).</returns>
    public async Task<(IEnumerable<Material> Items, int TotalCount)> FindAllAsync(MaterialFilterDto filter)
    {
        logger.LogInformation("Buscando materiales con filtro: search={Marca}, Page={Page}, PageSize={PageSize}", filter.Search,filter.Page,filter.Size);
        var query = context.Materiales.AsNoTracking().AsQueryable();
        query = query.Where(m=>!m.IsDeleted);
        if(filter.TournamentId != null)
            query = query.Where(m => m.TournamentId == filter.TournamentId);
        if (!string.IsNullOrEmpty(filter.Search))
        {
            query = query.Where(m =>
                EF.Functions.Like(m.Marca,$"%{filter.Search}%")
                ||EF.Functions.Like(m.Modelo,$"%{filter.Search}%")
                || EF.Functions.Like(m.Type.ToString(),$"%{filter.Search}%")
                || EF.Functions.Like(m.Id.ToString(),$"%{filter.Search}%"));
            
        }
        
        
        var totalCount = await query.CountAsync();
        bool isDesc= filter.Direction.ToLower() == "desc";
        query = filter.SortBy.ToLower() switch
        {
            "marca" => isDesc ? query.OrderByDescending(m => m.Marca) : query.OrderBy(m => m.Marca),
            "modelo" => isDesc ? query.OrderByDescending(m => m.Modelo) : query.OrderBy(m => m.Modelo),
            "stock" => isDesc ? query.OrderByDescending(m => m.Stock) : query.OrderBy(m => m.Stock),
            "precio" => isDesc ? query.OrderByDescending(m => m.Precio) : query.OrderBy(m => m.Precio),
            _ => isDesc ? query.OrderByDescending(m => m.Id) : query.OrderBy(m => m.Id)
        };
        var items = await query.Skip(filter.Page * filter.Size).Take(filter.Size).ToListAsync();
        return (Items: items, TotalCount: totalCount);
    }

    /// <summary>
    /// Busca un material por su ID numérico, excluyendo registros eliminados lógicamente.
    /// </summary>
    /// <remarks>
    /// <para>Usa <c>AsNoTracking()</c> por ser una operación de solo lectura.
    /// La consulta incluye el filtro <c>!IsDeleted</c> para respetar soft-delete.</para>
    /// <para>Si el material fue eliminado lógicamente (<c>IsDeleted = true</c>), el método retorna <c>null</c>
    /// como si no existiera.</para>
    /// </remarks>
    /// <param name="id">ID numérico del material a buscar.</param>
    /// <returns>Entidad <see cref="Material"/> si existe y no fue eliminada; <c>null</c> en caso contrario.</returns>
    public async Task<Material?> FindByIdAsync(long id)
    {
        logger.LogInformation("Buscando material con ID {Id}", id);
        return await context.Materiales.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    /// <summary>
    /// Busca un material por el nombre exacto del modelo.
    /// </summary>
    /// <remarks>
    /// <para>Usa <c>AsNoTracking()</c> por ser solo lectura.
    /// La comparación es exacta (<c>==</c>), no parcial.</para>
    /// <para>Ejemplo: si <paramref name="name"/> = "Pro Overgrip", busca materiales
    /// cuyo <c>Modelo</c> sea exactamente "Pro Overgrip" y no estén eliminados.</para>
    /// </remarks>
    /// <param name="name">Nombre exacto del modelo a buscar.</param>
    /// <returns>Entidad <see cref="Material"/> si existe y no fue eliminada; <c>null</c> en caso contrario.</returns>
    public async Task<Material?> FindByNameAsync(string name)
    {
        logger.LogInformation("Buscando material con Name {Name} ", name);
        return await context.Materiales.AsNoTracking().FirstOrDefaultAsync(m => m.Modelo == name && !m.IsDeleted);
    }

    /// <summary>
    /// Actualiza parcialmente un material existente.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el material por <paramref name="id"/> usando <c>FindAsync</c> (con tracking).</description></item>
    ///   <item><description>Si no existe → retorna <c>null</c> sin lanzar excepción.</description></item>
    ///   <item><description>Sobrescribe campos solo si el valor entrante no está vacío/null o es >= 0:</description></item>
    ///   <list type="bullet">
    ///     <item><description><c>Marca</c> — solo si <c>!string.IsNullOrEmpty</c>.</description></item>
    ///     <item><description><c>Modelo</c> — solo si <c>!string.IsNullOrEmpty</c>.</description></item>
    ///     <item><description><c>Stock</c> — solo si >= 0.</description></item>
    ///     <item><description><c>Precio</c> — solo si >= 0.</description></item>
    ///     <item><description><c>Type</c> — siempre se sobrescribe (aunque sea igual).</description></item>
    ///     <item><description><c>ImageUrl</c> — solo si <c>!string.IsNullOrEmpty</c>.</description></item>
    ///     <item><description><c>CloudinaryPublicId</c> — solo si <c>!string.IsNullOrEmpty</c>.</description></item>
    ///   </list>
    ///   <item><description>Marca la entidad como modificada con <c>Update</c> y persiste.</description></item>
    /// </list>
    /// <para><b>Nota:</b> <c>Type</c> se sobrescribe siempre porque es un enum value-type
    /// sin estado "no definido". Los demás campos usan valores centinela para decidir
    /// si deben actualizarse.</para>
    /// </remarks>
    /// <param name="item">Entidad <see cref="Material"/> con los campos a actualizar (puede contener solo los campos modificados).</param>
    /// <param name="id">ID del material a actualizar.</param>
    /// <returns>Entidad actualizada si existe; <c>null</c> si el material no fue encontrado.</returns>
    public async Task<Material?> UpdateAsync(Material item, long id)
    {
        logger.LogInformation("Buscando material con ID {Id}", id);
        var material= await context.Materiales.FindAsync(id);
        if (material == null) return null;
        if (!string.IsNullOrEmpty(item.Marca)) material.Marca = item.Marca;
        if (!string.IsNullOrEmpty(item.Modelo)) material.Modelo = item.Modelo;
        if (item.Stock >= 0) material.Stock = item.Stock;
        if (item.Precio >= 0) material.Precio = item.Precio;
        material.Type = item.Type;
        if (!string.IsNullOrEmpty(item.ImageUrl)) material.ImageUrl = item.ImageUrl;
        if (!string.IsNullOrEmpty(item.CloudinaryPublicId)) material.CloudinaryPublicId = item.CloudinaryPublicId;
        context.Materiales.Update(material);
        await context.SaveChangesAsync();
        return material;
    }

    /// <summary>
    /// Elimina lógicamente un material (soft-delete) por su ID.
    /// </summary>
    /// <remarks>
    /// <para>En lugar de borrar el registro físicamente, establece <c>IsDeleted = true</c>.
    /// Esto preserva la integridad referencial con pedidos históricos que referencien
    /// al material.</para>
    /// <para>Si el material no existe (o ya fue eliminado), retorna <c>false</c>.</para>
    /// </remarks>
    /// <param name="id">ID del material a eliminar lógicamente.</param>
    /// <returns><c>true</c> si se marcó como eliminado; <c>false</c> si el material no existe.</returns>
    public async Task<bool> DeleteAsync(long id)
    {
        logger.LogInformation("Eliminando material con ID {Id}", id);
        var material = await context.Materiales.FindAsync(id);
        if (material == null) return false;
        material.IsDeleted = true;
        context.Materiales.Update(material);
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Crea un nuevo material y retorna la entidad persistida con su ID asignado.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Agrega la entidad al DbSet con <c>AddAsync</c>.</description></item>
    ///   <item><description>Persiste los cambios con <c>SaveChangesAsync</c>.</description></item>
    ///   <item><description>Retorna la entidad con el <c>Id</c> autogenerado por la BD.</description></item>
    /// </list>
    /// <para><b>Nota:</b> El <c>Id</c> se asigna automáticamente (autoincremental).
    /// Las propiedades <c>CreatedAt</c> y <c>UpdatedAt</c> se establecen por defecto
    /// en el constructor de la entidad como <c>DateTime.UtcNow</c>.</para>
    /// </remarks>
    /// <param name="item">Entidad <see cref="Material"/> con los datos a persistir.</param>
    /// <returns>Entidad persistida con su ID generado.</returns>
    public async Task<Material> CreateAsync(Material item)
    {
        logger.LogInformation("Creando nuevo material: {Marca} {Modelo}", item.Marca, item.Modelo);
        var saved=await context.Materiales.AddAsync(item);
        await context.SaveChangesAsync();
        return saved.Entity;
    }
}