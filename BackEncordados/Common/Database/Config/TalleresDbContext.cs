using BackEncordados.Common.Database.Helpers;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Talleres.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BackEncordados.Common.Database.Config;

public class TalleresDbContext(DbContextOptions<TalleresDbContext> options)
    : DbContext(options)
{
    public DbSet<Tournaments> Partidos { get; set; } = null!;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Ulid>()
            .HaveConversion<UlidToStringConverterNonNullable>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureTournaments(modelBuilder);

        SeedData(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureTournaments(ModelBuilder modelBuilder)
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

            entity.Property(x => x.Id)
                .HasValueGenerator<UlidValueGenerator>();

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Logotype)
                .HasMaxLength(500);

            entity.Property(x => x.LogotypePublicId)
                .HasMaxLength(300);

            entity.ConfigureTimestamps();

            entity.Property(x => x.StartTournament)
                .IsRequired();

            entity.Property(x => x.EndTournament)
                .IsRequired();

            entity.Property(x => x.Owner)
                .IsRequired();

            entity.Property(x => x.WorkersList)
                .HasConversion(
                    ulidListConverter,
                    new ValueComparer<List<Ulid>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c == null ? 0 : c.Aggregate(0, (acc, v) => HashCode.Combine(acc, v)),
                        c => c == null ? new List<Ulid>() : c.ToList()))
                .HasColumnType("text");

            entity.Property(x => x.SupervisorList)
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

        var carlos = Ulid.Parse("01KS0Q28TE7CMWS2D8RVDFA7YJ");
        var maria = Ulid.Parse("01KS0Q28TE6CVB0NYYANTWEK7B");
        var owner= Ulid.Parse("01KS0Q28TE2EFVQTCW8EN0W0MF");
        var supervisor1 = Ulid.Parse("01KS0Q28TEHA2KF5YM3J6QS5Z9");
        var supervisor2 = Ulid.Parse("01KS0Q28TEXTDY9TQNRAXKAJ81");
        var t1 = Ulid.Parse("01KS0Q28TEJ0SYA6JJ5H4W4CMP");
        var t2 = Ulid.Parse("01KS0Q28TE9N7TG55K98TCB4X0");
        var t3= Ulid.Parse("01KS0Q28TEVEYS4303TXP202N4");
        var t4 = Ulid.Parse("01KS0Q28TET0JHJV4T5YFDJWBW");
        var t5 = Ulid.Parse("01KS0Q28TE5BA449NS2EVCBTDQ");

        modelBuilder.Entity<Tournaments>().HasData(
            new Tournaments
            {
                Id = t1,
                Title = "Torneo Madrileño 2025",
                Logotype = CloudinaryConstants.DEFAULT_IMAGE_TALLERES,
                LogotypePublicId = null,
                StartTournament = now.AddDays(-7),
                EndTournament = now.AddDays(7),
                WorkersList = new List<Ulid> { carlos, maria },
                SupervisorList = new List<Ulid> { supervisor1 },
                Owner = owner,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-2),
                UpdatedAt = now.AddMonths(-2)
            },
            
            new Tournaments
            {
                Id = t2,
                Title = "Open Circuit Barcelona",
                Logotype = CloudinaryConstants.DEFAULT_IMAGE_TALLERES,
                LogotypePublicId = null,
                StartTournament = now.AddDays(14),
                EndTournament = now.AddDays(28),
                WorkersList = new List<Ulid> { carlos },
                SupervisorList = new List<Ulid> { supervisor2 },
                Owner = owner,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            },
            new Tournaments
            {
                Id = t3,
                Title = "Campeonato Regional Valencia",
                Logotype = CloudinaryConstants.DEFAULT_IMAGE_TALLERES,
                LogotypePublicId = null,
                StartTournament = now.AddDays(-60),
                EndTournament = now.AddDays(-45),
                WorkersList = new List<Ulid> { maria },
                Owner = owner,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-4),
                UpdatedAt = now.AddMonths(-3)
            },
            new Tournaments
            {
                Id = t4,
                Title = "Cup Toledo - Elite",
                Logotype = CloudinaryConstants.DEFAULT_IMAGE_TALLERES,
                LogotypePublicId = null,
                StartTournament = now.AddDays(35),
                EndTournament = now.AddDays(42),
                WorkersList = new List<Ulid> { carlos, maria },
                SupervisorList = new List<Ulid> { supervisor1, supervisor2 },
                Owner = owner,
                IsDeleted = false,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-30)
            },

            new Tournaments
            {
                Id = t5,
                Title = "Torneo Prueba Excel",
                Logotype = CloudinaryConstants.DEFAULT_IMAGE_TALLERES,
                LogotypePublicId = null,
                StartTournament = now.AddDays(-10),
                EndTournament = now.AddDays(20),
                WorkersList = new List<Ulid> { carlos, maria },
                SupervisorList = new List<Ulid> { supervisor1 },
                Owner = owner,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            }
        );

        modelBuilder.Entity<Tournaments>()
            .OwnsMany(t => t.WorkerMachineAssignments)
            .HasData(
                new { Id = 1L, TournamentId = t1, WorkerId = carlos, MachineName = "Máquina Alpha-1" },
                new { Id = 2L, TournamentId = t1, WorkerId = maria, MachineName = "Máquina Beta-2" },
                new { Id = 3L, TournamentId = t2, WorkerId = carlos, MachineName = "Máquina Alpha-1" },
                new { Id = 4L, TournamentId = t3, WorkerId = maria, MachineName = "Máquina Beta-2" },
                new { Id = 5L, TournamentId = t4, WorkerId = carlos, MachineName = "Máquina Alpha-1" },
                new { Id = 6L, TournamentId = t4, WorkerId = maria, MachineName = "Máquina Beta-2" },
                new { Id = 7L, TournamentId = t4, WorkerId = maria, MachineName = "Máquina Gamma-3" }
            );
    }
}