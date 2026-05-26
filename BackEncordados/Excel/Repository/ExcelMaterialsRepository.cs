using BackEncordados.Common.Database.Config;
using BackEncordados.Excel.Dto;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Excel.Repository;

public class ExcelMaterialsRepository(MaterialsDbContext materialsDbContext) : IExcelMaterialsRepository
{
    public async Task<List<ExcelMaterialsDto>> GetMaterialsByTournamentAsync(Ulid tournamentId)
    {
        var materials = await materialsDbContext.Materiales
            .AsNoTracking()
            .Where(m => m.TournamentId == tournamentId)
            .ToListAsync();

        return materials.Select(m => new ExcelMaterialsDto
        {
            Id = m.Id,
            TournamentId = m.TournamentId.ToString(),
            Marca = m.Marca,
            Modelo = m.Modelo,
            Stock = m.Stock,
            Precio = m.Precio,
            Type = m.Type.ToString()
        }).ToList();
    }

    public async Task<List<ExcelCuerdasDto>> GetCuerdasByTournamentAsync(Ulid tournamentId)
    {
        var cuerdas = await materialsDbContext.Cuerdas
            .AsNoTracking()
            .Where(c => c.TournamentId == tournamentId)
            .ToListAsync();

        return cuerdas.Select(c => new ExcelCuerdasDto
        {
            Id = c.Id,
            TournamentId = c.TournamentId.ToString(),
            Marca = c.Marca,
            Modelo = c.Modelo,
            Stock = c.Stock,
            Precio = c.Precio,
            Calibre = c.Calibre,
            StringFormat = c.StringFormat.ToString(),
            StringsType = c.StringsType.ToString()
        }).ToList();
    }
}
