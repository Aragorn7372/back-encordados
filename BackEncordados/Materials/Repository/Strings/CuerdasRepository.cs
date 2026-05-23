using BackEncordados.Common.Database.Config;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Materials.Repository.Strings;

public class CuerdasRepository(ILogger<CuerdasRepository>logger, MaterialsDbContext context): ICuerdasRepository
{
    public async Task<(IEnumerable<Cuerdas> Items, int TotalCount)> FindAllAsync(CuerdaFilterdto filter)
    {
        logger.LogInformation("Buscando cuerdas con filtro: search={Marca}, Page={Page}, PageSize={PageSize}", filter.Search,filter.Page,filter.Size);
        var query = context.Cuerdas.AsQueryable();
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
            "stringformat" => isDesc ? query.OrderByDescending(c => c.StringFormat) : query.OrderBy(c => c.StringFormat),
            "stringstype" => isDesc ? query.OrderByDescending(c => c.StringsType) : query.OrderBy(c => c.StringsType),
            _ => isDesc ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id)
        };
        var items = await query.Skip(filter.Page * filter.Size).Take(filter.Size).ToListAsync();
        return (Items: items, TotalCount: totalCount);
    }

    public async Task<Cuerdas?> FindByIdAsync(long id)
    {
        logger.LogInformation("Buscando cuerda con ID {Id}", id);
        return await context.Cuerdas.FindAsync(id);
    }

    public async Task<Cuerdas?> FindByNameAsync(string name)
    {
        logger.LogInformation("Buscando cuerda con Name {Name} ", name);
        return await context.Cuerdas.FirstOrDefaultAsync(c => c.Marca == name || c.Modelo == name);
    }

    public async Task<Cuerdas?> UpdateAsync(Cuerdas item, long id)
    {
        logger.LogInformation("actualizando cuerda con ID {Id}", id);
        var cuerda= await context.Cuerdas.FindAsync(id);
        if (cuerda == null) return null;
        if (!string.IsNullOrEmpty(item.Marca)) cuerda.Marca = item.Marca;
        if (!string.IsNullOrEmpty(item.Modelo)) cuerda.Modelo = item.Modelo;
        if (item.Stock >= 0) cuerda.Stock = item.Stock;
        if (item.Precio >= 0) cuerda.Precio = item.Precio;
        cuerda.StringFormat = item.StringFormat;
        cuerda.StringsType = item.StringsType;
        if (!string.IsNullOrEmpty(item.ImageUrl)) cuerda.ImageUrl = item.ImageUrl;
        if (!string.IsNullOrEmpty(item.CloudinaryPublicId)) cuerda.CloudinaryPublicId = item.CloudinaryPublicId;
        var cuerdaSaved=context.Cuerdas.Update(cuerda);
        await context.SaveChangesAsync();
        return cuerdaSaved.Entity;
    }

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

    public async Task<Cuerdas> CreateAsync(Cuerdas item)
    {
        logger.LogInformation("Creando nueva cuerda: {Marca} {Modelo}", item.Marca, item.Modelo);
        var saved=await context.Cuerdas.AddAsync(item);
        await context.SaveChangesAsync();
        return saved.Entity;
    }
}