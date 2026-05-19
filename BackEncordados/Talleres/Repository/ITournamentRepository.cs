using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Model;

namespace BackEncordados.Talleres.Repository;

public interface ITournamentRepository
{
    Task<(IEnumerable<Tournaments> Items, int TotalCount)> FindAllAsync(FilterTournamentDto filter);
    Task<Tournaments?> FindByIdAsync(Ulid id);
    Task<Tournaments?> FindByNameAsync(string name);
    Task<Tournaments> SaveAsync(Tournaments tournament);
    Task<Tournaments?> UpdateAsync(Ulid id, Tournaments tournament);
    Task<bool> DeleteAsync(Ulid id);
    Task<Tournaments?> AsignWorker(Ulid id,Ulid workerId,string machineName);
    Task<Tournaments?> RemoveWorker(Ulid id,Ulid workerId);
  
    Task<IEnumerable<WorkerMachineAssignment>?> GetAssignedWorkerMachinesAsync(Ulid tournamentId);
    Task<Tournaments?> AsignSupervisor(Ulid id,Ulid supervisorId);
    Task<Tournaments?> RemoveSupervisor(Ulid id,Ulid supervisorId);
}