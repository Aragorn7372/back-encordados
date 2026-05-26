using BackEncordados.Common.Dto;
using BackEncordados.Common.Errors;
using BackEncordados.Common.Utils;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Error;
using CSharpFunctionalExtensions;

namespace BackEncordados.Talleres.Service;

/// <summary>
/// Interfaz del servicio de torneos que define las operaciones de negocio
/// sobre la entidad <see cref="Tournaments"/>.
/// </summary>
/// <remarks>
/// <para>Define doce métodos que cubren operaciones CRUD, asignación de trabajadores/máquinas
/// y gestión de supervisores:</para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Propósito</description>
///   </listheader>
///   <item><term><c>GetTournament</c></term><description>Obtiene detalle completo de un torneo por ULID.</description></item>
///   <item><term><c>GetAllTournamentsAsync</c></term><description>Lista paginada con filtros.</description></item>
///   <item><term><c>CreateTournament</c></term><description>Crea torneo desde administrador (especificando OwnerId).</description></item>
///   <item><term><c>UpdateTournament</c></term><description>Actualización parcial de torneo.</description></item>
///   <item><term><c>DeleteTournament</c></term><description>Elimina torneo (soft delete).</description></item>
///   <item><term><c>AssignWorkerMachine</c></term><description>Asigna trabajador + máquina.</description></item>
///   <item><term><c>UnassignWorkerMachine</c></term><description>Desasigna trabajador y máquina.</description></item>
///   <item><term><c>GetAssignedWorkerMachines</c></term><description>Lista asignaciones de máquinas.</description></item>
///   <item><term><c>GetTournamentByName</c></term><description>Busca torneo por nombre exacto.</description></item>
///   <item><term><c>OwnerCreateTournament</c></term><description>Crea torneo desde propietario (ownerId del JWT).</description></item>
///   <item><term><c>AssingSupervisor</c></term><description>Asigna supervisor a torneo.</description></item>
///   <item><term><c>AnassingSupervisor</c></term><description>Desasigna supervisor de torneo.</description></item>
/// </list>
/// <para>Los métodos que pueden fallar retornan <c>Result&lt;T, DomainErrors&gt;</c> o <c>Result&lt;T, TournamentsErrors&gt;</c>.</para>
/// </remarks>
public interface ITournamentService
{
    /// <summary>Obtiene el detalle completo de un torneo por ULID, incluyendo usuarios, owner y supervisores.</summary>
    /// <param name="id">ULID del torneo.</param>
    /// <returns>DTO detallado del torneo o error.</returns>
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> GetTournament(Ulid id);
    /// <summary>Obtiene una lista paginada de torneos aplicando filtros.</summary>
    /// <param name="filter">DTO con filtros de búsqueda, usuario y paginación.</param>
    /// <returns>Página con DTOs básicos de torneo.</returns>
    Task<PageResponseDto<TournamentResponseDto>> GetAllTournamentsAsync(FilterTournamentDto filter);
    /// <summary>Crea un nuevo torneo (solo administradores, especificando OwnerId).</summary>
    /// <param name="adminRequest">DTO con nombre, OwnerId, fechas y logotipo opcional.</param>
    /// <returns>DTO detallado del torneo creado o error.</returns>
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> CreateTournament(TournamentAdminRequestDto adminRequest);
    /// <summary>Actualiza parcialmente un torneo existente.</summary>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="request">DTO con campos opcionales (nombre, fechas, logotipo).</param>
    /// <returns>DTO básico actualizado o error.</returns>
    Task<Result<TournamentResponseDto, TournamentsErrors>> UpdateTournament(Ulid id, TournamentPatchDto request);
    /// <summary>Elimina un torneo (soft delete).</summary>
    /// <param name="id">ULID del torneo a eliminar.</param>
    /// <returns>Unit en éxito o error si no existe.</returns>
    Task<Result<Unit, TournamentsErrors>> DeleteTournament(Ulid id);
    /// <summary>Asigna un trabajador a una máquina dentro del torneo.</summary>
    /// <param name="tournamentId">ULID del torneo.</param>
    /// <param name="request">DTO con UserId (string) y MachineName.</param>
    /// <returns>DTO detallado del torneo actualizado o error.</returns>
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> AssignWorkerMachine(Ulid tournamentId, WorkerMachineAssignmentRequestDto request);
    /// <summary>Desasigna un trabajador del torneo y elimina sus asignaciones de máquina.</summary>
    /// <param name="tournamentId">ULID del torneo.</param>
    /// <param name="request">ULID del trabajador en formato string.</param>
    /// <returns>DTO detallado del torneo actualizado o error.</returns>
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> UnassignWorkerMachine(Ulid tournamentId, string request);
    /// <summary>Obtiene todas las asignaciones trabajador-máquina de un torneo.</summary>
    /// <param name="tournamentId">ULID del torneo.</param>
    /// <returns>Lista de asignaciones con datos del usuario, o error si el torneo no existe.</returns>
    Task<Result<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>> GetAssignedWorkerMachines(Ulid tournamentId);
    /// <summary>Busca un torneo por su nombre exacto.</summary>
    /// <param name="name">Nombre del torneo.</param>
    /// <returns>DTO detallado del torneo o error.</returns>
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> GetTournamentByName(string name);
    /// <summary>Crea un torneo desde el propietario autenticado (ownerId extraído del JWT).</summary>
    /// <param name="request">DTO con nombre, fechas y logotipo opcional.</param>
    /// <param name="ownerId">ULID del propietario (extraído del token).</param>
    /// <returns>DTO detallado del torneo creado o error.</returns>
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> OwnerCreateTournament(TournamentRequestDto request, Ulid ownerId);
    /// <summary>Asigna un supervisor a un torneo.</summary>
    /// <param name="request">DTO con TournamentId y SupervisorId.</param>
    /// <returns>DTO detallado del torneo actualizado o error.</returns>
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> AssingSupervisor(SupervisorAsignmentRequestDto request);
    /// <summary>Desasigna un supervisor de un torneo.</summary>
    /// <param name="request">DTO con TournamentId y SupervisorId.</param>
    /// <returns>DTO detallado del torneo actualizado o error.</returns>
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> AnassingSupervisor(SupervisorAsignmentRequestDto request);
}