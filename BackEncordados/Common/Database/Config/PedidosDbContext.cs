using BackEncordados.Purchased.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.Database.Config;

public class PedidosDbContext(DbContextOptions<PedidosDbContext> options): DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurar entidad Pedidos
        var pedidosBuilder = modelBuilder.Entity<Pedidos>();

        pedidosBuilder.HasKey(p => p.Id);

        pedidosBuilder.Property(p => p.Id)
            .ValueGeneratedOnAdd();
        
        pedidosBuilder.Property(p => p.TournamentId)
            .IsRequired();
        
        pedidosBuilder.Property(p => p.TypeString)
            .IsRequired()
            .HasMaxLength(100);

        pedidosBuilder.Property(p => p.TypeWork)
            .IsRequired();

        pedidosBuilder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        pedidosBuilder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        pedidosBuilder.Property(p => p.DateString)
            .IsRequired();

        pedidosBuilder.Property(p => p.Logotype)
            .IsRequired();

        pedidosBuilder.Property(p => p.RaquetModel)
            .IsRequired()
            .HasMaxLength(200);

        pedidosBuilder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("double precision");

        pedidosBuilder.Property(p => p.Nudos)
            .IsRequired();

        pedidosBuilder.Property(p => p.PlayerId)
            .IsRequired();

        pedidosBuilder.Property(p => p.AssignedTo)
            .IsRequired();

        pedidosBuilder.Property(p => p.Machine)
            .IsRequired()
            .HasMaxLength(100);

        pedidosBuilder.Property(p => p.Comments)
            .HasMaxLength(1000);

        pedidosBuilder.Property(p => p.PayStatus)
            .IsRequired();

        pedidosBuilder.Property(p => p.Status)
            .IsRequired();

        // Configurar StringSetup como entidad embebida (Owned Type)
        pedidosBuilder.OwnsOne(p => p.StringSetup, sb =>
        {
            sb.Property(s => s.StringV).HasColumnName("StringSetup_StringV");
            sb.Property(s => s.TensionV).HasColumnName("StringSetup_TensionV");
            sb.Property(s => s.PreStetchV).HasColumnName("StringSetup_PreStetchV");
            sb.Property(s => s.StringH).HasColumnName("StringSetup_StringH");
            sb.Property(s => s.TensionH).HasColumnName("StringSetup_TensionH");
            sb.Property(s => s.PreStetchH).HasColumnName("StringSetup_PreStetchH");
        });

        // Índices útiles
        pedidosBuilder.HasIndex(p => p.PlayerId);
        pedidosBuilder.HasIndex(p => p.AssignedTo);
        pedidosBuilder.HasIndex(p => p.Status);
        pedidosBuilder.HasIndex(p => p.PayStatus);
    }

    public DbSet<Pedidos> Pedidos { get; set; } = null!;

    private void SeedData(ModelBuilder modelBuilder)
    {
        
    }
}