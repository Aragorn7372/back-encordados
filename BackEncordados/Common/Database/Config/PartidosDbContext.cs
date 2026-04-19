using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.Database.Config;

public class PartidosDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
    }

    public DbSet<Tournaments> Partidos { get; set; } = null!;

    private void SeedData(ModelBuilder modelBuilder)
    {
        
    }
}