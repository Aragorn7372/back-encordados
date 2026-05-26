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

/// <summary>
/// Implementación de <see cref="ITournamentService"/> que orquesta las operaciones de negocio
/// sobre torneos coordinando el repositorio de torneos, repositorio de usuarios y Cloudinary.
/// </summary>
/// <remarks>
/// <para><b>Dependencias inyectadas:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Parámetro</term>
///     <term>Tipo</term>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>logger</c></term>
///     <term><c>ILogger&lt;TournamentService&gt;</c></term>
///     <description>Logging de todas las operaciones de torneos.</description>
///   </item>
///   <item>
///     <term><c>repository</c></term>
///     <term><see cref="ITournamentRepository"/></term>
///     <description>Acceso a datos de torneos (CRUD + asignaciones).</description>
///   </item>
///   <item>
///     <term><c>userRepository</c></term>
///     <term><see cref="IUserRepository"/></term>
///     <description>Acceso a datos de usuarios para resolver workers, supervisores y owners.</description>
///   </item>
///   <item>
///     <term><c>cloudinary</c></term>
///     <term><see cref="ICloudinaryService"/></term>
///     <description>Gestión de logotipos de torneos (subida, borrado, resolución de URLs).</description>
///   </item>
/// </list>
/// <para>El método privado <c>BuildResponseAsync</c> es el corazón del servicio: construye el DTO detallado
/// resolviendo todos los IDs de usuarios (workers, supervisores, owner) a sus DTOs correspondientes.</para>
/// </remarks>
public class TournamentService(
    ILogger<TournamentService> logger,
    ITournamentRepository repository, 
    IUserRepository userRepository,
    ICloudinaryService cloudinary
    ): ITournamentService
{
    /// <summary>
    /// Construye la respuesta detallada de un torneo resolviendo todos los IDs de usuarios.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Recolecta todos los IDs de <c>WorkersList</c>, <c>SupervisorList</c> y <c>Owner</c>.</description></item>
    ///   <item><description>Consulta los usuarios en lote mediante <c>userRepository.FindByIdsAsync</c>.</description></item>
    ///   <item><description>Si el owner no está en el resultado, intenta cargarlo individualmente (o usa <c>preloadedOwner</c> si se proporciona).</description></item>
    ///   <item><description>Mapea workers, supervisores y owner a DTOs mediante <see cref="UserMapper.ToDto"/>.</description></item>
    ///   <item><description>Construye el <see cref="TournamentResponseDetailsDto"/> con todas las listas.</description></item>
    /// </list>
    /// <para><b>Casos borde:</b> Si un usuario referenciado en WorkersList o SupervisorList no existe en la BD,
    /// se omite silenciosamente (no se incluye en la lista del DTO).</para>
    /// </remarks>
    /// <param name="tournament">Entidad de torneo con sus listas de IDs.</param>
    /// <param name="preloadedOwner">Usuario owner precargado (opcional, para evitar consultas duplicadas).</param>
    /// <returns>DTO detallado del torneo o error si el owner no existe.</returns>
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

    /// <summary>
    /// Obtiene el detalle completo de un torneo por ULID.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el torneo por ULID en el repositorio.</description></item>
    ///   <item><description>Si no existe o está eliminado, retorna <see cref="TournamentNotFoundError"/>.</description></item>
    ///   <item><description>Construye la respuesta detallada resolviendo usuarios mediante <c>BuildResponseAsync</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">ULID del torneo.</param>
    /// <returns>DTO detallado del torneo o <see cref="TournamentNotFoundError"/>.</returns>
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

    /// <summary>
    /// Obtiene una lista paginada de torneos con filtros.
    /// </summary>
    /// <param name="filter">DTO con filtros de búsqueda, usuario y paginación.</param>
    /// <returns>Página con DTOs básicos de torneo (sin listas de usuarios).</returns>
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

    /// <summary>
    /// Crea un nuevo torneo (solo administradores, especificando OwnerId).
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Valida que el usuario <c>OwnerId</c> exista y tenga rol OWNER.</description></item>
    ///   <item><description>Si se proporciona logotipo, lo sube a Cloudinary.</description></item>
    ///   <item><description>Mapea el DTO a entidad y persiste mediante <c>repository.SaveAsync</c>.</description></item>
    ///   <item><description>Construye la respuesta detallada con el owner precargado.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="adminRequest">DTO con nombre, OwnerId, fechas y logotipo opcional.</param>
    /// <returns>DTO detallado del torneo creado o error si el owner no es válido.</returns>
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

    /// <summary>
    /// Actualiza parcialmente un torneo existente.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el torneo por ID. Si no existe, retorna <see cref="TournamentNotFoundError"/>.</description></item>
    ///   <item><description>Aplica cambios: fechas (si se proporcionan), nombre (si no es null/vacío), logotipo (subiendo nuevo y eliminando anterior si no es el default).</description></item>
    ///   <item><description>Persiste mediante <c>repository.UpdateAsync</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">ULID del torneo.</param>
    /// <param name="request">DTO con campos opcionales (nombre, fechas, logotipo).</param>
    /// <returns>DTO básico actualizado o error.</returns>
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

    /// <summary>
    /// Elimina un torneo (soft delete).
    /// </summary>
    /// <param name="id">ULID del torneo a eliminar.</param>
    /// <returns>Unit en éxito o <see cref="TournamentNotFoundError"/> si no existe.</returns>
    public async Task<Result<Unit, TournamentsErrors>> DeleteTournament(Ulid id)
    {
        logger.LogInformation("Deleting tournament {Id}", id);
        return await repository.DeleteAsync(id) 
            ? Result.Success<Unit, TournamentsErrors>(Unit.Value) 
            : Result.Failure<Unit, TournamentsErrors>(new TournamentNotFoundError());
    }

    /// <summary>
    /// Asigna un trabajador a una máquina dentro del torneo.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Parsea el <c>UserId</c> string a ULID. Si no es válido, retorna <see cref="ValidationError"/>.</description></item>
    ///   <item><description>Delega la asignación a <c>repository.AsignWorker</c>.</description></item>
    ///   <item><description>Si el torneo no existe, retorna <see cref="TournamentNotFoundError"/>.</description></item>
    ///   <item><description>Construye la respuesta detallada del torneo actualizado.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="tournamentId">ULID del torneo.</param>
    /// <param name="request">DTO con UserId (string) y MachineName.</param>
    /// <returns>DTO detallado del torneo actualizado o error.</returns>
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

    /// <summary>
    /// Desasigna un trabajador del torneo y elimina sus asignaciones de máquina.
    /// </summary>
    /// <param name="tournamentId">ULID del torneo.</param>
    /// <param name="request">ULID del trabajador en formato string.</param>
    /// <returns>DTO detallado del torneo actualizado o error.</returns>
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

    /// <summary>
    /// Obtiene todas las asignaciones trabajador-máquina de un torneo.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Obtiene las asignaciones del repositorio.</description></item>
    ///   <item><description>Si el torneo no existe, retorna <see cref="TournamentNotFoundError"/>.</description></item>
    ///   <item><description>Recolecta los IDs de workers y los resuelve a DTOs mediante <c>userRepository.FindByIdsAsync</c>.</description></item>
    ///   <item><description>Construye la lista de <see cref="WorkerMachineAssignmentResponseDto"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="tournamentId">ULID del torneo.</param>
    /// <returns>Lista de asignaciones con datos del usuario, o error si el torneo no existe.</returns>
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

    /// <summary>
    /// Busca un torneo por su nombre exacto.
    /// </summary>
    /// <param name="name">Nombre exacto del torneo.</param>
    /// <returns>DTO detallado del torneo o <see cref="TournamentNotFoundError"/>.</returns>
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

    /// <summary>
    /// Crea un torneo desde el propietario autenticado (ownerId extraído del JWT).
    /// </summary>
    /// <remarks>
    /// <para>Similar a <c>CreateTournament</c> pero el ownerId se pasa como parámetro en lugar de venir del DTO.</para>
    /// </remarks>
    /// <param name="request">DTO con nombre, fechas y logotipo opcional.</param>
    /// <param name="ownerId">ULID del propietario (extraído del token JWT).</param>
    /// <returns>DTO detallado del torneo creado o error.</returns>
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

    /// <summary>
    /// Asigna un supervisor a un torneo.
    /// </summary>
    /// <remarks>
    /// <para>Parsea el <c>SupervisorId</c> string a ULID. Si no es válido, retorna <see cref="ValidationError"/>.
    /// Delega la asignación a <c>repository.AsignSupervisor</c>.</para>
    /// </remarks>
    /// <param name="request">DTO con TournamentId y SupervisorId (string).</param>
    /// <returns>DTO detallado del torneo actualizado o error.</returns>
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

    /// <summary>
    /// Desasigna un supervisor de un torneo.
    /// </summary>
    /// <remarks>
    /// <para>Parsea el <c>SupervisorId</c> string a ULID. Delega la operación a <c>repository.RemoveSupervisor</c>.</para>
    /// </remarks>
    /// <param name="request">DTO con TournamentId y SupervisorId (string).</param>
    /// <returns>DTO detallado del torneo actualizado o error.</returns>
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