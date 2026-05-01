using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Error;
using BackEncordados.Talleres.Mapper;
using BackEncordados.Talleres.Model;
using BackEncordados.Talleres.Repository;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Mapper;
using BackEncordados.Usuarios.Repository;
using CSharpFunctionalExtensions;

namespace BackEncordados.Talleres.Service;

public class TournamentService(ILogger<TournamentService> logger,ITournamentRepository repository, IUserRepository userRepository): ITournamentService
{
    public async Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> GetTournament(long id)
    {
        logger.LogInformation("Getting tournament {id}", id);
        var tournament = await repository.FindByIdAsync(id);
        if (tournament is null)
        {
            logger.LogWarning("Tournament with id {id} not found", id);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new TournamentsErrors("Tournament not found"));
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
        logger.LogInformation("Creating tournament with title {title}", request.Name);
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

    public async Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> UpdateTournament(long id, TournamentPatchDto request)
    {
        logger.LogInformation("Updating tournament {id}", id);
        var oldTournament =await repository.FindByIdAsync(id);
        if (oldTournament is null)
        {
            logger.LogWarning("Tournament with id {id} not found", id);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(
                    new TournamentsErrors("Tournament not found"));
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
            ? Result.Success<TournamentResponseDetailsDto, TournamentsErrors>(updated.ToTournamentResponseDetailsDto(new List<UserResponseDto>()))
            : Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new TournamentsErrors("Error updating tournament"));
    }

    public async Task<Result<Unit, TournamentsErrors>> DeleteTournament(long id)
    {
        logger.LogInformation("Deleting tournament {id}", id);
        return await repository.DeleteAsync(id) 
            ? Result.Success<Unit, TournamentsErrors>(Unit.Value) 
            : Result.Failure<Unit, TournamentsErrors>(new TournamentsErrors("Error deleting tournament"));
    }

    public async Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> AssignWorkerMachine(long tournamentId, WorkerMachineAssignmentRequestDto request)
    {
        logger.LogInformation("Assigning worker machine {id}", tournamentId);
        var workerGuid= Guid.TryParse(request.UserId, out var guid) ? guid : Guid.Empty;
        if (workerGuid == Guid.Empty)        {
            logger.LogWarning("Invalid user id {id}", request.UserId);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new TournamentsErrors("Invalid user id"));
        }
        var tournamentUpdated =await repository.AsignWorker(tournamentId, workerGuid, request.MachineName);
        if (tournamentUpdated is null)
        {
            logger.LogWarning("Tournament with id {id} not found", tournamentId);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new TournamentsErrors("Tournament not found"));
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

    public async Task<Result<TournamentResponseDetailsDto, TournamentsErrors>> UnassignWorkerMachine(long tournamentId, WorkerMachineAssignmentRequestDto request)
    {
        logger.LogInformation("Assigning worker machine {id}", tournamentId);
        var workerGuid= Guid.TryParse(request.UserId, out var guid) ? guid : Guid.Empty;
        if (workerGuid == Guid.Empty)        {
            logger.LogWarning("Invalid user id {id}", request.UserId);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new TournamentsErrors("Invalid user id"));
        }
        var tournamentUpdated =await repository.RemoveWorker(tournamentId, workerGuid);
        if (tournamentUpdated is null)
        {
            logger.LogWarning("Tournament with id {id} not found", tournamentId);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new TournamentsErrors("Tournament not found"));
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
        logger.LogInformation("Getting assigned worker machines for tournament {id}", tournamentId);
        var assignments = await repository.GetAssignedWorkerMachinesAsync(tournamentId);

        if (assignments == null)
        {
            logger.LogWarning("Tournament with id {id} not found or deleted", tournamentId);
            return Result.Failure<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>(
                new TournamentsErrors("Tournament not found"));
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
        logger.LogInformation("Getting tournament {id}", name);
        var tournament = await repository.FindByNameAsync(name);
        if (tournament is null)
        {
            logger.LogWarning("Tournament with id {id} not found", name);
            return Result.Failure<TournamentResponseDetailsDto, TournamentsErrors>(new TournamentsErrors("Tournament not found"));
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