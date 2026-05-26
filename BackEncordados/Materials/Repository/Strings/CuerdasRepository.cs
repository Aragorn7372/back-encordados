using BackEncordados.Common.Database.Config;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Materials.Repository.Strings;

/// <summary>
/// Repositorio de <see cref="Cuerdas"/> con operaciones CRUD y búsqueda paginada.
/// </summary>
/// <remarks>
/// <para>Implementa <see cref="ICuerdasRepository"/> usando <see cref="MaterialsDbContext"/>.</para>
/// <para><b>Funcionalidades:</b></para>
/// <list type="bullet">
///   <item><description>Búsqueda paginada con filtros por Marca, Modelo, StringFormat, StringsType.</description></item>
///   <item><description>Ordenamiento dinámico por cualquier campo incluyendo Calibre, StringFormat, StringsType.</description></item>
///   <item><description>Soft-delete: todas las consultas excluyen <c>IsDeleted=true</c>.</description></item>
///   <item><description>Actualización parcial: solo sobrescribe campos no vacíos/positivos.</description></item>
/// </list>
/// </remarks>
/// <param name="logger">Logger para seguimiento de operaciones de base de datos.</param>
/// <param name="context">DbContext del módulo Materials que contiene las tablas Cuerdas y Materiales.</param>
public class CuerdasRepository(ILogger<CuerdasRepository>logger, MaterialsDbContext context): ICuerdasRepository
{
    /// <summary>
    /// Busca cuerdas con filtros, paginación y ordenamiento dinámico.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Inicia consulta sobre <c>context.Cuerdas</c> con <c>AsNoTracking()</c>.</description></item>
    ///   <item><description>Filtra registros no eliminados (<c>IsDeleted == false</c>).</description></item>
    ///   <item><description>Si <c>TournamentId</c> está presente, filtra por torneo específico.</description></item>
    ///   <item><description>Si hay texto de búsqueda (<c>Search</c>), aplica <c>LIKE</c> sobre Marca, Modelo, StringFormat (string), StringsType (string) e Id.</description></item>
    ///   <item><description>Cuenta total de registros sin paginar para el paginador.</description></item>
    ///   <item><description>Aplica ordenamiento dinámico según <c>SortBy</c> y <c>Direction</c> (soporta Marca, Modelo, Stock, Precio, Calibre, StringFormat, StringsType e Id como default).</description></item>
    ///   <item><description>Aplica paginación (<c>Skip</c> + <c>Take</c>) y ejecuta la consulta.</description></item>
    /// </list>
    /// <para><b>Casos borde:</b></para>
    /// <list type="bullet">
    ///   <item><description>Si no hay cuerdas activas → retorna lista vacía y TotalCount = 0.</description></item>
    ///   <item><description>Si <c>Search</c> no coincide con ningún campo → mismo resultado que sin filtro.</description></item>
    ///   <item><description>Si <c>SortBy</c> no coincide con ningún campo conocido → ordena por Id (default).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="filter">DTO con filtros (TournamentId, Search, Page, Size, SortBy, Direction).</param>
    /// <returns>Tupla con la lista de cuerdas (<c>Items</c>) y el total de registros sin paginar (<c>TotalCount</c>).</returns>
    public async Task<(IEnumerable<Cuerdas> Items, int TotalCount)> FindAllAsync(CuerdaFilterdto filter)
    {
        logger.LogInformation("Buscando cuerdas con filtro: search={Marca}, Page={Page}, PageSize={PageSize}", filter.Search,filter.Page,filter.Size);
        var query = context.Cuerdas.AsNoTracking().AsQueryable();
        query = query.Where(c=>!c.IsDeleted);
        if (filter.TournamentId != null) 
            query = query.Where(c => c.TournamentId == filter.TournamentId);
            
        if (!string.IsNullOrEmpty(filter.Search))
        {
            query = query.Where(c =>EF.Functions.Like(c.Marca,$"%{filter.Search}%")
            ||EF.Functions.Like(c.Modelo,$"%{filter.Search}%")
            || EF.Functions.Like(c.StringFormat.ToString(),$"%{filter.Search}%")
            || EF.Functions.Like(c.StringsType.ToString(),$"%{filter.Search}%")
            || EF.Functions.Like(c.Id.ToString(),$"%{filter.Search}%"));
            
            }
        
        
        var totalCount = await query.CountAsync();
        bool isDesc= filter.Direction.ToLower() == "desc";
        query = filter.SortBy.ToLower() switch
        {
            "marca" => isDesc ? query.OrderByDescending(c => c.Marca) : query.OrderBy(c => c.Marca),
            "modelo" => isDesc ? query.OrderByDescending(c => c.Modelo) : query.OrderBy(c => c.Modelo),
            "stock" => isDesc ? query.OrderByDescending(c => c.Stock) : query.OrderBy(c => c.Stock),
            "precio" => isDesc ? query.OrderByDescending(c => c.Precio) : query.OrderBy(c => c.Precio),
            "calibre" => isDesc ? query.OrderByDescending(c => c.Calibre) : query.OrderBy(c => c.Calibre),
            "stringformat" => isDesc ? query.OrderByDescending(c => c.StringFormat) : query.OrderBy(c => c.StringFormat),
            "stringstype" => isDesc ? query.OrderByDescending(c => c.StringsType) : query.OrderBy(c => c.StringsType),
            _ => isDesc ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id)
        };
        var items = await query.Skip(filter.Page * filter.Size).Take(filter.Size).ToListAsync();
        return (Items: items, TotalCount: totalCount);
    }

    /// <summary>
    /// Busca una cuerda por su ID numérico, excluyendo registros eliminados lógicamente.
    /// </summary>
    /// <remarks>
    /// <para>Usa <c>AsNoTracking()</c> por ser una operación de solo lectura.
    /// La consulta incluye el filtro <c>!IsDeleted</c> para respetar soft-delete.</para>
    /// <para>Si la cuerda fue eliminada lógicamente, retorna <c>null</c>.</para>
    /// </remarks>
    /// <param name="id">ID numérico de la cuerda a buscar.</param>
    /// <returns>Entidad <see cref="Cuerdas"/> si existe y no fue eliminada; <c>null</c> en caso contrario.</returns>
    public async Task<Cuerdas?> FindByIdAsync(long id)
    {
        logger.LogInformation("Buscando cuerda con ID {Id}", id);
        return await context.Cuerdas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
    }

    /// <summary>
    /// Busca cuerdas por marca o modelo exacto.
    /// </summary>
    /// <remarks>
    /// <para>Usa <c>AsNoTracking()</c> por ser solo lectura.
    /// La comparación es exacta (<c>==</c>) contra <c>Marca</c> O <c>Modelo</c>.
    /// Si <paramref name="name"/> coincide con Marca de una cuerda y con Modelo de otra,
    /// retorna la primera encontrada (orden indeterminado).</para>
    /// </remarks>
    /// <param name="name">Valor a buscar (puede coincidir con Marca o Modelo).</param>
    /// <returns>Entidad <see cref="Cuerdas"/> si existe; <c>null</c> en caso contrario.</returns>
    public async Task<Cuerdas?> FindByNameAsync(string name)
    {
        logger.LogInformation("Buscando cuerda con Name {Name} ", name);
        return await context.Cuerdas.AsNoTracking().FirstOrDefaultAsync(c => (c.Marca == name || c.Modelo == name) && !c.IsDeleted);
    }

    /// <summary>
    /// Actualiza parcialmente una cuerda existente.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca la cuerda por <paramref name="id"/> usando <c>FindAsync</c> (con tracking).</description></item>
    ///   <item><description>Si no existe → retorna <c>null</c> sin lanzar excepción.</description></item>
    ///   <item><description>Sobrescribe campos condicionalmente:</description></item>
    ///   <list type="bullet">
    ///     <item><description><c>Marca</c> — solo si <c>!string.IsNullOrEmpty</c>.</description></item>
    ///     <item><description><c>Modelo</c> — solo si <c>!string.IsNullOrEmpty</c>.</description></item>
    ///     <item><description><c>Stock</c> — solo si >= 0.</description></item>
    ///     <item><description><c>Precio</c> — solo si >= 0.</description></item>
    ///     <item><description><c>Calibre</c> — solo si > 0.</description></item>
    ///     <item><description><c>StringFormat</c> y <c>StringsType</c> — siempre se sobrescriben.</description></item>
    ///     <item><description><c>ImageUrl</c> y <c>CloudinaryPublicId</c> — solo si no vacíos.</description></item>
    ///   </list>
    ///   <item><description>Marca la entidad como modificada con <c>Update</c> y persiste.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="item">Entidad <see cref="Cuerdas"/> con los campos a actualizar.</param>
    /// <param name="id">ID de la cuerda a actualizar.</param>
    /// <returns>Entidad actualizada si existe; <c>null</c> si la cuerda no fue encontrada.</returns>
    public async Task<Cuerdas?> UpdateAsync(Cuerdas item, long id)
    {
        logger.LogInformation("actualizando cuerda con ID {Id}", id);
        var cuerda= await context.Cuerdas.FindAsync(id);
        if (cuerda == null) return null;
        if (!string.IsNullOrEmpty(item.Marca)) cuerda.Marca = item.Marca;
        if (!string.IsNullOrEmpty(item.Modelo)) cuerda.Modelo = item.Modelo;
        if (item.Stock >= 0) cuerda.Stock = item.Stock;
        if (item.Precio >= 0) cuerda.Precio = item.Precio;
        if (item.Calibre > 0) cuerda.Calibre = item.Calibre;
        cuerda.StringFormat = item.StringFormat;
        cuerda.StringsType = item.StringsType;
        if (!string.IsNullOrEmpty(item.ImageUrl)) cuerda.ImageUrl = item.ImageUrl;
        if (!string.IsNullOrEmpty(item.CloudinaryPublicId)) cuerda.CloudinaryPublicId = item.CloudinaryPublicId;
        var cuerdaSaved=context.Cuerdas.Update(cuerda);
        await context.SaveChangesAsync();
        return cuerdaSaved.Entity;
    }

    /// <summary>
    /// Elimina lógicamente una cuerda (soft-delete) por su ID.
    /// </summary>
    /// <remarks>
    /// <para>En lugar de borrar el registro físicamente, establece <c>IsDeleted = true</c>.
    /// Esto preserva la integridad referencial con pedidos históricos que referencien
    /// a la cuerda.</para>
    /// <para>Si la cuerda no existe (o ya fue eliminada), retorna <c>false</c>.</para>
    /// </remarks>
    /// <param name="id">ID de la cuerda a eliminar lógicamente.</param>
    /// <returns><c>true</c> si se marcó como eliminada; <c>false</c> si no existe.</returns>
    public async Task<bool> DeleteAsync(long id)
    {
        logger.LogInformation("Eliminando cuerda con ID {Id}", id);
        var cuerda = await context.Cuerdas.FindAsync(id);
        if (cuerda == null) return false;
        cuerda.IsDeleted = true;
        context.Cuerdas.Update(cuerda);
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Crea una nueva cuerda y retorna la entidad persistida con su ID asignado.
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
    /// <param name="item">Entidad <see cref="Cuerdas"/> con los datos a persistir.</param>
    /// <returns>Entidad persistida con su ID generado.</returns>
    public async Task<Cuerdas> CreateAsync(Cuerdas item)
    {
        logger.LogInformation("Creando nueva cuerda: {Marca} {Modelo}", item.Marca, item.Modelo);
        var saved=await context.Cuerdas.AddAsync(item);
        await context.SaveChangesAsync();
        return saved.Entity;
    }
}