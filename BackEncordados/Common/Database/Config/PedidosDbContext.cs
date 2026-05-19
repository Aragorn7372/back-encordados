using BackEncordados.Common.Database.Helpers;
using BackEncordados.Purchased.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.Database.Config;

public class PedidosDbContext(DbContextOptions<PedidosDbContext> options): DbContext(options) {
    private const string Time = "CURRENT_TIMESTAMP";

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Ulid>()
            .HaveConversion<UlidToStringConverterNonNullable>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurePedidos(modelBuilder);
        ConfigurePedidoLinea(modelBuilder);
        SeedData(modelBuilder);
    }

    private static void ConfigurePedidos(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Pedidos>();

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasValueGenerator<UlidValueGenerator>();
        
        builder.Property(p => p.TournamentId)
            .IsRequired();
        
        builder.Property(p => p.PlayerId)
            .IsRequired();

        builder.Property(p => p.AssignedTo)
            .IsRequired();

        builder.Property(p => p.Machine)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Comments)
            .HasMaxLength(1000);

        builder.Property(p => p.PayStatus)
            .IsRequired();

        builder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("decimal(10, 2)");

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql(Time);

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql(Time);

        builder.HasMany(p => p.Lineas)
            .WithOne(l => l.Pedido)
            .HasForeignKey(l => l.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.PlayerId);
        builder.HasIndex(p => p.AssignedTo);
        builder.HasIndex(p => p.PayStatus);
    }

    private static void ConfigurePedidoLinea(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<PedidoLinea>();

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasValueGenerator<UlidValueGenerator>();

        builder.Property(l => l.PedidoId)
            .IsRequired();

        builder.Property(l => l.RaquetModel)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.Nudos)
            .IsRequired();

        builder.Property(l => l.DateString)
            .IsRequired();

        builder.Property(l => l.Logotype)
            .IsRequired();

        builder.Property(l => l.Color)
            .HasMaxLength(100);

        builder.Property(l => l.Status)
            .IsRequired();

        builder.Property(l => l.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql(Time);

        builder.Property(l => l.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql(Time);

        builder.OwnsOne(l => l.StringSetup, sb =>
        {
            sb.Property(s => s.StringV).HasColumnName("StringSetup_StringV");
            sb.Property(s => s.TensionV).HasColumnName("StringSetup_TensionV");
            sb.Property(s => s.PreStetchV).HasColumnName("StringSetup_PreStetchV");
            sb.Property(s => s.StringH).HasColumnName("StringSetup_StringH");
            sb.Property(s => s.TensionH).HasColumnName("StringSetup_TensionH");
            sb.Property(s => s.PreStetchH).HasColumnName("StringSetup_PreStetchH");
        });

        builder.HasIndex(l => l.PedidoId);
        builder.HasIndex(l => l.Status);
    }

    public DbSet<Pedidos> Pedidos { get; set; } = null!;
    public DbSet<PedidoLinea> PedidoLineas { get; set; } = null!;

    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;

        var juan = Ulid.Parse("01KS0Q28TD6SAPN0GN0XKRPK5D");
        var ana = Ulid.Parse("01KS0Q28TE3RJTW6W35NJRMTZ4");
        var pedro = Ulid.Parse("01KS0Q28TED4PWJPT7DMJ46WBN");
        var carlos = Ulid.Parse("01KS0Q28TE7CMWS2D8RVDFA7YJ");
        var maria = Ulid.Parse("01KS0Q28TE6CVB0NYYANTWEK7B");
        var admin= Ulid.Parse("01KS0Q28TESE956013XYJKP6ST");


        var pedido1Id = Ulid.Parse("01KR42ME5P1Q2BGWW1DY11Z4TJ");
        var pedido2Id = Ulid.Parse("01KR424NQJVJRJR4GKWNTP2HK2");
        var pedido3Id = Ulid.Parse("01KR424NQJ6VBW36R0TS0HYGAR");
        var pedido4Id = Ulid.Parse("01KR424NQJWHMD2MN2YFQA7K6E");
        var pedido5Id = Ulid.Parse("01KR424NQJDA22KTM967R9RW9N");
        var pedido6Id = Ulid.Parse("01KR42766T458MYAE94CSAYWY7");

        var linea1Id = Ulid.Parse("01KR42ME5P1Q2BGWW1DY11Z5AA");
        var linea2Id = Ulid.Parse("01KR424NQJVJRJR4GKWNTP2HA2");
        var linea3Id = Ulid.Parse("01KR424NQJ6VBW36R0TS0HYGA3");
        var linea4Id = Ulid.Parse("01KR424NQJWHMD2MN2YFQA7K7E");
        var linea5Id = Ulid.Parse("01KR424NQJDA22KTM967R9RWAN");
        var linea6Id = Ulid.Parse("01KR42766T458MYAE94CSAYWB8");

        var t1 = Ulid.Parse("01KS0Q28TEJ0SYA6JJ5H4W4CMP");
        var t2 = Ulid.Parse("01KS0Q28TE9N7TG55K98TCB4X0");
        var t3= Ulid.Parse("01KS0Q28TEVEYS4303TXP202N4");



        var pedidoExcel1Id = Ulid.Parse("01KS0Q28TENZNBNAK70DSCD8KM");
        var pedidoExcel2Id = Ulid.Parse("01KS0Q28TEW6AVWP9S8WVJ9Z5G");
        var lineaExcel1Id = Ulid.Parse("01KS0Q28TEJK7VANZ51KZPVX70");
        var lineaExcel2Id = Ulid.Parse("01KS0Q1Y13HYP6FEB8GQ2AM563");

        modelBuilder.Entity<Pedidos>().HasData(
            new Pedidos
            {
                Id = pedido1Id,
                TournamentId = t1,
                PlayerId = juan,
                AssignedTo = carlos,
                Machine = "Máquina Alpha-1",
                Comments = "Cliente preferente, ser cuidadoso con el logotipo",
                Price = 45.50,
                PayStatus = PaymentStatus.PENDING_PAYMENT,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5)
            },
            new Pedidos
            {
                Id = pedido2Id,
                TournamentId = t1,
                PlayerId = ana,
                AssignedTo = maria,
                Machine = "Máquina Beta-2",
                Comments = "Tensión estándar, sin especificaciones especiales",
                Price = 38.00,
                PayStatus = PaymentStatus.PAID,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-1)
            },
            new Pedidos
            {
                Id = pedido3Id,
                TournamentId = t2,
                PlayerId = pedro,
                AssignedTo = carlos,
                Machine = "Máquina Alpha-1",
                Comments = "Revisar empuñadura, cambiar grip si es necesario",
                Price = 52.75,
                PayStatus = PaymentStatus.PAID,
                CreatedAt = now.AddDays(-15),
                UpdatedAt = now.AddDays(-2)
            },
            new Pedidos
            {
                Id = pedido4Id,
                TournamentId = t1,
                PlayerId = juan,
                AssignedTo = maria,
                Machine = "Máquina Gamma-3",
                Comments = "Segunda raqueta del jugador",
                Price = 40.00,
                PayStatus = PaymentStatus.PAID,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-4)
            },
            new Pedidos
            {
                Id = pedido5Id,
                TournamentId = t2,
                PlayerId = ana,
                AssignedTo = carlos,
                Machine = "Máquina Alpha-1",
                Comments = "CANCELADO: Cliente cambió de planes",
                Price = 35.50,
                PayStatus = PaymentStatus.CANCELED,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-3)
            },
            new Pedidos
            {
                Id = pedido6Id,
                TournamentId = t1,
                PlayerId = juan,
                AssignedTo = maria,
                Machine = "Banco de trabajo",
                Comments = "Microfisura en el marco. Cliente prioritario",
                Price = 60.00,
                PayStatus = PaymentStatus.PENDING_PAYMENT,
                CreatedAt = now.AddDays(-8),
                UpdatedAt = now.AddDays(-1)
            },
            new Pedidos
            {
                Id = pedidoExcel1Id,
                TournamentId = t3,
                PlayerId = pedro,
                AssignedTo = carlos,
                Machine = "Máquina Alpha-1",
                Comments = "Pedido de prueba para Excel",
                Price = 42.50,
                PayStatus = PaymentStatus.PAID,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-1)
            },
            new Pedidos
            {
                Id = pedidoExcel2Id,
                TournamentId = t3,
                PlayerId = ana,
                AssignedTo = maria,
                Machine = "Máquina Beta-2",
                Comments = "Segundo pedido de prueba para Excel",
                Price = 35.00,
                PayStatus = PaymentStatus.PENDING_PAYMENT,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2)
            }
        );

        modelBuilder.Entity<PedidoLinea>().HasData(
            new { Id = linea1Id, PedidoId = pedido1Id, RaquetModel = "Wilson Pro H", Nudos = (byte)1, DateString = now.AddDays(3), Logotype = true, Color = "Negro", Status = Status.PENDING, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-5) },
            new { Id = linea2Id, PedidoId = pedido2Id, RaquetModel = "Head Graphene 360", Nudos = (byte)2, DateString = now.AddDays(1), Logotype = false, Color = "Rojo", Status = Status.IN_PROGRESS, CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-1) },
            new { Id = linea3Id, PedidoId = pedido3Id, RaquetModel = "Tecnifibre TF-X", Nudos = (byte)0, DateString = now.AddDays(10), Logotype = false, Color = "Azul", Status = Status.COMPLETED, CreatedAt = now.AddDays(-15), UpdatedAt = now.AddDays(-2) },
            new { Id = linea4Id, PedidoId = pedido4Id, RaquetModel = "Yonex Vcore Pro", Nudos = (byte)1, DateString = now.AddDays(2), Logotype = true, Color = "Blanco", Status = Status.DELIVERED_TOpLAYER, CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-4) },
            new { Id = linea5Id, PedidoId = pedido5Id, RaquetModel = "Dunlop Biomimetic", Nudos = (byte)1, DateString = now.AddDays(20), Logotype = false, Color = "Amarillo", Status = Status.CANCELED, CreatedAt = now.AddDays(-7), UpdatedAt = now.AddDays(-3) },
            new { Id = linea6Id, PedidoId = pedido6Id, RaquetModel = "Wilson Pro Staff", Nudos = (byte)0, DateString = now.AddDays(5), Logotype = true, Color = "Negro", Status = Status.IN_PROGRESS, CreatedAt = now.AddDays(-8), UpdatedAt = now.AddDays(-1) },
            new { Id = lineaExcel1Id, PedidoId = pedidoExcel1Id, RaquetModel = "Babolat Pure Drive", Nudos = (byte)1, DateString = now.AddDays(1), Logotype = true, Color = "Negro", Status = Status.COMPLETED, CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-1) },
            new { Id = lineaExcel2Id, PedidoId = pedidoExcel2Id, RaquetModel = "Head Speed Pro", Nudos = (byte)2, DateString = now.AddDays(2), Logotype = false, Color = "Azul", Status = Status.IN_PROGRESS, CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-2) }
        );

        modelBuilder.Entity<PedidoLinea>().OwnsOne(l => l.StringSetup).HasData(
            new { PedidoLineaId = linea1Id, StringV = "Babolat RPM Blast", TensionV = 26.5, PreStetchV = (short)2, StringH = "Babolat Pro", TensionH = 25.0, PreStetchH = (short)1 },
            new { PedidoLineaId = linea2Id, StringV = "Luxilon Big Banger", TensionV = 25.0, PreStetchV = (short)2, StringH = "Luxilon Big Banger", TensionH = 24.5, PreStetchH = (short)1 },
            new { PedidoLineaId = linea3Id, StringV = "Mantenimiento", TensionV = 0.0, PreStetchV = (short)0, StringH = "Mantenimiento", TensionH = 0.0, PreStetchH = (short)0 },
            new { PedidoLineaId = linea4Id, StringV = "Prince Synthetic Gut", TensionV = 24.0, PreStetchV = (short)1, StringH = "Prince Synthetic Gut", TensionH = 23.5, PreStetchH = (short)1 },
            new { PedidoLineaId = linea5Id, StringV = "Wilson NPS Tour", TensionV = 23.5, PreStetchV = (short)1, StringH = "Wilson NPS Tour", TensionH = 23.0, PreStetchH = (short)1 },
            new { PedidoLineaId = linea6Id, StringV = "Reparación", TensionV = 0.0, PreStetchV = (short)0, StringH = "Reparación", TensionH = 0.0, PreStetchH = (short)0 },
            new { PedidoLineaId = lineaExcel1Id, StringV = "Babolat VS Team", TensionV = 24.0, PreStetchV = (short)1, StringH = "Babolat VS Team", TensionH = 23.5, PreStetchH = (short)1 },
            new { PedidoLineaId = lineaExcel2Id, StringV = "Technifibre XR2", TensionV = 25.5, PreStetchV = (short)2, StringH = "Technifibre XR2", TensionH = 24.5, PreStetchH = (short)2 }
        );
    }
}