using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Error;
using BackEncordados.Talleres.Mapper;
using BackEncordados.Talleres.Repository;
using BackEncordados.Usuarios.Mapper;
using BackEncordados.Usuarios.Repository;
using CSharpFunctionalExtensions;

namespace BackEncordados.Talleres.Service;

public class TournamentService(ILogger<TournamentService> logger,ITournamentRepository repository, IUserRepository userRepository): ITournamentService
{
    public async Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> GetTournament(long id)
    {
        logger.LogInformation("Getting tournament {Id}", id);
        var tournament = await repository.FindByIdAsync(id);
        if (tournament is null)
        {
            logger.LogWarning("Tournament with id {Id} not found", id);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new TournamentNotFoundError());
        }
        var userTasks = tournament.WorkersList.Select(w => userRepository.FindByIdAsync(w));
        var fetchedUsers = await Task.WhenAll(userTasks);

        var validUsers = fetchedUsers
                .Where(u => u != null)
                .Select(u => u!.ToDto()) 
            .ToList();

        var responseDto = tournament.ToTournamentResponseDetailsDto(validUsers);
        return Result.Success<TournamentResponseDetailsDto, TournamentsErrors>(responseDto);
    }

    public async Task<PageResponseDto<TournamentResponseDto>> GetAllTournamentsAsync(FilterTournamentDto filter)
    {
        var paged= await repository.FindAllAsync(filter);
        int totalPages = filter.Size > 0 ? (int)Math.Ceiling(paged.TotalCount / (double)filter.Size) : 0;
        return new PageResponseDto<TournamentResponseDto>(
            Content: paged.Items.Select(item => item.ToTournamentResponseDto()).ToList(),
            TotalPages: totalPages,
            TotalElements: paged.TotalCount,
            PageSize: filter.Size,
            PageNumber: filter.Page,    
            TotalPageElements: paged.Items.Count(),
            SortBy: filter.SortBy,
            Direction: filter.Direction
        );
    }

    public async Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> CreateTournament(
        TournamentRequestDto request)
    {
        logger.LogInformation("Creating tournament with title {Title}", request.Name);
        var saved = await repository.SaveAsync(request.ToTournaments());
        var userTasks = saved.WorkersList.Select(w => userRepository.FindByIdAsync(w));
        var fetchedUsers = await Task.WhenAll(userTasks);

        var validUsers = fetchedUsers
            .Where(u => u != null)
            .Select(u => u!.ToDto())
            .ToList();

        var responseDto = saved.ToTournamentResponseDetailsDto(validUsers);
        return Result.Success<TournamentResponseDetailsDto, TournamentsErrors>(responseDto);
    }

    public async Task<Result<TournamentResponseDto, TournamentsErrors>> UpdateTournament(long id, TournamentPatchDto request)
    {
        logger.LogInformation("Updating tournament {Id}", id);
        var oldTournament =await repository.FindByIdAsync(id);
        if (oldTournament is null)
        {
            logger.LogWarning("Tournament with id {Id} not found", id);
            return Result.Failure<TournamentResponseDto, TournamentsErrors>(
                    new TournamentNotFoundError());
        }

        if (request.EndTournament != null) 
            oldTournament.EndTournament = request.EndTournament.Value;
        

        if (request.StartTournament != null)
            oldTournament.StartTournament = request.StartTournament.Value;
        if (request.Name != null || request.Name?.Trim().Length > 0)
            oldTournament.Title = request.Name;
        if(request.Logotype != null || request.Logotype?.Trim().Length > 0)
            oldTournament.Logotype = request.Logotype;
        return await repository.UpdateAsync(id, oldTournament) is {} updated
            ? Result.Success<TournamentResponseDto, TournamentsErrors>(updated.ToTournamentResponseDto())
            : Result.Failure<TournamentResponseDto, TournamentsErrors>(new ConflictError("Error updating tournament"));
    }

    public async Task<Result<Unit, TournamentsErrors>> DeleteTournament(long id)
    {
        logger.LogInformation("Deleting tournament {Id}", id);
        return await repository.DeleteAsync(id) 
            ? Result.Success<Unit, TournamentsErrors>(Unit.Value) 
            : Result.Failure<Unit, TournamentsErrors>(new TournamentNotFoundError());
    }

    public async Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> AssignWorkerMachine(long tournamentId, WorkerMachineAssignmentRequestDto request)
    {
        logger.LogInformation("Assigning worker machine {Id}", tournamentId);
        var workerUlid= Ulid.TryParse(request.UserId, out var ulid) ? ulid : Ulid.Empty;
        if (workerUlid == Ulid.Empty)        {
            logger.LogWarning("Invalid user id {Id}", request.UserId);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new ValidationError("Invalid user id"));
        }
        var tournamentUpdated =await repository.AsignWorker(tournamentId, workerUlid, request.MachineName);
        if (tournamentUpdated is null)
        {
            logger.LogWarning("Tournament with id {Id} not found", tournamentId);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new TournamentNotFoundError());
        }
        var users= tournamentUpdated.WorkersList.Select(async w => await userRepository.FindByIdAsync(w)).Where(u => u != null).ToList();
        var userTasks = tournamentUpdated.WorkersList.Select(w => userRepository.FindByIdAsync(w));
        var fetchedUsers = await Task.WhenAll(userTasks);

        var validUsers = fetchedUsers
            .Where(u => u != null)
            .Select(u => u!.ToDto()) 
            .ToList();

        var responseDto = tournamentUpdated.ToTournamentResponseDetailsDto(validUsers);
        return Result.Success<TournamentResponseDetailsDto, TournamentsErrors>(responseDto);
    }

    public async Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> UnassignWorkerMachine(long tournamentId, string request)
    {
        logger.LogInformation("Assigning worker machine {Id}", tournamentId);
        var workerUlid= Ulid.TryParse(request, out var ulid) ? ulid : Ulid.Empty;
        if (workerUlid == Ulid.Empty)        {
            logger.LogWarning("Invalid user id {Id}", request);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new ValidationError("Invalid user id"));
        }
        var tournamentUpdated =await repository.RemoveWorker(tournamentId, workerUlid);
        if (tournamentUpdated is null)
        {
            logger.LogWarning("Tournament with id {Id} not found", tournamentId);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new TournamentNotFoundError());
        }
        var users= tournamentUpdated.WorkersList.Select(async w => await userRepository.FindByIdAsync(w)).Where(u => u != null).ToList();
        var userTasks = tournamentUpdated.WorkersList.Select(w => userRepository.FindByIdAsync(w));
        var fetchedUsers = await Task.WhenAll(userTasks);

        var validUsers = fetchedUsers
            .Where(u => u != null)
            .Select(u => u!.ToDto()) 
            .ToList();

        var responseDto = tournamentUpdated.ToTournamentResponseDetailsDto(validUsers);
        return Result.Success<TournamentResponseDetailsDto, TournamentsErrors>(responseDto);
    }

    public async Task<Result<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>> GetAssignedWorkerMachines(long tournamentId)
    {
        logger.LogInformation("Getting assigned worker machines for tournament {Id}", tournamentId);
        var assignments = await repository.GetAssignedWorkerMachinesAsync(tournamentId);

        if (assignments == null)
        {
            logger.LogWarning("Tournament with id {Id} not found or deleted", tournamentId);
            return Result.Failure<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>(
                new TournamentNotFoundError());
        }
        var tasks = assignments.Select(async a =>
        {
            var user = await userRepository.FindByIdAsync(a.WorkerId);
            return new { Assignment = a, User = user };
        });

        var results = await Task.WhenAll(tasks);

        var responseDtos = results
            .Where(r => r.User != null) // Si el repositorio no encontró al usuario, se ignora
            .Select(r => r.Assignment.ToWorkerMachineAssignmentResponseDto(r.User!.ToDto()))
            .ToList();

        return Result.Success<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>(responseDtos);
    }

    public async Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> GetTournamentByName(string name)
    {
        logger.LogInformation("Getting tournament {Id}", name);
        var tournament = await repository.FindByNameAsync(name);
        if (tournament is null)
        {
            logger.LogWarning("Tournament with id {Id} not found", name);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new TournamentNotFoundError());
        }
        var userTasks = tournament.WorkersList.Select(w => userRepository.FindByIdAsync(w));
        var fetchedUsers = await Task.WhenAll(userTasks);

        var validUsers = fetchedUsers
            .Where(u => u != null)
            .Select(u => u!.ToDto()) 
            .ToList();

        var responseDto = tournament.ToTournamentResponseDetailsDto(validUsers);
        return Result.Success<TournamentResponseDetailsDto, TournamentsErrors>(responseDto);
    }
}