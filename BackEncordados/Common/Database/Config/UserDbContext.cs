using BackEncordados.Common.Database.Helpers;
using BackEncordados.Usuarios.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.Database.Config;

public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        SeedData(modelBuilder);
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
            entity.Property(u => u.Phone).IsRequired().HasMaxLength(20);
            entity.Property(u => u.ImageUrl).IsRequired().HasMaxLength(100);
            entity.Property(u => u.IsDeleted).HasDefaultValue(false);
            entity.ConfigureTimestamps();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasQueryFilter(u => !u.IsDeleted);
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

        var users = new List<User>
        {
            // Usuario ADMIN
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username = "admin_encordados",
                Name = "Administrador",
                Email = "admin@encordados.com",
                Phone = "912345678",
                PasswordHash = adminPasswordHash,
                Role = User.UserRoles.ADMIN,
                IsDeleted = false,
                ImageUrl = "/images/users/default.jpg",
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow.AddMonths(-6)
            },

            // Usuario OWNER
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Username = "owner_principal",
                Name = "Propietario",
                Email = "owner@encordados.com",
                Phone = "923456789",
                PasswordHash = ownerPasswordHash,
                Role = User.UserRoles.OWNER,
                IsDeleted = false,
                ImageUrl = "/images/users/default.jpg",
                CreatedAt = DateTime.UtcNow.AddMonths(-5),
                UpdatedAt = DateTime.UtcNow.AddMonths(-5)
            },

            // Usuario ENCORDER 1
            new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Username = "carlos_encordador",
                Name = "Carlos García",
                Email = "carlos@encordados.com",
                Phone = "934567890",
                PasswordHash = encorderPasswordHash,
                Role = User.UserRoles.ENCORDER,
                IsDeleted = false,
                ImageUrl = "/images/users/default.jpg",
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                UpdatedAt = DateTime.UtcNow.AddMonths(-4)
            },

            // Usuario ENCORDER 2
            new User
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Username = "maria_encordadora",
                Name = "María López",
                Email = "maria@encordados.com",
                Phone = "945678901",
                PasswordHash = encorderPasswordHash,
                Role = User.UserRoles.ENCORDER,
                IsDeleted = false,
                ImageUrl = "/images/users/default.jpg",
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UpdatedAt = DateTime.UtcNow.AddMonths(-3)
            },

            // Usuario JUGADOR 1
            new User
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Username = "jugador_juan",
                Name = "Juan Martínez",
                Email = "juan@tenis.com",
                Phone = "956789012",
                PasswordHash = userPasswordHash,
                Role = User.UserRoles.USER,
                IsDeleted = false,
                ImageUrl = "/images/users/default.jpg",
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                UpdatedAt = DateTime.UtcNow.AddMonths(-2)
            },

            // Usuario JUGADOR 2
            new User
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Username = "jugador_ana",
                Name = "Ana Pérez",
                Email = "ana@tenis.com",
                Phone = "967890123",
                PasswordHash = userPasswordHash,
                Role = User.UserRoles.USER,
                IsDeleted = false,
                ImageUrl = "/images/users/default.jpg",
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                UpdatedAt = DateTime.UtcNow.AddMonths(-1)
            },

            // Usuario JUGADOR 3
            new User
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Username = "jugador_pedro",
                Name = "Pedro Rodríguez",
                Email = "pedro@tenis.com",
                Phone = "978901234",
                PasswordHash = userPasswordHash,
                Role = User.UserRoles.USER,
                IsDeleted = false,
                ImageUrl = "/images/users/default.jpg",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        modelBuilder.Entity<User>().HasData(users);
    }
}
