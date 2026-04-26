using BackEncordados.Common.Database.Helpers;
using BackEncordados.Talleres.Model;
using Microsoft.EntityFrameworkCore;
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
        var guidListConverter = new ValueConverter<List<Guid>, string>(
            v => string.Join(";", v),
            v => string.IsNullOrWhiteSpace(v)
                ? new List<Guid>()
                : v.Split(";", StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse)
                    .ToList()
        );

        modelBuilder.Entity<Tournaments>(entity =>
        {
            entity.ToTable("Tournaments");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.logotype)
                .HasMaxLength(500);

            entity.ConfigureTimestamps();

            entity.Property(x => x.StartTournament)
                .IsRequired();

            entity.Property(x => x.EndTournament)
                .IsRequired();

            // List<Guid> -> string
            entity.Property(x => x.PurchasedList)
                .HasConversion(guidListConverter)
                .HasColumnType("text");

            entity.Property(x => x.WorkersList)
                .HasConversion(guidListConverter)
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
        // Seed opcional
    }
}