using BackEncordados.Common.Dto;
using BackEncordados.Common.Errors;
using BackEncordados.Common.Utils;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Error;
using CSharpFunctionalExtensions;

namespace BackEncordados.Talleres.Service;

public interface ITournamentService
{
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> GetTournament(Ulid id);
    Task<PageResponseDto<TournamentResponseDto>> GetAllTournamentsAsync(FilterTournamentDto filter);
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> CreateTournament(TournamentAdminRequestDto adminRequest);
    Task<Result<TournamentResponseDto, TournamentsErrors>> UpdateTournament(Ulid id, TournamentPatchDto request);
    Task<Result<Unit, TournamentsErrors>> DeleteTournament(Ulid id);
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> AssignWorkerMachine(Ulid tournamentId, WorkerMachineAssignmentRequestDto request);
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> UnassignWorkerMachine(Ulid tournamentId, string request);
    Task<Result<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>> GetAssignedWorkerMachines(Ulid tournamentId);
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> GetTournamentByName(string name);
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> OwnerCreateTournament(TournamentRequestDto request, Ulid ownerId);
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> AssingSupervisor(SupervisorAsignmentRequestDto request);
    Task<Result<TournamentResponseDetailsDto, DomainErrors>> AnassingSupervisor(SupervisorAsignmentRequestDto request);
}