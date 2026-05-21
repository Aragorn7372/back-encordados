using System.Security.Claims;
using BackEncordados.Common.Database.Config;
using BackEncordados.Common.SignalR;
using BackEncordados.Talleres.Model;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace TestEncordados.Unit.Common.SignalR;

public class SignalHubTests
{
    private Mock<HubCallerContext> _mockContext = null!;
    private Mock<IGroupManager> _mockGroups = null!;
    private TextWriter _originalConsoleOut = null!;
    private StringWriter _consoleOutput = null!;
    private TalleresDbContext _dbContext = null!;

    private static readonly Ulid UserUlid = Ulid.NewUlid();
    private const string ConnectionId = "test-connection-id";

    [SetUp]
    public void SetUp()
    {
        _mockContext = new Mock<HubCallerContext>();
        _mockContext.Setup(c => c.ConnectionId).Returns(ConnectionId);
        _mockContext.Setup(c => c.UserIdentifier).Returns(UserUlid.ToString());

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, "USER")]);
        _mockContext.Setup(c => c.User).Returns(new ClaimsPrincipal(identity));

        _mockGroups = new Mock<IGroupManager>(MockBehavior.Loose);
        _mockGroups
            .Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new DbContextOptionsBuilder<TalleresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TalleresDbContext(options);

        _originalConsoleOut = Console.Out;
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _consoleOutput.Dispose();
        Console.SetOut(_originalConsoleOut);
    }

    private SignalHub CreateHub()
    {
        var hub = new SignalHub(_dbContext);
        hub.Context = _mockContext.Object;
        hub.Groups = _mockGroups.Object;
        return hub;
    }

    private void SeedTournaments(params Tournaments[] tournaments)
    {
        _dbContext.Partidos.AddRange(tournaments);
        _dbContext.SaveChanges();
    }

    private static Tournaments CreateTournament(
        Ulid? id = null,
        Ulid? owner = null,
        List<Ulid>? workers = null,
        List<Ulid>? supervisors = null,
        bool isDeleted = false)
    {
        return new Tournaments
        {
            Id = id ?? Ulid.NewUlid(),
            Owner = owner ?? Ulid.NewUlid(),
            Title = "Test Tournament",
            WorkersList = workers ?? [],
            SupervisorList = supervisors ?? [],
            IsDeleted = isDeleted,
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(7)
        };
    }

    #region Admin

    [Test]
    public async Task OnConnectedAsync_WhenAdmin_JoinsAdminGroupAndIgnoresTournamentGroups()
    {
        var adminPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Role, "ADMIN")]));
        _mockContext.Setup(c => c.User).Returns(adminPrincipal);
        SeedTournaments(CreateTournament(owner: UserUlid));

        var hub = CreateHub();
        await hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync(ConnectionId, "Tournament_All_Admin", It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGroups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(),
                It.Is<string>(s => s.StartsWith("Tournament_") && s != "Tournament_All_Admin"),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Owned tournaments

    [Test]
    public async Task OnConnectedAsync_WhenOwnsTournament_JoinsTournamentGroup()
    {
        var tournament = CreateTournament(owner: UserUlid);
        SeedTournaments(tournament);

        var hub = CreateHub();
        await hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync(ConnectionId, $"Tournament_{tournament.Id}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_WhenOwnsMultipleTournaments_JoinsAllGroups()
    {
        var t1 = CreateTournament(owner: UserUlid);
        var t2 = CreateTournament(owner: UserUlid);
        SeedTournaments(t1, t2);

        var hub = CreateHub();
        await hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync(ConnectionId, $"Tournament_{t1.Id}", It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGroups.Verify(
            g => g.AddToGroupAsync(ConnectionId, $"Tournament_{t2.Id}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_WhenTournamentDeleted_DoesNotJoinGroup()
    {
        var alive = CreateTournament(owner: UserUlid);
        var deleted = CreateTournament(owner: UserUlid, isDeleted: true);
        SeedTournaments(alive);
        _dbContext.Partidos.Add(deleted);
        _dbContext.SaveChanges();

        var hub = CreateHub();
        await hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), $"Tournament_{alive.Id}", It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGroups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), $"Tournament_{deleted.Id}", It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region WorkersList and SupervisorList

    [Test]
    public async Task OnConnectedAsync_WhenInWorkersList_JoinsGroup()
    {
        var tournament = CreateTournament(workers: [UserUlid]);
        SeedTournaments(tournament);

        var hub = CreateHub();
        await hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync(ConnectionId, $"Tournament_{tournament.Id}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_WhenInSupervisorList_JoinsGroup()
    {
        var tournament = CreateTournament(supervisors: [UserUlid]);
        SeedTournaments(tournament);

        var hub = CreateHub();
        await hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync(ConnectionId, $"Tournament_{tournament.Id}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_WhenOwnerAndWorker_DoesNotDuplicate()
    {
        var tournament = CreateTournament(owner: UserUlid, workers: [UserUlid]);
        SeedTournaments(tournament);

        var hub = CreateHub();
        await hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync(ConnectionId, $"Tournament_{tournament.Id}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Edge cases

    [Test]
    public async Task OnConnectedAsync_WhenInvalidUlid_JoinsNoGroups()
    {
        _mockContext.Setup(c => c.UserIdentifier).Returns("not-a-valid-ulid");

        var hub = CreateHub();
        await hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task OnConnectedAsync_WhenUserIdentifierIsNull_JoinsNoGroups()
    {
        _mockContext.Setup(c => c.UserIdentifier).Returns((string?)null);

        var hub = CreateHub();
        await hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task OnConnectedAsync_WhenNoMatchingTournaments_JoinsNoGroups()
    {
        SeedTournaments(CreateTournament());

        var hub = CreateHub();
        await hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task OnConnectedAsync_WhenUserIsNull_DoesNotThrow()
    {
        _mockContext.Setup(c => c.User).Returns((ClaimsPrincipal?)null);

        var hub = CreateHub();
        await hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Exception handling

    [Test]
    public async Task OnConnectedAsync_WhenDbContextThrows_LogsErrorAndCompletes()
    {
        var options = new DbContextOptionsBuilder<TalleresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new TalleresDbContext(options);
        context.Partidos.Add(CreateTournament(owner: UserUlid));
        context.SaveChanges();
        context.Dispose();

        var hub = new SignalHub(context);
        hub.Context = _mockContext.Object;
        hub.Groups = _mockGroups.Object;

        await hub.OnConnectedAsync();

        _consoleOutput.ToString().Should().Contain("Error en SignalHub.OnConnectedAsync");
    }

    #endregion
}
