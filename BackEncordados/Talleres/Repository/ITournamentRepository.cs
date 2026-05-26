using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Model;

namespace BackEncordados.Talleres.Repository;

/// <summary>
/// Contrato del repositorio de torneos que define las operaciones de acceso a datos
/// sobre la entidad <see cref="Tournaments"/>.
/// </summary>
/// <remarks>
/// <para>Define diez métodos que cubren operaciones CRUD y de asignación:</para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Propósito</description>
///   </listheader>
///   <item><term><c>FindAllAsync</c></term><description>Consulta paginada con filtros (por usuario, búsqueda textual, ordenación).</description></item>
///   <item><term><c>FindByIdAsync</c></term><description>Busca torneo por ULID incluyendo asignaciones de máquinas.</description></item>
///   <item><term><c>FindByNameAsync</c></term><description>Busca torneo por nombre exacto incluyendo asignaciones.</description></item>
///   <item><term><c>SaveAsync</c></term><description>Persiste un nuevo torneo.</description></item>
///   <item><term><c>UpdateAsync</c></term><description>Actualización parcial de un torneo existente.</description></item>
///   <item><term><c>DeleteAsync</c></term><description>Eliminación lógica (soft delete) de un torneo.</description></item>
///   <item><term><c>AsignWorker</c></term><description>Asigna un trabajador a una máquina dentro del torneo.</description></item>
///   <item><term><c>RemoveWorker</c></term><description>Desasigna un trabajador y elimina sus asignaciones de máquina.</description></item>
///   <item><term><c>GetAssignedWorkerMachinesAsync</c></term><description>Obtiene todas las asignaciones trabajador-máquina de un torneo.</description></item>
///   <item><term><c>AsignSupervisor</c></term><description>Asigna un supervisor al torneo.</description></item>
///   <item><term><c>RemoveSupervisor</c></term><description>Desasigna un supervisor del torneo.</description></item>
/// </list>
/// </remarks>
public interface ITournamentRepository
{
    /// <summary>Obtiene torneos paginados con filtros por usuario y búsqueda textual.</summary>
    /// <param name="filter">DTO con filtros (Search, UserId, Page, Size, SortBy, Direction).</param>
    /// <returns>Tupla con lista de torneos (<c>Items</c>) y conteo total (<c>TotalCount</c>).</returns>
    Task<(IEnumerable<Tournaments> Items, int TotalCount)> FindAllAsync(FilterTournamentDto filter);
    /// <summary>Busca un torneo por ULID incluyendo las asignaciones trabajador-máquina.</summary>
    /// <param name="id">ULID del torneo.</param>
    /// <returns>Torneo encontrado o <c>null</c> si no existe o está eliminado.</returns>
    Task<Tournaments?> FindByIdAsync(Ulid id);
    /// <summary>Busca un torneo por nombre exacto incluyendo las asignaciones trabajador-máquina.</summary>
    /// <param name="name">Nombre exacto del torneo.</param>
    /// <returns>Torneo encontrado o <c>null</c> si no existe.</returns>
    Task<Tournaments?> FindByNameAsync(string name);
    /// <summary>Persiste un nuevo torneo en la base de datos.</summary>
    /// <param name="tournament">Entidad <see cref="Tournaments"/> a guardar.</param>
    /// <returns>Torneo guardado con su ULID generado.</returns>
    Task<Tournaments> SaveAsync(Tournaments tournament);
    /// <summary>Actualiza parcialmente un torneo existente (nombre, fechas, logotipo).</summary>
    /// <param name="id">ULID del torneo a actualizar.</param>
    /// <param name="tournament">Entidad con campos a actualizar.</param>
    /// <returns>Torneo actualizado o <c>null</c> si no existe.</returns>
    Task<Tournaments?> UpdateAsync(Ulid id, Tournaments tournament);
    /// <summary>Elimina un torneo mediante soft delete.</summary>
    /// <param name="id">ULID del torneo a eliminar.</param>
    /// <returns><c>true</c> si se eliminó, <c>false</c> si no existe o ya estaba eliminado.</returns>
    Task<bool> DeleteAsync(Ulid id);
    /// <summary>Asigna un trabajador a una máquina dentro del torneo.</summary>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="workerId">ULID del trabajador a asignar.</param>
    /// <param name="machineName">Nombre de la máquina asignada.</param>
    /// <returns>Torneo actualizado o <c>null</c> si no existe.</returns>
    Task<Tournaments?> AsignWorker(Ulid id, Ulid workerId, string machineName);
    /// <summary>Desasigna un trabajador del torneo y elimina sus asignaciones de máquina.</summary>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="workerId">ULID del trabajador a desasignar.</param>
    /// <returns>Torneo actualizado o <c>null</c> si no existe.</returns>
    Task<Tournaments?> RemoveWorker(Ulid id, Ulid workerId);
    /// <summary>Obtiene todas las asignaciones trabajador-máquina de un torneo.</summary>
    /// <param name="tournamentId">ULID del torneo.</param>
    /// <returns>Lista de asignaciones o <c>null</c> si el torneo no existe.</returns>
    Task<IEnumerable<WorkerMachineAssignment>?> GetAssignedWorkerMachinesAsync(Ulid tournamentId);
    /// <summary>Asigna un supervisor al torneo.</summary>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="supervisorId">ULID del supervisor a asignar.</param>
    /// <returns>Torneo actualizado o <c>null</c> si no existe.</returns>
    Task<Tournaments?> AsignSupervisor(Ulid id, Ulid supervisorId);
    /// <summary>Desasigna un supervisor del torneo.</summary>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="supervisorId">ULID del supervisor a desasignar.</param>
    /// <returns>Torneo actualizado o <c>null</c> si no existe.</returns>
    Task<Tournaments?> RemoveSupervisor(Ulid id, Ulid supervisorId);
}