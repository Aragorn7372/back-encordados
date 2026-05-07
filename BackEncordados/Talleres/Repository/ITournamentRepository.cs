using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Model;

namespace BackEncordados.Talleres.Repository;

public interface ITournamentRepository
{
    Task<(IEnumerable<Tournaments> Items, int TotalCount)> FindAllAsync(FilterTournamentDto filter);
    Task<Tournaments?> FindByIdAsync(long id);
    Task<Tournaments?> FindByNameAsync(string name);
    Task<Tournaments> SaveAsync(Tournaments tournament);
    Task<Tournaments?> UpdateAsync(long id, Tournaments tournament);
    Task<bool> DeleteAsync(long id);
    Task<Tournaments?> AsignWorker(long id,Guid workerId,string machineName);
    Task<Tournaments?> RemoveWorker(long id,Guid workerId);
 
    Task<IEnumerable<WorkerMachineAssignment>?> GetAssignedWorkerMachinesAsync(long tournamentId);
}