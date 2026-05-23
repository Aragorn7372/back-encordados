using BackEncordados.Common.Database.Helpers;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Usuarios.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.Database.Config;

public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        SeedData(modelBuilder);
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasConversion<UlidToStringConverterNonNullable>().HasValueGenerator<UlidValueGenerator>();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100).IsUnicode(false);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
            entity.Property(u => u.Phone).HasMaxLength(20);
            entity.Property(u => u.ImageUrl).IsRequired().HasMaxLength(500);
            entity.Property(u => u.CloudinaryPublicId).HasMaxLength(300);
            entity.Property(u => u.IsDeleted).HasDefaultValue(false);
            entity.Property(u => u.Bonos).HasDefaultValue(0);
            entity.Property(u => u.Version).IsConcurrencyToken();
            entity.ConfigureTimestamps();
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_users_email_unique");
            entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("IX_users_username_unique");
            entity.HasQueryFilter(u => !u.IsDeleted);
            entity.Property(u => u.TournamentId).HasConversion<UlidToStringConverter>().IsRequired(false);
        });
    }

    public DbSet<User> Users { get; set; } = null!;

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Generar hashes de contraseñas para los datos de prueba
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 11);
        var userPasswordHash = BCrypt.Net.BCrypt.HashPassword("user123", workFactor: 11);
        var encorderPasswordHash = BCrypt.Net.BCrypt.HashPassword("encorder123", workFactor: 11);
        var ownerPasswordHash = BCrypt.Net.BCrypt.HashPassword("owner123", workFactor: 11);
        var supervisorPasswordHash = BCrypt.Net.BCrypt.HashPassword("supervisor123", workFactor: 11);
        var juan = Ulid.Parse("01KS0Q28TD6SAPN0GN0XKRPK5D");
        var ana = Ulid.Parse("01KS0Q28TE3RJTW6W35NJRMTZ4");
        var pedro = Ulid.Parse("01KS0Q28TED4PWJPT7DMJ46WBN");
        var carlos = Ulid.Parse("01KS0Q28TE7CMWS2D8RVDFA7YJ");
        var maria = Ulid.Parse("01KS0Q28TE6CVB0NYYANTWEK7B");
        var admin= Ulid.Parse("01KS0Q28TESE956013XYJKP6ST");
        var owner= Ulid.Parse("01KS0Q28TE2EFVQTCW8EN0W0MF");
        var supervisor1 = Ulid.Parse("01KS0Q28TEHA2KF5YM3J6QS5Z9");
        var supervisor2 = Ulid.Parse("01KS0Q28TEXTDY9TQNRAXKAJ81");
        var t1 = Ulid.Parse("01KS0Q28TEJ0SYA6JJ5H4W4CMP");
        var t2 = Ulid.Parse("01KS0Q28TE9N7TG55K98TCB4X0");
        var user = Ulid.Parse("01KS0Q28TE5BA449NS2EVCBTDQ");
        var users = new List<User>
        {
            
            // Usuario ADMIN
            new() {
                Id = admin,
                Username = "admin_encordados",
                Name = "Administrador",
                Email = "admin@encordados.com",
                Phone = "912345678",
                PasswordHash = adminPasswordHash,
                Role = User.UserRoles.ADMIN,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow.AddMonths(-6),
                TournamentId = null,
                
            },

            // Usuario OWNER
            new() {
                Id = owner,
                Username = "owner_principal",
                Name = "Propietario",
                Email = "owner@encordados.com",
                Phone = "923456789",
                PasswordHash = ownerPasswordHash,
                Role = User.UserRoles.OWNER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-5),
                UpdatedAt = DateTime.UtcNow.AddMonths(-5),
                TournamentId = null,
                
            },

              
            // Usuario ENCORDER 1
            new() {
                Id = carlos,
                Username = "carlos_encordador",
                Name = "Carlos García",
                Email = "carlos@encordados.com",
                Phone = "934567890",
                PasswordHash = encorderPasswordHash,
                Role = User.UserRoles.ENCORDER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                UpdatedAt = DateTime.UtcNow.AddMonths(-4),
                TournamentId = null,
                
            },

            // Usuario ENCORDER 2
            new() {
                Id = maria,
                Username = "maria_encordadora",
                Name = "María López",
                Email = "maria@encordados.com",
                Phone = "945678901",
                PasswordHash = encorderPasswordHash,
                Role = User.UserRoles.ENCORDER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UpdatedAt = DateTime.UtcNow.AddMonths(-3),
                TournamentId = null,
                
            },

            // Usuario JUGADOR 1
            new() {
                Id = juan,
                Username = "jugador_juan",
                Name = "Juan Martínez",
                Email = "juan@tenis.com",
                Phone = "956789012",
                PasswordHash = userPasswordHash,
                Role = User.UserRoles.USER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                UpdatedAt = DateTime.UtcNow.AddMonths(-2),
                TournamentId = t1,
                Bonos = 100.0,
                
            },

            // Usuario JUGADOR 2
            new() {
                Id = ana,
                Username = "jugador_ana",
                Name = "Ana Pérez",
                Email = "ana@tenis.com",
                Phone = "967890123",
                PasswordHash = userPasswordHash,
                Role = User.UserRoles.USER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                UpdatedAt = DateTime.UtcNow.AddMonths(-1),
                TournamentId = t1,
                Bonos = 25.0,
                
            },

            // Usuario JUGADOR 3
            new() {
                Id = pedro,
                Username = "jugador_pedro",
                Name = "Pedro Rodríguez",
                Email = "pedro@tenis.com",
                Phone = "978901234",
                PasswordHash = userPasswordHash,
                Role = User.UserRoles.USER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TournamentId = t2,
                Bonos = 0,
                
            },

            // Usuario SUPERVISOR 1
            new() {
                Id = supervisor1,
                    Username = "supervisor_luis",
                Name = "Luis Fernández",
                Email = "luis@encordados.com",
                Phone = "989012345",
                PasswordHash = supervisorPasswordHash,
                Role = User.UserRoles.SUPERVISOR,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UpdatedAt = DateTime.UtcNow.AddMonths(-3),
                TournamentId = null,
                
            },

            // Usuario SUPERVISOR 2
            new() {
                Id = supervisor2,
                Username = "supervisor_pablo",
                Name = "Pablo Sánchez",
                Email = "pablo@encordados.com",
                Phone = "990123456",
                PasswordHash = supervisorPasswordHash,
                Role = User.UserRoles.SUPERVISOR,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                UpdatedAt = DateTime.UtcNow.AddMonths(-2),
                TournamentId = null,
                
            },
            new() {
                Id = user,
                Username = "user",
                Name = "User",
                Email = "user@example.com",
                Phone = "999999000",
                PasswordHash = userPasswordHash,
                Role = User.UserRoles.USER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TournamentId = null,
            }
        };

        modelBuilder.Entity<User>().HasData(users);
    }
}
