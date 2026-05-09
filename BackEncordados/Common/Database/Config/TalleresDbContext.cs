using BackEncordados.Common.Database.Helpers;
using BackEncordados.Talleres.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BackEncordados.Common.Database.Config;

public class TalleresDbContext(DbContextOptions<TalleresDbContext> options)
    : DbContext(options)
{
    public DbSet<Tournaments> Partidos { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureTournaments(modelBuilder);

        SeedData(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void ConfigureTournaments(ModelBuilder modelBuilder)
    {
        var ulidListConverter = new ValueConverter<List<Ulid>, string>(
            v => string.Join(";", v),
            v => string.IsNullOrWhiteSpace(v)
                ? new List<Ulid>()
                : v.Split(";", StringSplitOptions.RemoveEmptyEntries)
                    .Select(Ulid.Parse)
                    .ToList()
        );

        modelBuilder.Entity<Tournaments>(entity =>
        {
            entity.ToTable("Tournaments");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Logotype)
                .HasMaxLength(500);

            entity.ConfigureTimestamps();

            entity.Property(x => x.StartTournament)
                .IsRequired();

            entity.Property(x => x.EndTournament)
                .IsRequired();

            entity.Property(x => x.WorkersList)
                .HasConversion(
                    ulidListConverter,
                    new ValueComparer<List<Ulid>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c == null ? 0 : c.Aggregate(0, (acc, v) => HashCode.Combine(acc, v)),
                        c => c == null ? new List<Ulid>() : c.ToList()))
                .HasColumnType("text");
            entity.Property(x => x.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
            entity.HasQueryFilter(u => !u.IsDeleted);

            // Owned collection embebida
            entity.OwnsMany(x => x.WorkerMachineAssignments, owned =>
            {
                owned.ToTable("TournamentWorkerMachineAssignments");

                owned.WithOwner()
                    .HasForeignKey("TournamentId");

                owned.Property<long>("Id");
                owned.HasKey("Id");

                owned.Property(x => x.WorkerId)
                    .IsRequired();

                owned.Property(x => x.MachineName)
                    .IsRequired()
                    .HasMaxLength(100);
            });
        });
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;
        
     
        var carlos = Ulid.Parse("01KR424NQJKSBKBMH15K4V835W");
        var maria = Ulid.Parse("01KR424NQJKMNYS1FEC7NXBBH2");
        

        modelBuilder.Entity<Tournaments>().HasData(
            new Tournaments
            {
                Id = 1,
                Title = "Torneo Madrileño 2025",
                Logotype = "/logos/torneo-madrid.jpg",
                StartTournament = now.AddDays(-7),
                EndTournament = now.AddDays(7),
                WorkersList = new List<Ulid> { carlos, maria },
                IsDeleted = false,
                CreatedAt = now.AddMonths(-2),
                UpdatedAt = now.AddMonths(-2)
            },
            new Tournaments
            {
                Id = 2,
                Title = "Open Circuit Barcelona",
                Logotype = "/logos/torneo-barcelona.jpg",
                StartTournament = now.AddDays(14),
                EndTournament = now.AddDays(28),
                WorkersList = new List<Ulid> { carlos },
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            },
            new Tournaments
            {
                Id = 3,
                Title = "Campeonato Regional Valencia",
                Logotype = "/logos/torneo-valencia.jpg",
                StartTournament = now.AddDays(-60),
                EndTournament = now.AddDays(-45),
                WorkersList = new List<Ulid> { maria },
                IsDeleted = false,
                CreatedAt = now.AddMonths(-4),
                UpdatedAt = now.AddMonths(-3)
            },
            new Tournaments
            {
                Id = 4,
                Title = "Cup Toledo - Elite",
                Logotype = "/logos/torneo-toledo.jpg",
                StartTournament = now.AddDays(35),
                EndTournament = now.AddDays(42),
                WorkersList = new List<Ulid> { carlos, maria },
                IsDeleted = false,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-30)
            }
        );

        modelBuilder.Entity<Tournaments>()
            .OwnsMany(t => t.WorkerMachineAssignments)
            .HasData(
                new { Id = 1L, TournamentId = 1L, WorkerId = carlos, MachineName = "Máquina Alpha-1" },
                new { Id = 2L, TournamentId = 1L, WorkerId = maria, MachineName = "Máquina Beta-2" },
                new { Id = 3L, TournamentId = 2L, WorkerId = carlos, MachineName = "Máquina Alpha-1" },
                new { Id = 4L, TournamentId = 3L, WorkerId = maria, MachineName = "Máquina Beta-2" },
                new { Id = 5L, TournamentId = 4L, WorkerId = carlos, MachineName = "Máquina Alpha-1" },
                new { Id = 6L, TournamentId = 4L, WorkerId = maria, MachineName = "Máquina Beta-2" },
                new { Id = 7L, TournamentId = 4L, WorkerId = maria, MachineName = "Máquina Gamma-3" }
            );
    }
}