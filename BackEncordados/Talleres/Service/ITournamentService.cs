using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Error;
using CSharpFunctionalExtensions;

namespace BackEncordados.Talleres.Service;

public interface ITournamentService
{
    Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> GetTournament(long id);
    Task<PageResponseDto<TournamentResponseDto>> GetAllTournamentsAsync(FilterTournamentDto filter);
    Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> CreateTournament(TournamentRequestDto request);
    Task<Result<TournamentResponseDto, TournamentsErrors>> UpdateTournament(long id, TournamentPatchDto request);
    Task<Result<Unit, TournamentsErrors>> DeleteTournament(long id);
    Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> AssignWorkerMachine(long tournamentId, WorkerMachineAssignmentRequestDto request);
    Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> UnassignWorkerMachine(long tournamentId, string request);
    Task<Result<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>> GetAssignedWorkerMachines(long tournamentId);
    Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> GetTournamentByName(string name);
}