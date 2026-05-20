using BackEncordados.Common.Dto;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Error;
using BackEncordados.Talleres.Model;
using BackEncordados.Talleres.Repository;
using BackEncordados.Talleres.Service;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TestEncordados.Unit.Fixtures;
using TournamentServiceType = BackEncordados.Talleres.Service.TournamentService;
using ITournamentRepositoryType = BackEncordados.Talleres.Repository.ITournamentRepository;
using IUserRepositoryType = BackEncordados.Usuarios.Repository.IUserRepository;
using ICloudinaryServiceType = BackEncordados.Common.Service.Cloudinary.ICloudinaryService;

namespace TestEncordados.Unit.Services.Talleres;

public class TournamentServiceTests
{
    private readonly Mock<ITournamentRepositoryType> _mockRepo;
    private readonly Mock<IUserRepositoryType> _mockUserRepo;
    private readonly Mock<ICloudinaryServiceType> _mockCloudinary;
    private readonly Mock<ILogger<TournamentServiceType>> _mockLogger;
    private readonly TournamentServiceType _service;

    public TournamentServiceTests()
    {
        _mockRepo = new Mock<ITournamentRepositoryType>();
        _mockUserRepo = new Mock<IUserRepositoryType>();
        _mockCloudinary = CloudinaryServiceBuilder.Create();
        _mockLogger = new Mock<ILogger<TournamentServiceType>>();
        _service = new TournamentServiceType(
            _mockLogger.Object,
            _mockRepo.Object,
            _mockUserRepo.Object,
            _mockCloudinary.Object);
    }

    private static FilterTournamentDto CreateFilter(string search = "") =>
        new(Search: search, UserId: null, Page: 0, Size: 10, SortBy: "name", Direction: "asc");

    [Test]
    public async Task GetTournament_ExistingId_ReturnsSuccess()
    {
        var tournamentId = Ulid.NewUlid();
        var ownerId = Ulid.NewUlid();
        var owner = UserBuilder.OwnerUser(ownerId);
        var tournament = TournamentBuilder.WithOwner(ownerId);

        _mockRepo.Setup(r => r.FindByIdAsync(tournamentId)).ReturnsAsync(tournament);
        _mockUserRepo.Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Ulid>>())).ReturnsAsync(new List<User> { owner });
        _mockUserRepo.Setup(r => r.FindByIdAsync(ownerId)).ReturnsAsync(owner);

        var result = await _service.GetTournament(tournamentId);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task GetTournament_NonExistingId_ReturnsNotFoundError()
    {
        var tournamentId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.FindByIdAsync(tournamentId)).ReturnsAsync((Tournaments?)null);

        var result = await _service.GetTournament(tournamentId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TournamentNotFoundError>();
    }

    [Test]
    public async Task GetAllTournamentsAsync_ReturnsPagedResponse()
    {
        var filter = CreateFilter();
        var tournaments = new List<Tournaments> { TournamentBuilder.Create() };
        var pagedResult = (Items: (IEnumerable<Tournaments>)tournaments, TotalCount: 1);

        _mockRepo.Setup(r => r.FindAllAsync(filter)).ReturnsAsync(pagedResult);

        var result = await _service.GetAllTournamentsAsync(filter);

        result.Content.Should().HaveCount(1);
        result.TotalElements.Should().Be(1);
    }

    [Test]
    public async Task GetAllTournamentsAsync_EmptyResult_ReturnsEmptyPage()
    {
        var filter = CreateFilter();
        var pagedResult = (Items: (IEnumerable<Tournaments>)new List<Tournaments>(), TotalCount: 0);

        _mockRepo.Setup(r => r.FindAllAsync(filter)).ReturnsAsync(pagedResult);

        var result = await _service.GetAllTournamentsAsync(filter);

        result.Content.Should().BeEmpty();
        result.TotalElements.Should().Be(0);
    }

    [Test]
    public async Task CreateTournament_ValidOwner_ReturnsSuccess()
    {
        var ownerId = Ulid.NewUlid();
        var owner = UserBuilder.OwnerUser(ownerId);
        var tournament = (Tournaments?)TournamentBuilder.WithOwner(ownerId);
        var dto = new TournamentAdminRequestDto
        {
            Name = "Test Tournament",
            OwnerId = ownerId,
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(7)
        };

        _mockUserRepo.Setup(r => r.FindByIdAsync(ownerId)).ReturnsAsync(owner);
        _mockRepo.Setup(r => r.SaveAsync(It.IsAny<Tournaments>())).ReturnsAsync(tournament);
        _mockUserRepo.Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Ulid>>())).ReturnsAsync(new List<User>());

        var result = await _service.CreateTournament(dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CreateTournament_NonExistingOwner_ReturnsUserNotFoundError()
    {
        var ownerId = Ulid.NewUlid();
        var dto = new TournamentAdminRequestDto
        {
            Name = "Test Tournament",
            OwnerId = ownerId,
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(7)
        };

        _mockUserRepo.Setup(r => r.FindByIdAsync(ownerId)).ReturnsAsync((User?)null);

        var result = await _service.CreateTournament(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task CreateTournament_NonOwnerUser_ReturnsUserNotFoundError()
    {
        var userId = Ulid.NewUlid();
        var user = UserBuilder.StandardUser(userId);
        var dto = new TournamentAdminRequestDto
        {
            Name = "Test Tournament",
            OwnerId = userId,
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(7)
        };

        _mockUserRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(user);

        var result = await _service.CreateTournament(dto);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateTournament_ExistingId_ReturnsSuccess()
    {
        var tournamentId = Ulid.NewUlid();
        var tournament = TournamentBuilder.Create(id: tournamentId);
        var dto = new TournamentPatchDto { Name = "Updated Tournament" };

        _mockRepo.Setup(r => r.FindByIdAsync(tournamentId)).ReturnsAsync((Tournaments?)TournamentBuilder.Create(id: tournamentId));
        _mockRepo.Setup(r => r.UpdateAsync(tournamentId, It.IsAny<Tournaments>())).ReturnsAsync((Tournaments?)TournamentBuilder.Create(id: tournamentId));

        var result = await _service.UpdateTournament(tournamentId, dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UpdateTournament_NonExistingId_ReturnsNotFoundError()
    {
        var tournamentId = Ulid.NewUlid();
        var dto = new TournamentPatchDto { Name = "Updated Tournament" };

        _mockRepo.Setup(r => r.FindByIdAsync(tournamentId)).ReturnsAsync((Tournaments?)null);

        var result = await _service.UpdateTournament(tournamentId, dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TournamentNotFoundError>();
    }

    [Test]
    public async Task DeleteTournament_ExistingId_ReturnsSuccess()
    {
        var tournamentId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.DeleteAsync(tournamentId)).ReturnsAsync(true);

        var result = await _service.DeleteTournament(tournamentId);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteTournament_NonExistingId_ReturnsNotFoundError()
    {
        var tournamentId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.DeleteAsync(tournamentId)).ReturnsAsync(false);

        var result = await _service.DeleteTournament(tournamentId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TournamentNotFoundError>();
    }

    [Test]
    public async Task AssignWorkerMachine_ValidWorker_ReturnsSuccess()
    {
        var tournamentId = Ulid.NewUlid();
        var workerId = Ulid.NewUlid();
        var ownerId = Ulid.NewUlid();
        var tournament = (Tournaments?)TournamentBuilder.WithOwner(ownerId);
        var owner = UserBuilder.OwnerUser(ownerId);
        var request = new WorkerMachineAssignmentRequestDto
        {
            UserId = workerId.ToString(),
            MachineName = "Machine-1"
        };

        _mockRepo.Setup(r => r.AsignWorker(tournamentId, workerId, "Machine-1")).ReturnsAsync(tournament);
        _mockUserRepo.Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Ulid>>())).ReturnsAsync(new List<User>());
        _mockUserRepo.Setup(r => r.FindByIdAsync(ownerId)).ReturnsAsync(owner);

        var result = await _service.AssignWorkerMachine(tournamentId, request);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task AssignWorkerMachine_InvalidUserId_ReturnsValidationError()
    {
        var tournamentId = Ulid.NewUlid();
        var request = new WorkerMachineAssignmentRequestDto
        {
            UserId = "invalid-ulid",
            MachineName = "Machine-1"
        };

        var result = await _service.AssignWorkerMachine(tournamentId, request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BackEncordados.Talleres.Error.ValidationError>();
    }

    [Test]
    public async Task AssignWorkerMachine_TournamentNotFound_ReturnsNotFoundError()
    {
        var tournamentId = Ulid.NewUlid();
        var workerId = Ulid.NewUlid();
        var request = new WorkerMachineAssignmentRequestDto
        {
            UserId = workerId.ToString(),
            MachineName = "Machine-1"
        };

        _mockRepo.Setup(r => r.AsignWorker(tournamentId, workerId, "Machine-1")).ReturnsAsync((Tournaments?)null);

        var result = await _service.AssignWorkerMachine(tournamentId, request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TournamentNotFoundError>();
    }

    [Test]
    public async Task UnassignWorkerMachine_ValidWorker_ReturnsSuccess()
    {
        var tournamentId = Ulid.NewUlid();
        var workerId = Ulid.NewUlid();
        var ownerId = Ulid.NewUlid();
        var tournament = (Tournaments?)TournamentBuilder.WithOwner(ownerId);
        var owner = UserBuilder.OwnerUser(ownerId);

        _mockRepo.Setup(r => r.RemoveWorker(tournamentId, workerId)).ReturnsAsync(tournament);
        _mockUserRepo.Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Ulid>>())).ReturnsAsync(new List<User>());
        _mockUserRepo.Setup(r => r.FindByIdAsync(ownerId)).ReturnsAsync(owner);

        var result = await _service.UnassignWorkerMachine(tournamentId, workerId.ToString());

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UnassignWorkerMachine_InvalidUserId_ReturnsValidationError()
    {
        var tournamentId = Ulid.NewUlid();

        var result = await _service.UnassignWorkerMachine(tournamentId, "invalid-ulid");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BackEncordados.Talleres.Error.ValidationError>();
    }

    [Test]
    public async Task GetAssignedWorkerMachines_ExistingTournament_ReturnsAssignments()
    {
        var tournamentId = Ulid.NewUlid();
        var workerId = Ulid.NewUlid();
        var assignments = new List<WorkerMachineAssignment>
        {
            new() { WorkerId = workerId, MachineName = "Machine-1" }
        };
        var worker = UserBuilder.StandardUser(workerId);

        _mockRepo.Setup(r => r.GetAssignedWorkerMachinesAsync(tournamentId)).ReturnsAsync((IEnumerable<WorkerMachineAssignment>?)assignments);
        _mockUserRepo.Setup(r => r.FindByIdsAsync(It.Is<IEnumerable<Ulid>>(ids => ids.Contains(workerId)))).ReturnsAsync(new List<User> { worker });

        var result = await _service.GetAssignedWorkerMachines(tournamentId);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task GetAssignedWorkerMachines_NonExistingTournament_ReturnsNotFoundError()
    {
        var tournamentId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.GetAssignedWorkerMachinesAsync(tournamentId)).ReturnsAsync((IEnumerable<WorkerMachineAssignment>?)null);

        var result = await _service.GetAssignedWorkerMachines(tournamentId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TournamentNotFoundError>();
    }

    [Test]
    public async Task GetTournamentByName_ExistingName_ReturnsSuccess()
    {
        var tournament = TournamentBuilder.Create();

        _mockRepo.Setup(r => r.FindByNameAsync(tournament.Title)).ReturnsAsync((Tournaments?)TournamentBuilder.Create());
        _mockUserRepo.Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Ulid>>())).ReturnsAsync(new List<User>());
        _mockUserRepo.Setup(r => r.FindByIdAsync(It.IsAny<Ulid>())).ReturnsAsync(UserBuilder.OwnerUser());

        var result = await _service.GetTournamentByName(tournament.Title);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task GetTournamentByName_NonExistingName_ReturnsNotFoundError()
    {
        _mockRepo.Setup(r => r.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((Tournaments?)null);

        var result = await _service.GetTournamentByName("NonExistentTournament");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TournamentNotFoundError>();
    }

    [Test]
    public async Task OwnerCreateTournament_ValidOwner_ReturnsSuccess()
    {
        var ownerId = Ulid.NewUlid();
        var owner = UserBuilder.OwnerUser(ownerId);
        var tournament = (Tournaments?)TournamentBuilder.WithOwner(ownerId);
        var request = new TournamentRequestDto
        {
            Name = "Test Tournament",
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(7)
        };

        _mockUserRepo.Setup(r => r.FindByIdAsync(ownerId)).ReturnsAsync(owner);
        _mockRepo.Setup(r => r.SaveAsync(It.IsAny<Tournaments>())).ReturnsAsync(tournament);
        _mockUserRepo.Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Ulid>>())).ReturnsAsync(new List<User>());

        var result = await _service.OwnerCreateTournament(request, ownerId);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task AssingSupervisor_ValidSupervisor_ReturnsSuccess()
    {
        var tournamentId = Ulid.NewUlid();
        var supervisorId = Ulid.NewUlid();
        var ownerId = Ulid.NewUlid();
        var tournament = (Tournaments?)TournamentBuilder.WithOwner(ownerId);
        var owner = UserBuilder.OwnerUser(ownerId);
        var request = new SupervisorAsignmentRequestDto
        {
            TournamentId = tournamentId,
            SupervisorId = supervisorId.ToString()
        };

        _mockRepo.Setup(r => r.AsignSupervisor(tournamentId, supervisorId)).ReturnsAsync(tournament);
        _mockUserRepo.Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Ulid>>())).ReturnsAsync(new List<User>());
        _mockUserRepo.Setup(r => r.FindByIdAsync(ownerId)).ReturnsAsync(owner);

        var result = await _service.AssingSupervisor(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task AssingSupervisor_InvalidSupervisorId_ReturnsValidationError()
    {
        var request = new SupervisorAsignmentRequestDto
        {
            TournamentId = Ulid.NewUlid(),
            SupervisorId = "invalid-ulid"
        };

        var result = await _service.AssingSupervisor(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BackEncordados.Talleres.Error.ValidationError>();
    }

    [Test]
    public async Task AnassingSupervisor_ValidSupervisor_ReturnsSuccess()
    {
        var tournamentId = Ulid.NewUlid();
        var supervisorId = Ulid.NewUlid();
        var ownerId = Ulid.NewUlid();
        var tournament = (Tournaments?)TournamentBuilder.WithOwner(ownerId);
        var owner = UserBuilder.OwnerUser(ownerId);
        var request = new SupervisorAsignmentRequestDto
        {
            TournamentId = tournamentId,
            SupervisorId = supervisorId.ToString()
        };

        _mockRepo.Setup(r => r.RemoveSupervisor(tournamentId, supervisorId)).ReturnsAsync(tournament);
        _mockUserRepo.Setup(r => r.FindByIdsAsync(It.IsAny<IEnumerable<Ulid>>())).ReturnsAsync(new List<User>());
        _mockUserRepo.Setup(r => r.FindByIdAsync(ownerId)).ReturnsAsync(owner);

        var result = await _service.AnassingSupervisor(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task AnassingSupervisor_TournamentNotFound_ReturnsNotFoundError()
    {
        var tournamentId = Ulid.NewUlid();
        var supervisorId = Ulid.NewUlid();
        var request = new SupervisorAsignmentRequestDto
        {
            TournamentId = tournamentId,
            SupervisorId = supervisorId.ToString()
        };

        _mockRepo.Setup(r => r.RemoveSupervisor(tournamentId, supervisorId)).ReturnsAsync((Tournaments?)null);

        var result = await _service.AnassingSupervisor(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<TournamentNotFoundError>();
    }
}