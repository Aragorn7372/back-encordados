using BackEncordados.Materials.Model;

namespace BackEncordados.Excel.Repository;

public interface IMaterialsExcelRepository
{
    Task<List<Material>> GetMaterialsByTournamentAsync(Ulid tournamentId);
    Task<List<Cuerdas>> GetCuerdasByTournamentAsync(Ulid tournamentId);
}
