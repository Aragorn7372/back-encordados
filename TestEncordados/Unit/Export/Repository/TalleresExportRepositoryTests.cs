using BackEncordados.Common.Database.Config;
using BackEncordados.Export.Repository;
using BackEncordados.Talleres.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestEncordados.Unit.Export.Repository;

public class TalleresExportRepositoryTests
{
    private static DbContextOptions<TalleresDbContext> CreateInMemoryOptions(string dbName)
    {
        return new DbContextOptionsBuilder<TalleresDbContext>()
            .UseInMemoryDatabase($"TalleresExport_{dbName}_{Guid.NewGuid()}")
            .Options;
    }

    private static TalleresExportRepository CreateRepository(TalleresDbContext context)
    {
        var logger = new Mock<ILogger<TalleresExportRepository>>();
        return new TalleresExportRepository(context, logger.Object);
    }

    [Test]
    public async Task GetTournamentsDataAsync_WhenDataExists_ReturnsAllTournaments()
    {
        var options = CreateInMemoryOptions(nameof(GetTournamentsDataAsync_WhenDataExists_ReturnsAllTournaments));
        using var context = new TalleresDbContext(options);

        context.Partidos.AddRange(
            new Tournaments { Id = Ulid.NewUlid(), Title = "Tournament 1", Owner = Ulid.NewUlid() },
            new Tournaments { Id = Ulid.NewUlid(), Title = "Tournament 2", Owner = Ulid.NewUlid() }
        );
        await context.SaveChangesAsync();

        var repo = CreateRepository(context);

        var result = await repo.GetTournamentsDataAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Title == "Tournament 1");
        result.Should().Contain(t => t.Title == "Tournament 2");
    }

    [Test]
    public async Task GetTournamentsDataAsync_WhenEmpty_ReturnsEmptyList()
    {
        var options = CreateInMemoryOptions(nameof(GetTournamentsDataAsync_WhenEmpty_ReturnsEmptyList));
        using var context = new TalleresDbContext(options);
        var repo = CreateRepository(context);

        var result = await repo.GetTournamentsDataAsync();

        result.Should().BeEmpty();
    }

    [Test]
    public async Task ClearTournamentsAsync_WithData_ClearsAllTournaments()
    {
        var options = CreateInMemoryOptions(nameof(ClearTournamentsAsync_WithData_ClearsAllTournaments));
        using var context = new TalleresDbContext(options);

        context.Partidos.AddRange(
            new Tournaments { Id = Ulid.NewUlid(), Title = "T1", Owner = Ulid.NewUlid() },
            new Tournaments { Id = Ulid.NewUlid(), Title = "T2", Owner = Ulid.NewUlid() }
        );
        await context.SaveChangesAsync();

        var repo = CreateRepository(context);

        await repo.ClearTournamentsAsync();

        var remaining = await context.Partidos.IgnoreQueryFilters().ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Test]
    public async Task ImportTournamentsAsync_WithEmptyList_DoesNothing()
    {
        var options = CreateInMemoryOptions(nameof(ImportTournamentsAsync_WithEmptyList_DoesNothing));
        using var context = new TalleresDbContext(options);
        var repo = CreateRepository(context);

        await repo.ImportTournamentsAsync(new List<Tournaments>());

        var count = await context.Partidos.IgnoreQueryFilters().CountAsync();
        count.Should().Be(0);
    }

    [Test]
    public async Task ImportTournamentsAsync_WithTournaments_ImportsSuccessfully()
    {
        var options = CreateInMemoryOptions(nameof(ImportTournamentsAsync_WithTournaments_ImportsSuccessfully));
        using var context = new TalleresDbContext(options);
        var repo = CreateRepository(context);

        var tournaments = new List<Tournaments>
        {
            new() { Id = Ulid.NewUlid(), Title = "New Tournament", Owner = Ulid.NewUlid() }
        };

        await repo.ImportTournamentsAsync(tournaments);

        var saved = await context.Partidos.IgnoreQueryFilters().ToListAsync();
        saved.Should().HaveCount(1);
        saved[0].Title.Should().Be("New Tournament");
    }

    [Test]
    public async Task ImportTournamentsAsync_WithWorkerMachineAssignments_ImportsAssignments()
    {
        var options = CreateInMemoryOptions(nameof(ImportTournamentsAsync_WithWorkerMachineAssignments_ImportsAssignments));
        using var context = new TalleresDbContext(options);
        var repo = CreateRepository(context);

        var tournamentId = Ulid.NewUlid();
        var tournaments = new List<Tournaments>
        {
            new()
            {
                Id = tournamentId,
                Title = "With Assignments",
                Owner = Ulid.NewUlid()
            }
        };

        await repo.ImportTournamentsAsync(tournaments);

        var saved = await context.Partidos
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == tournamentId);

        saved.Should().NotBeNull();
        saved.Title.Should().Be("With Assignments");
    }
}
