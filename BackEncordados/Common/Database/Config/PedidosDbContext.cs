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

        // Seed data
        SeedData(modelBuilder);
    }

    public DbSet<Pedidos> Pedidos { get; set; } = null!;

    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;

        var juan = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var ana = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var pedro = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var carlos = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var maria = Guid.Parse("44444444-4444-4444-4444-444444444444");

        // IDs fijos para los pedidos
        var pedido1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var pedido2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var pedido3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var pedido4Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var pedido5Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var pedido6Id = Guid.Parse("66666666-6666-6666-6666-666666666666");

        // Seed de Pedidos SIN StringSetup
        modelBuilder.Entity<Pedidos>().HasData(
            new Pedidos
            {
                Id = pedido1Id,
                TournamentId = 1,
                TypeString = "Babolat Pro Staff",
                TypeWork = TypePuchase.ENCORDADO,
                DateString = now.AddDays(3),
                Logotype = true,
                RaquetModel = "Wilson Pro H",
                Price = 25.50,
                Nudos = 1,
                PlayerId = juan,
                AssignedTo = carlos,
                Machine = "Máquina Alpha-1",
                Comments = "Cliente preferente, ser cuidadoso con el logotipo",
                PayStatus = PaymentStatus.PENDING_PAYMENT,
                Status = Status.PENDING,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5)
            },
            new Pedidos
            {
                Id = pedido2Id,
                TournamentId = 1,
                TypeString = "Yonex Poly Tour Pro",
                TypeWork = TypePuchase.ENCORDADO,
                DateString = now.AddDays(1),
                Logotype = false,
                RaquetModel = "Head Graphene 360",
                Price = 22.00,
                Nudos = 2,
                PlayerId = ana,
                AssignedTo = maria,
                Machine = "Máquina Beta-2",
                Comments = "Tensión estándar, sin especificaciones especiales",
                PayStatus = PaymentStatus.PAID,
                Status = Status.IN_PROGRESS,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-1)
            },
            new Pedidos
            {
                Id = pedido3Id,
                TournamentId = 2,
                TypeString = "Mantenimiento general",
                TypeWork = TypePuchase.ORDEN_DE_TALLER,
                DateString = now.AddDays(10),
                Logotype = false,
                RaquetModel = "Tecnifibre TF-X",
                Price = 45.00,
                Nudos = 0,
                PlayerId = pedro,
                AssignedTo = carlos,
                Machine = "Máquina Alpha-1",
                Comments = "Revisar empuñadura, cambiar grip si es necesario",
                PayStatus = PaymentStatus.PAID,
                Status = Status.COMPLETED,
                CreatedAt = now.AddDays(-15),
                UpdatedAt = now.AddDays(-2)
            },
            new Pedidos
            {
                Id = pedido4Id,
                TournamentId = 1,
                TypeString = "Prince Synthetic Gut",
                TypeWork = TypePuchase.ENCORDADO,
                DateString = now.AddDays(2),
                Logotype = true,
                RaquetModel = "Yonex Vcore Pro",
                Price = 20.00,
                Nudos = 1,
                PlayerId = juan,
                AssignedTo = maria,
                Machine = "Máquina Gamma-3",
                Comments = "Segunda raqueta del jugador",
                PayStatus = PaymentStatus.PAID,
                Status = Status.DELIVERED_TOpLAYER,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-4)
            },
            new Pedidos
            {
                Id = pedido5Id,
                TournamentId = 2,
                TypeString = "Wilson NPS Tour",
                TypeWork = TypePuchase.ENCORDADO,
                DateString = now.AddDays(20),
                Logotype = false,
                RaquetModel = "Dunlop Biomimetic",
                Price = 19.50,
                Nudos = 1,
                PlayerId = ana,
                AssignedTo = carlos,
                Machine = "Máquina Alpha-1",
                Comments = "CANCELADO: Cliente cambió de planes",
                PayStatus = PaymentStatus.CANCELED,
                Status = Status.CANCELED,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-3)
            },
            new Pedidos
            {
                Id = pedido6Id,
                TournamentId = 1,
                TypeString = "Reparación de marco",
                TypeWork = TypePuchase.ORDEN_DE_TALLER,
                DateString = now.AddDays(5),
                Logotype = true,
                RaquetModel = "Wilson Pro Staff",
                Price = 65.00,
                Nudos = 0,
                PlayerId = juan,
                AssignedTo = maria,
                Machine = "Banco de trabajo",
                Comments = "Microfisura en el marco. Cliente prioritario",
                PayStatus = PaymentStatus.PENDING_PAYMENT,
                Status = Status.IN_PROGRESS,
                CreatedAt = now.AddDays(-8),
                UpdatedAt = now.AddDays(-1)
            }
        );

        modelBuilder.Entity<Pedidos>()
            .OwnsOne(p => p.StringSetup)
            .HasData(
                // Pedido 1
                new { PedidosId = pedido1Id, StringV = "Babolat RPM Blast", TensionV = 26.5, PreStetchV = (short)2, StringH = "Babolat Pro", TensionH = 25.0, PreStetchH = (short)1 },
                // Pedido 2
                new { PedidosId = pedido2Id, StringV = "Luxilon Big Banger", TensionV = 25.0, PreStetchV = (short)2, StringH = "Luxilon Big Banger", TensionH = 24.5, PreStetchH = (short)1 },
                // Pedido 4
                new { PedidosId = pedido4Id, StringV = "Prince Synthetic Gut", TensionV = 24.0, PreStetchV = (short)1, StringH = "Prince Synthetic Gut", TensionH = 23.5, PreStetchH = (short)1 },
                // Pedido 5
                new { PedidosId = pedido5Id, StringV = "Wilson NPS Tour", TensionV = 23.5, PreStetchV = (short)1, StringH = "Wilson NPS Tour", TensionH = 23.0, PreStetchH = (short)1 }
            );
    }
}