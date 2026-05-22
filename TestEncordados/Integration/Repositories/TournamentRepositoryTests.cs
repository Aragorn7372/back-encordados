using BackEncordados.Common.Database.Config;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Model;
using BackEncordados.Talleres.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace TestEncordados.Integration.Repositories;

public class TournamentRepositoryTests
{
    private TalleresDbContext _context = null!;
    private ITournamentRepository _repository = null!;
    private Mock<ILogger<TournamentRepository>> _loggerMock = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<TalleresDbContext>()
            .UseInMemoryDatabase("TournamentRepositoryTestDb_" + Guid.NewGuid())
            .Options;

        _context = new TalleresDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _loggerMock = new Mock<ILogger<TournamentRepository>>();
        _repository = new TournamentRepository(_context, _loggerMock.Object);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    // ──────────────────────── SaveAsync ────────────────────────

    [Test]
    public async Task SaveAsync_ValidTournament_ReturnsSavedTournament()
    {
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Test Tournament",
            StartTournament = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _repository.SaveAsync(tournament);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(default);
        result.Title.Should().Be("Test Tournament");
        result.Owner.Should().Be(tournament.Owner);
    }

    // ──────────────────────── FindByIdAsync ────────────────────────

    [Test]
    public async Task FindByIdAsync_ExistingTournament_ReturnsTournament()
    {
        var ownerId = Ulid.NewUlid();
        var tournament = new Tournaments
        {
            Owner = ownerId,
            Title = "FindById Test",
            StartTournament = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);

        var result = await _repository.FindByIdAsync(saved.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(saved.Id);
        result.Title.Should().Be("FindById Test");
        result.Owner.Should().Be(ownerId);
    }

    [Test]
    public async Task FindByIdAsync_NonExistingTournament_ReturnsNull()
    {
        var result = await _repository.FindByIdAsync(Ulid.NewUlid());
        result.Should().BeNull();
    }

    [Test]
    public async Task FindByIdAsync_DeletedTournament_ReturnsNull()
    {
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "To Delete",
            StartTournament = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 8, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);
        await _repository.DeleteAsync(saved.Id);

        var result = await _repository.FindByIdAsync(saved.Id);
        result.Should().BeNull();
    }

    // ──────────────────────── FindByNameAsync ────────────────────────

    [Test]
    public async Task FindByNameAsync_ExistingTournament_ReturnsTournament()
    {
        var ownerId = Ulid.NewUlid();
        var tournament = new Tournaments
        {
            Owner = ownerId,
            Title = "UniqueName_XYZ",
            StartTournament = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 9, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);

        var result = await _repository.FindByNameAsync("UniqueName_XYZ");

        result.Should().NotBeNull();
        result!.Id.Should().Be(saved.Id);
    }

    [Test]
    public async Task FindByNameAsync_NonExistingTournament_ReturnsNull()
    {
        var result = await _repository.FindByNameAsync("NonExistentName");
        result.Should().BeNull();
    }

    [Test]
    public async Task FindByNameAsync_DeletedTournament_ReturnsNull()
    {
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "DeleteByName",
            StartTournament = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);
        await _repository.DeleteAsync(saved.Id);

        var result = await _repository.FindByNameAsync("DeleteByName");
        result.Should().BeNull();
    }

    // ──────────────────────── UpdateAsync ────────────────────────

    [Test]
    public async Task UpdateAsync_ExistingTournament_UpdatesFields()
    {
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Original Title",
            StartTournament = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);

        var updated = new Tournaments
        {
            Title = "Updated Title",
            StartTournament = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            Logotype = "new_logo.jpg"
        };

        var result = await _repository.UpdateAsync(saved.Id, updated);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.StartTournament.Should().Be(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        result.EndTournament.Should().Be(new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc));
        result.Logotype.Should().Be("new_logo.jpg");
    }

    [Test]
    public async Task UpdateAsync_NonExistingTournament_ReturnsNull()
    {
        var updated = new Tournaments { Title = "Anything" };
        var result = await _repository.UpdateAsync(Ulid.NewUlid(), updated);
        result.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_DeletedTournament_ReturnsNull()
    {
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "To Update After Delete",
            StartTournament = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);
        await _repository.DeleteAsync(saved.Id);

        var updated = new Tournaments { Title = "Should Not Update" };
        var result = await _repository.UpdateAsync(saved.Id, updated);
        result.Should().BeNull();
    }

    // ──────────────────────── DeleteAsync ────────────────────────

    [Test]
    public async Task DeleteAsync_ExistingTournament_MarksAsDeleted()
    {
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "To Delete",
            StartTournament = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);

        var result = await _repository.DeleteAsync(saved.Id);

        result.Should().BeTrue();
        var deleted = await _context.Partidos.IgnoreQueryFilters().FirstAsync(t => t.Id == saved.Id);
        deleted.IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_NonExistingTournament_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(Ulid.NewUlid());
        result.Should().BeFalse();
    }

    [Test]
    public async Task DeleteAsync_AlreadyDeletedTournament_ReturnsFalse()
    {
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Already Deleted",
            StartTournament = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);
        await _repository.DeleteAsync(saved.Id);

        var result = await _repository.DeleteAsync(saved.Id);
        result.Should().BeFalse();
    }

    // ──────────────────────── FindAllAsync ────────────────────────

    [Test]
    public async Task FindAllAsync_WithoutFilters_ReturnsAllNonDeleted()
    {
        var ownerId = Ulid.NewUlid();
        for (int i = 0; i < 3; i++)
        {
            await _repository.SaveAsync(new Tournaments
            {
                Owner = ownerId,
                Title = $"Tournament {i}",
                StartTournament = new DateTime(2025, 6, 1 + i, 0, 0, 0, DateTimeKind.Utc),
                EndTournament = new DateTime(2025, 6, 10 + i, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                UpdatedAt = DateTime.UtcNow
            });
        }

        var filter = new FilterTournamentDto(string.Empty, null, 0, 50, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        // Seed data (5) + our 3
        totalCount.Should().Be(8);
        items.Count().Should().Be(8);
    }

    [Test]
    public async Task FindAllAsync_FilterByUserId_ReturnsOnlyRelated()
    {
        var userId = Ulid.NewUlid();
        var tournament1 = new Tournaments
        {
            Owner = userId,
            Title = "Owned Tournament",
            StartTournament = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var tournament2 = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Worker Tournament",
            WorkersList = new List<Ulid> { userId },
            StartTournament = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 8, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var tournament3 = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Supervisor Tournament",
            SupervisorList = new List<Ulid> { userId },
            StartTournament = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 9, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var tournament4 = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Unrelated Tournament",
            StartTournament = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(tournament1);
        await _repository.SaveAsync(tournament2);
        await _repository.SaveAsync(tournament3);
        await _repository.SaveAsync(tournament4);

        var filter = new FilterTournamentDto(string.Empty, userId, 0, 50, "title", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(3);
        items.Select(t => t.Title).Should().Contain(new[] { "Owned Tournament", "Worker Tournament", "Supervisor Tournament" });
        items.Select(t => t.Title).Should().NotContain("Unrelated Tournament");
    }

    [Test]
    public async Task FindAllAsync_SearchByUlid_ReturnsMatching()
    {
        var ownerId = Ulid.NewUlid();
        var tournamentId = Ulid.NewUlid();
        // The repository applies BOTH Id filter and Title.Contains filter sequentially.
        // Title must contain the search string for both filters to pass.
        var tournament = new Tournaments
        {
            Id = tournamentId,
            Owner = ownerId,
            Title = $"Tournament {tournamentId}",
            StartTournament = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 11, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);

        var filter = new FilterTournamentDto(saved.Id.ToString()!, null, 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        // Assert
        totalCount.Should().Be(1);
        items.First().Id.Should().Be(saved.Id);
    }

    [Test]
    public async Task FindAllAsync_SearchByTitle_ReturnsMatching()
    {
        var ownerId = Ulid.NewUlid();
        await _repository.SaveAsync(new Tournaments
        {
            Owner = ownerId,
            Title = "Alpha Searchable Tournament",
            StartTournament = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _repository.SaveAsync(new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Beta Tournament",
            StartTournament = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var filter = new FilterTournamentDto("Alpha", null, 0, 10, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().Title.Should().Be("Alpha Searchable Tournament");
    }

    [Test]
    public async Task FindAllAsync_SortByTitleAsc_ReturnsSorted()
    {
        var ownerId = Ulid.NewUlid();
        await _repository.SaveAsync(new Tournaments
        {
            Owner = ownerId,
            Title = "Z Tournament",
            StartTournament = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _repository.SaveAsync(new Tournaments
        {
            Owner = ownerId,
            Title = "A Tournament",
            StartTournament = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var filter = new FilterTournamentDto(string.Empty, ownerId, 0, 10, "title", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        items.ElementAt(0).Title.Should().Be("A Tournament");
        items.ElementAt(1).Title.Should().Be("Z Tournament");
    }

    [Test]
    public async Task FindAllAsync_SortByStartDesc_ReturnsSorted()
    {
        var ownerId = Ulid.NewUlid();
        await _repository.SaveAsync(new Tournaments
        {
            Owner = ownerId,
            Title = "Earlier",
            StartTournament = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _repository.SaveAsync(new Tournaments
        {
            Owner = ownerId,
            Title = "Later",
            StartTournament = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var filter = new FilterTournamentDto(string.Empty, ownerId, 0, 10, "start", "desc");
        var (items, _) = await _repository.FindAllAsync(filter);

        items.ElementAt(0).Title.Should().Be("Later");
        items.ElementAt(1).Title.Should().Be("Earlier");
    }

    [Test]
    public async Task FindAllAsync_SortByEndAsc_ReturnsSorted()
    {
        var ownerId = Ulid.NewUlid();
        await _repository.SaveAsync(new Tournaments
        {
            Owner = ownerId,
            Title = "Short",
            StartTournament = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _repository.SaveAsync(new Tournaments
        {
            Owner = ownerId,
            Title = "Long",
            StartTournament = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var filter = new FilterTournamentDto(string.Empty, ownerId, 0, 10, "end", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        items.ElementAt(0).Title.Should().Be("Short");
        items.ElementAt(1).Title.Should().Be("Long");
    }

    [Test]
    public async Task FindAllAsync_WithPagination_ReturnsCorrectPage()
    {
        var ownerId = Ulid.NewUlid();
        for (int i = 1; i <= 5; i++)
        {
            await _repository.SaveAsync(new Tournaments
            {
                Owner = ownerId,
                Title = $"Pagination {i}",
                StartTournament = new DateTime(2025, 6, i, 0, 0, 0, DateTimeKind.Utc),
                EndTournament = new DateTime(2025, 6, i + 10, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow.AddMinutes(i),
                UpdatedAt = DateTime.UtcNow
            });
        }

        var filter = new FilterTournamentDto(string.Empty, ownerId, 1, 2, "createdat", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Test]
    public async Task FindAllAsync_DeletedTournament_ExcludedFromResults()
    {
        var ownerId = Ulid.NewUlid();
        var t1 = await _repository.SaveAsync(new Tournaments
        {
            Owner = ownerId,
            Title = "Active Tournament",
            StartTournament = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        var t2 = await _repository.SaveAsync(new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Will Be Deleted",
            StartTournament = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _repository.DeleteAsync(t2.Id);

        var filter = new FilterTournamentDto(string.Empty, ownerId, 0, 50, "id", "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().Id.Should().Be(t1.Id);
    }

    // ──────────────────────── AsignWorker ────────────────────────

    [Test]
    public async Task AsignWorker_ValidTournament_AddsWorkerAndAssignment()
    {
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Worker Assignment Test",
            StartTournament = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);
        var workerId = Ulid.NewUlid();

        var result = await _repository.AsignWorker(saved.Id, workerId, "Machine Alpha");

        result.Should().NotBeNull();
        result!.WorkersList.Should().Contain(workerId);
        result.WorkerMachineAssignments.Should().ContainSingle(a => a.WorkerId == workerId);
        result.WorkerMachineAssignments.First(a => a.WorkerId == workerId).MachineName.Should().Be("Machine Alpha");
    }

    [Test]
    public async Task AsignWorker_DuplicateWorkerId_DoesNotAddDuplicate()
    {
        var workerId = Ulid.NewUlid();
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Duplicate Worker Test",
            WorkersList = new List<Ulid> { workerId },
            StartTournament = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);

        var result = await _repository.AsignWorker(saved.Id, workerId, "Machine Beta");

        result.Should().NotBeNull();
        result!.WorkersList.Count(w => w == workerId).Should().Be(1);
    }

    [Test]
    public async Task AsignWorker_NonExistingTournament_ReturnsNull()
    {
        var result = await _repository.AsignWorker(Ulid.NewUlid(), Ulid.NewUlid(), "Machine");
        result.Should().BeNull();
    }

    // ──────────────────────── RemoveWorker ────────────────────────

    [Test]
    public async Task RemoveWorker_ExistingWorker_RemovesWorkerAndAssignments()
    {
        var workerId = Ulid.NewUlid();
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Remove Worker Test",
            WorkersList = new List<Ulid> { workerId },
            WorkerMachineAssignments = new List<WorkerMachineAssignment>
            {
                new() { Id = 100, WorkerId = workerId, MachineName = "Machine Alpha" }
            },
            StartTournament = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);

        var result = await _repository.RemoveWorker(saved.Id, workerId);

        result.Should().NotBeNull();
        result!.WorkersList.Should().NotContain(workerId);
        result.WorkerMachineAssignments.Should().NotContain(a => a.WorkerId == workerId);
    }

    [Test]
    public async Task RemoveWorker_NonExistingTournament_ReturnsNull()
    {
        var result = await _repository.RemoveWorker(Ulid.NewUlid(), Ulid.NewUlid());
        result.Should().BeNull();
    }

    // ──────────────────────── GetAssignedWorkerMachinesAsync ────────────────────────

    [Test]
    public async Task GetAssignedWorkerMachinesAsync_WithAssignments_ReturnsList()
    {
        var workerId = Ulid.NewUlid();
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Get Machines Test",
            WorkerMachineAssignments = new List<WorkerMachineAssignment>
            {
                new() { Id = 100, WorkerId = workerId, MachineName = "Machine A" },
                new() { Id = 101, WorkerId = Ulid.NewUlid(), MachineName = "Machine B" }
            },
            StartTournament = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);

        var result = await _repository.GetAssignedWorkerMachinesAsync(saved.Id);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetAssignedWorkerMachinesAsync_NoAssignments_ReturnsEmpty()
    {
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "No Machines",
            StartTournament = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);

        var result = await _repository.GetAssignedWorkerMachinesAsync(saved.Id);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetAssignedWorkerMachinesAsync_NonExistingTournament_ReturnsNull()
    {
        var result = await _repository.GetAssignedWorkerMachinesAsync(Ulid.NewUlid());
        result.Should().BeNull();
    }

    [Test]
    public async Task GetAssignedWorkerMachinesAsync_DeletedTournament_ReturnsNull()
    {
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Deleted Get Machines",
            StartTournament = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);
        await _repository.DeleteAsync(saved.Id);

        var result = await _repository.GetAssignedWorkerMachinesAsync(saved.Id);
        result.Should().BeNull();
    }

    // ──────────────────────── AsignSupervisor ────────────────────────

    [Test]
    public async Task AsignSupervisor_ValidTournament_AddsSupervisor()
    {
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Supervisor Add Test",
            StartTournament = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);
        var supervisorId = Ulid.NewUlid();

        var result = await _repository.AsignSupervisor(saved.Id, supervisorId);

        result.Should().NotBeNull();
        result!.SupervisorList.Should().Contain(supervisorId);
    }

    [Test]
    public async Task AsignSupervisor_DuplicateSupervisor_DoesNotAddDuplicate()
    {
        var supervisorId = Ulid.NewUlid();
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Duplicate Supervisor Test",
            SupervisorList = new List<Ulid> { supervisorId },
            StartTournament = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);

        var result = await _repository.AsignSupervisor(saved.Id, supervisorId);

        result.Should().NotBeNull();
        result!.SupervisorList.Count(s => s == supervisorId).Should().Be(1);
    }

    [Test]
    public async Task AsignSupervisor_NonExistingTournament_ReturnsNull()
    {
        var result = await _repository.AsignSupervisor(Ulid.NewUlid(), Ulid.NewUlid());
        result.Should().BeNull();
    }

    // ──────────────────────── RemoveSupervisor ────────────────────────

    [Test]
    public async Task RemoveSupervisor_ExistingSupervisor_Removes()
    {
        var supervisorId = Ulid.NewUlid();
        var tournament = new Tournaments
        {
            Owner = Ulid.NewUlid(),
            Title = "Remove Supervisor Test",
            SupervisorList = new List<Ulid> { supervisorId },
            StartTournament = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2025, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var saved = await _repository.SaveAsync(tournament);

        var result = await _repository.RemoveSupervisor(saved.Id, supervisorId);

        result.Should().NotBeNull();
        result!.SupervisorList.Should().NotContain(supervisorId);
    }

    [Test]
    public async Task RemoveSupervisor_NonExistingTournament_ReturnsNull()
    {
        var result = await _repository.RemoveSupervisor(Ulid.NewUlid(), Ulid.NewUlid());
        result.Should().BeNull();
    }
}