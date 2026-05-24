using BackEncordados.Common.Dto;
using BackEncordados.Common.Errors;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Common.Utils;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Error;
using BackEncordados.Talleres.Mapper;
using BackEncordados.Talleres.Model;
using BackEncordados.Talleres.Repository;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Mapper;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using CSharpFunctionalExtensions;
using ConflictError = BackEncordados.Talleres.Error.ConflictError;
using ValidationError = BackEncordados.Talleres.Error.ValidationError;

namespace BackEncordados.Talleres.Service;

public class TournamentService(
    ILogger<TournamentService> logger,
    ITournamentRepository repository, 
    IUserRepository userRepository,
    ICloudinaryService cloudinary
    ): ITournamentService
{
    private async Task<Result<TournamentResponseDetailsDto, DomainErrors>> BuildResponseAsync(Tournaments tournament, User? preloadedOwner = null)
    {
        var allIds = tournament.WorkersList
            .Union(tournament.SupervisorList)
            .Append(tournament.Owner)
            .Distinct()
            .ToList();

        var users = await userRepository.FindByIdsAsync(allIds);
        var userDict = users.ToDictionary(u => u.Id);

        if (!userDict.ContainsKey(tournament.Owner))
        {
            if (preloadedOwner is not null)
                userDict[preloadedOwner.Id] = preloadedOwner;
            else
            {
                var ownerUser = await userRepository.FindByIdAsync(tournament.Owner);
                if (ownerUser is null)
                    return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(
                        new UserNotFoundError("owner de torneo no encontrado"));
                userDict[tournament.Owner] = ownerUser;
            }
        }

        var workers = tournament.WorkersList
            .Where(id => userDict.ContainsKey(id))
            .Select(id => userDict[id].ToDto(cloudinary))
            .ToList();
        var supervisors = tournament.SupervisorList
            .Where(id => userDict.ContainsKey(id))
            .Select(id => userDict[id].ToDto(cloudinary))
            .ToList();
        var owner = userDict[tournament.Owner].ToDto(cloudinary);

        return Result.Success<TournamentResponseDetailsDto, DomainErrors>(
            tournament.ToTournamentResponseDetailsDto(workers, owner, supervisors, cloudinary));
    }

    public async Task<Result<TournamentResponseDetailsDto, DomainErrors>> GetTournament(Ulid id)
    {
        logger.LogInformation("Getting tournament {Id}", id);
        var tournament = await repository.FindByIdAsync(id);
        if (tournament is null)
        {
            logger.LogWarning("Tournament with id {Id} not found", id);
            return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(new TournamentNotFoundError());
        }
        return await BuildResponseAsync(tournament);
    }

    public async Task<PageResponseDto<TournamentResponseDto>> GetAllTournamentsAsync(FilterTournamentDto filter)
    {
        var paged= await repository.FindAllAsync(filter);
        int totalPages = filter.Size > 0 ? (int)Math.Ceiling(paged.TotalCount / (double)filter.Size) : 0;
        return new PageResponseDto<TournamentResponseDto>(
            Content: paged.Items.Select(item => item.ToTournamentResponseDto(cloudinary)).ToList(),
            TotalPages: totalPages,
            TotalElements: paged.TotalCount,
            PageSize: filter.Size,
            PageNumber: filter.Page,    
            TotalPageElements: paged.Items.Count(),
            SortBy: filter.SortBy,
            Direction: filter.Direction
        );
    }

    public async Task<Result<TournamentResponseDetailsDto, DomainErrors>> CreateTournament(
        TournamentAdminRequestDto adminRequest)
    {
        logger.LogInformation("Creating tournament with title {Title}", adminRequest.Name);
        var user = await userRepository.FindByIdAsync(adminRequest.OwnerId);
        if (user is null || user.Role!= User.UserRoles.OWNER) 
            return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(new UserNotFoundError("usuario invalido o no encontrado"))
                .TapError((() => logger.LogInformation("Usuario invalido o no encontrado con id {Id}",adminRequest.OwnerId)));
        var imageUrl= CloudinaryConstants.DEFAULT_IMAGE_TALLERES;
        string? publicId=null;
        if (adminRequest.Logotype != null) {
            var upload= await cloudinary.UploadWithAutoNameAsync(adminRequest.Logotype, adminRequest.Name, CloudinaryConstants.FOLDER_TALLERES);
            imageUrl = upload.ImageUrl;
            publicId = upload.PublicId;
        }
        var saved = await repository.SaveAsync(adminRequest.ToTournaments(imageUrl,publicId));
        return await BuildResponseAsync(saved, user);
    }

    public async Task<Result<TournamentResponseDto, TournamentsErrors>> UpdateTournament(Ulid id, TournamentPatchDto request)
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
        if (request.Logotype != null) {
            if (oldTournament.Logotype != CloudinaryConstants.DEFAULT_IMAGE_TALLERES) 
                await cloudinary.DeleteAsync(oldTournament.LogotypePublicId!);
            var upload= await cloudinary.UploadWithAutoNameAsync(request.Logotype, id.ToString(), CloudinaryConstants.FOLDER_TALLERES);
            oldTournament.Logotype = upload.ImageUrl;
            oldTournament.LogotypePublicId = upload.PublicId;
        }
        return await repository.UpdateAsync(id, oldTournament) is {} updated
            ? Result.Success<TournamentResponseDto, TournamentsErrors>(updated.ToTournamentResponseDto(cloudinary))
            : Result.Failure<TournamentResponseDto, TournamentsErrors>(new ConflictError("Error updating tournament"));
    }

    public async Task<Result<Unit, TournamentsErrors>> DeleteTournament(Ulid id)
    {
        logger.LogInformation("Deleting tournament {Id}", id);
        return await repository.DeleteAsync(id) 
            ? Result.Success<Unit, TournamentsErrors>(Unit.Value) 
            : Result.Failure<Unit, TournamentsErrors>(new TournamentNotFoundError());
    }

    public async Task<Result<TournamentResponseDetailsDto, DomainErrors>> AssignWorkerMachine(Ulid tournamentId, WorkerMachineAssignmentRequestDto request)
    {
        logger.LogInformation("Assigning worker machine {Id}", tournamentId);
        var workerUlid= Ulid.TryParse(request.UserId, out var ulid) ? ulid : Ulid.Empty;
        if (workerUlid == Ulid.Empty)        {
            logger.LogWarning("Invalid user id {Id}", request.UserId);
            return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(new ValidationError("Invalid user id"));
        }
        var tournamentUpdated =await repository.AsignWorker(tournamentId, workerUlid, request.MachineName);
        if (tournamentUpdated is null)
        {
            logger.LogWarning("Tournament with id {Id} not found", tournamentId);
            return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(new TournamentNotFoundError());
        }
        return await BuildResponseAsync(tournamentUpdated);
    }

    public async Task<Result<TournamentResponseDetailsDto, DomainErrors>> UnassignWorkerMachine(Ulid tournamentId, string request)
    {
        logger.LogInformation("Unassigning worker machine {Id}", tournamentId);
        var workerUlid= Ulid.TryParse(request, out var ulid) ? ulid : Ulid.Empty;
        if (workerUlid == Ulid.Empty)        {
            logger.LogWarning("Invalid user id {Id}", request);
            return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(new ValidationError("Invalid user id"));
        }
        var tournamentUpdated =await repository.RemoveWorker(tournamentId, workerUlid);
        if (tournamentUpdated is null)
        {
            logger.LogWarning("Tournament with id {Id} not found", tournamentId);
            return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(new TournamentNotFoundError());
        }
        return await BuildResponseAsync(tournamentUpdated);
    }

    public async Task<Result<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>> GetAssignedWorkerMachines(Ulid tournamentId)
    {
        logger.LogInformation("Getting assigned worker machines for tournament {Id}", tournamentId);
        var assignments = await repository.GetAssignedWorkerMachinesAsync(tournamentId);

        if (assignments == null)
        {
            logger.LogWarning("Tournament with id {Id} not found or deleted", tournamentId);
            return Result.Failure<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>(
                new TournamentNotFoundError());
        }

        var workerMachineAssignments = assignments.ToList();
        var workerIds = workerMachineAssignments.Select(a => a.WorkerId);
        var users = await userRepository.FindByIdsAsync(workerIds);
        var userDict = users.ToDictionary(u => u.Id);

        var responseDtos = workerMachineAssignments
            .Where(a => userDict.ContainsKey(a.WorkerId))
            .Select(a => a.ToWorkerMachineAssignmentResponseDto(userDict[a.WorkerId].ToDto(cloudinary)))
            .ToList();

        return Result.Success<IEnumerable<WorkerMachineAssignmentResponseDto>, TournamentsErrors>(responseDtos);
    }

    public async Task<Result<TournamentResponseDetailsDto, DomainErrors>> GetTournamentByName(string name)
    {
        logger.LogInformation("Getting tournament {Id}", name);
        var tournament = await repository.FindByNameAsync(name);
        if (tournament is null)
        {
            logger.LogWarning("Tournament with id {Id} not found", name);
            return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(new TournamentNotFoundError());
        }
        return await BuildResponseAsync(tournament);
    }

    public async Task<Result<TournamentResponseDetailsDto, DomainErrors>> OwnerCreateTournament(TournamentRequestDto request, Ulid ownerId) {
        var user= await userRepository.FindByIdAsync(ownerId);
        if (user is null || user.Role!= User.UserRoles.OWNER) 
            return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(new UserNotFoundError("usuario invalido o no encontrado"))
                .TapError((() => logger.LogInformation("Usuario con id {Id} invalido o no encontrado",ownerId)));
        var imageUrl= CloudinaryConstants.DEFAULT_IMAGE_TALLERES;
        string? publicId=null;
        if (request.Logotype != null) {
            var upload= await cloudinary.UploadWithAutoNameAsync(request.Logotype, request.Name, CloudinaryConstants.FOLDER_TALLERES);
            imageUrl = upload.ImageUrl;
            publicId = upload.PublicId;
        }
        var saved = await repository.SaveAsync(request.ToTournaments(ownerId,imageUrl,publicId));
        return await BuildResponseAsync(saved, user);
    }

    public async Task<Result<TournamentResponseDetailsDto, DomainErrors>> AssingSupervisor(SupervisorAsignmentRequestDto request) {
        
        logger.LogInformation("Assigning worker machine {Id}", request.TournamentId);
        var supervisorId= Ulid.TryParse(request.SupervisorId, out var ulid) ? ulid : Ulid.Empty;
        if (supervisorId == Ulid.Empty)        {
            logger.LogWarning("Invalid user id {Id}", request.SupervisorId);
            return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(new ValidationError("Invalid user id"));
        }
        var tournamentUpdated =await repository.AsignSupervisor(request.TournamentId, supervisorId);
        if (tournamentUpdated is null)
        {
            logger.LogWarning("Tournament with id {Id} not found", request.SupervisorId);
            return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(new TournamentNotFoundError());
        }
        return await BuildResponseAsync(tournamentUpdated);
    }

    public async Task<Result<TournamentResponseDetailsDto, DomainErrors>> AnassingSupervisor(SupervisorAsignmentRequestDto request) {
        logger.LogInformation("Assigning worker machine {Id}", request.TournamentId);
        var supervisorId= Ulid.TryParse(request.SupervisorId, out var ulid) ? ulid : Ulid.Empty;
        if (supervisorId == Ulid.Empty)        {
            logger.LogWarning("Invalid user id {Id}", request);
            return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(new ValidationError("Invalid user id"));
        }
        var tournamentUpdated =await repository.RemoveSupervisor(request.TournamentId, supervisorId);
        if (tournamentUpdated is null)
        {
            logger.LogWarning("Tournament with id {Id} not found", request.TournamentId);
            return Result.Failure<TournamentResponseDetailsDto, DomainErrors>(new TournamentNotFoundError());
        }
        return await BuildResponseAsync(tournamentUpdated);
    }
}