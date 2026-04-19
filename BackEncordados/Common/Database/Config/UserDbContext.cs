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
        
    }
}
