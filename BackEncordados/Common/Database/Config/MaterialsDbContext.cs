using BackEncordados.Common.Database.Helpers;
using BackEncordados.Materials.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.Database.Config;

public class MaterialsDbContext(DbContextOptions<MaterialsDbContext> options): DbContext(options)
{
    public DbSet<Cuerdas> Cuerdas { get; set; }
    public DbSet<Material> Materiales { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        SeedData(modelBuilder);

        modelBuilder.Entity<Cuerdas>(builder =>
        {
            builder.ToTable("Cuerdas"); 
            builder.HasKey(c => c.Id);  
            builder.Property(c => c.TournamentId).IsRequired();

            builder.Property(c => c.Marca).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Modelo).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Stock).IsRequired();
            builder.Property(c => c.Precio).IsRequired();

            builder.Property(c => c.StringFormat)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(c => c.StringsType)
                .HasConversion<string>()
                .IsRequired();
            builder.ConfigureTimestamps();
            builder.HasQueryFilter(u => !u.IsDeleted);
        });

        modelBuilder.Entity<Material>(builder =>
        {
            builder.ToTable("Materiales");
            builder.HasKey(m => m.Id); 
            builder.Property(c => c.TournamentId).IsRequired();
            builder.Property(m => m.Marca).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Modelo).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Stock).IsRequired();
            builder.Property(m => m.Precio).IsRequired();

            builder.Property(m => m.Type)
                .HasConversion<string>()
                .IsRequired();

            builder.ConfigureTimestamps();
            
            builder.HasQueryFilter(u => !u.IsDeleted);
        });
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;

        modelBuilder.Entity<Cuerdas>().HasData(
            new Cuerdas
            {
                Id = 1,
                TournamentId = 1,
                Marca = "Babolat",
                Modelo = "RPM Blast",
                Stock = 50,
                Precio = 19.99,
                StringFormat = FormatoCuerda.Reel,
                StringsType = StringsType.Polyester,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-2),
                UpdatedAt = now.AddMonths(-2)
            },
            new Cuerdas
            {
                Id = 2,
                TournamentId = 1,
                Marca = "Wilson",
                Modelo = "Champion's Choice",
                Stock = 20,
                Precio = 24.99,
                StringFormat = FormatoCuerda.Set,
                StringsType = StringsType.NaturalGut,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-2),
                UpdatedAt = now.AddMonths(-2)
            },
            new Cuerdas
            {
                Id = 3,
                TournamentId = 2,
                Marca = "Luxilon",
                Modelo = "ALU Power",
                Stock = 45,
                Precio = 21.99,
                StringFormat = FormatoCuerda.Reel,
                StringsType = StringsType.Polyester,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            },
            new Cuerdas
            {
                Id = 4,
                TournamentId = 2,
                Marca = "Head",
                Modelo = "Intellitour",
                Stock = 30,
                Precio = 18.50,
                StringFormat = FormatoCuerda.Set,
                StringsType = StringsType.Multifilament,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            },
            new Cuerdas
            {
                Id = 5,
                TournamentId = 3,
                Marca = "Signum Pro",
                Modelo = "Plasma",
                Stock = 25,
                Precio = 17.99,
                StringFormat = FormatoCuerda.Reel,
                StringsType = StringsType.Hybrid,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-4),
                UpdatedAt = now.AddMonths(-3)
            },
            new Cuerdas
            {
                Id = 6,
                TournamentId = 4,
                Marca = "Kirschbaum",
                Modelo = "Pro Line",
                Stock = 35,
                Precio = 15.99,
                StringFormat = FormatoCuerda.Set,
                StringsType = StringsType.SyntheticGut,
                IsDeleted = false,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-30)
            }
        );

        modelBuilder.Entity<Material>().HasData(
            new Material
            {
                Id = 1,
                TournamentId = 1,
                Marca = "Head",
                Modelo = "Hydrosorb Comfort",
                Stock = 100,
                Precio = 8.99,
                Type = MaterialType.Grip,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-2),
                UpdatedAt = now.AddMonths(-2)
            },
            new Material
            {
                Id = 2,
                TournamentId = 1,
                Marca = "Babolat",
                Modelo = "Pro Overgrip",
                Stock = 200,
                Precio = 3.99,
                Type = MaterialType.Overgrip,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-2),
                UpdatedAt = now.AddMonths(-2)
            },
            new Material
            {
                Id = 3,
                TournamentId = 2,
                Marca = "Tourna",
                Modelo = "Lead Tape 1/2\"",
                Stock = 80,
                Precio = 12.99,
                Type = MaterialType.LeadTape,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            },
            new Material
            {
                Id = 4,
                TournamentId = 2,
                Marca = "Wilson",
                Modelo = "ShockShield",
                Stock = 50,
                Precio = 9.99,
                Type = MaterialType.Silicone,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            },
            new Material
            {
                Id = 5,
                TournamentId = 3,
                Marca = "Head",
                Modelo = "Graphene 360+",
                Stock = 10,
                Precio = 199.99,
                Type = MaterialType.Raquet,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-4),
                UpdatedAt = now.AddMonths(-3)
            },
            new Material
            {
                Id = 6,
                TournamentId = 4,
                Marca = "Babolat",
                Modelo = "Syn Pro",
                Stock = 75,
                Precio = 6.99,
                Type = MaterialType.Grip,
                IsDeleted = false,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-30)
            }
        );
    }
}
