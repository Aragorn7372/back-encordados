using BackEncordados.Usuarios.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.Database.Config;

public class PedidosDbContext(DbContextOptions<PedidosDbContext> options): DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
    }

    public DbSet<Pedidos> Pedidos { get; set; } = null!;

    private void SeedData(ModelBuilder modelBuilder)
    {
        
    }
}