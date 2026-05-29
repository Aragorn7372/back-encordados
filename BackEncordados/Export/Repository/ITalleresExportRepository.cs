using BackEncordados.Talleres.Model;

namespace BackEncordados.Export.Repository;

public interface ITalleresExportRepository
{
    Task<List<Tournaments>> GetTournamentsDataAsync();
    Task ClearTournamentsAsync();
    Task ImportTournamentsAsync(List<Tournaments> tournaments);
}
