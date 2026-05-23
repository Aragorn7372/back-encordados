using BackEncordados.Common.Database.Config;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Materials.Repository.Materials;

public class MaterialsRepository(ILogger<MaterialsRepository>logger,MaterialsDbContext context):IMaterialsRepository
{
    public async Task<(IEnumerable<Material> Items, int TotalCount)> FindAllAsync(MaterialFilterDto filter)
    {
        logger.LogInformation("Buscando materiales con filtro: search={Marca}, Page={Page}, PageSize={PageSize}", filter.Search,filter.Page,filter.Size);
        var query = context.Materiales.AsQueryable();
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
        
        
        var totalCount = query.Count();
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

    public async Task<Material?> FindByIdAsync(long id)
    {
        logger.LogInformation("Buscando material con ID {Id}", id);
        return await context.Materiales.FindAsync(id);
    }

    public async Task<Material?> FindByNameAsync(string name)
    {
        logger.LogInformation("Buscando material con Name {Name} ", name);
        return await context.Materiales.FirstOrDefaultAsync(m => m.Modelo == name && !m.IsDeleted);
    }

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

    public async Task<Material> CreateAsync(Material item)
    {
        logger.LogInformation("Creando nuevo material: {Marca} {Modelo}", item.Marca, item.Modelo);
        var saved=await context.Materiales.AddAsync(item);
        await context.SaveChangesAsync();
        return saved.Entity;
    }
}