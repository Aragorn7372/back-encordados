using BackEncordados.Talleres.Model;

namespace BackEncordados.Export.Repository;

public interface IExportTalleresRepository
{
    Task<List<Tournaments>> GetAllTournamentsAsync();
    Task ClearTournamentsAsync();
    Task ImportTournamentsAsync(List<Tournaments> tournaments);
}
