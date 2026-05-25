using BackEncordados.Common.Database.Helpers;
using BackEncordados.Purchased.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.Database.Config;

/// <summary>
/// DbContext de EF Core para la gestión de pedidos de encordados.
/// Administra las tablas <c>Pedidos</c> (solicitudes de clientes) y <c>PedidoLineas</c>
/// (detalles individuales de cada raqueta dentro de un pedido).
/// </summary>
/// <remarks>
/// <para><b>Responsabilidades:</b></para>
/// <list type="bullet">
///   <item><description>Configurar el mapeo de las entidades <see cref="Pedidos"/> y <see cref="PedidoLinea"/> a sus tablas correspondientes.</description></item>
///   <item><description>Establecer la relación 1:N entre <c>Pedidos</c> y sus líneas (<c>Lineas</c> → <c>PedidoLineas</c>) con borrado en cascada.</description></item>
///   <item><description>Configurar la entidad owned <see cref="StringSetup"/> como parte de <c>PedidoLinea</c> (tensión y tipo de cuerda en vertical/horizontal).</description></item>
///   <item><description>Establecer índices para búsquedas por <c>PlayerId</c>, <c>AssignedTo</c>, <c>PayStatus</c>, <c>PedidoId</c> y <c>Status</c>.</description></item>
///   <item><description>Poblar datos de semilla para 8 pedidos con sus líneas y configuraciones de cuerda en 3 torneos de prueba.</description></item>
/// </list>
/// <para><b>Entidades administradas:</b></para>
/// <list type="bullet">
///   <item><description><see cref="Pedidos"/> → Tabla <c>Pedidos</c>: solicitudes de encordado con máquina asignada, precio y estado de pago.</description></item>
///   <item><description><see cref="PedidoLinea"/> → Tabla <c>PedidoLineas</c>: cada raqueta a encordar, con modelo, nudos, colores, logotipo y <see cref="StringSetup"/>.</description></item>
/// </list>
/// <para><b>Comportamiento especial:</b></para>
/// <list type="bullet">
///   <item><description>El DbSet <c>PedidoLineas</c> usa entidades anónimas para <c>HasData()</c> (sin constructor público de seed).</description></item>
///   <item><description>La propiedad estática <see cref="DisableTransactions"/> permite deshabilitar warnings de transacciones en modo test.</description></item>
/// </list>
/// </remarks>
/// <param name="options">Opciones de configuración del DbContext. Inyectadas a través del contenedor de dependencias (DI) con <c>AddDbContext&lt;PedidosDbContext&gt;()</c>.</param>
public class PedidosDbContext(DbContextOptions<PedidosDbContext> options): DbContext(options) {
    private const string Time = "CURRENT_TIMESTAMP";
    
    /// <summary>Cuando es <c>true</c>, desactiva los warnings de transacciones (<c>CoreEventId.SaveChangesStarting</c>) en <see cref="OnConfiguring"/>. Útil para entornos de test que no usan réplicas adecuadas.</summary>
    public static bool DisableTransactions { get; set; } = false;

    /// <summary>
    /// Configura las convenciones globales de propiedades para todo el modelo.
    /// Establece que todas las propiedades de tipo <see cref="Ulid"/> se conviertan automáticamente
    /// a <c>string</c> para su almacenamiento en la base de datos.
    /// </summary>
    /// <remarks>
    /// <para>Esta convención evita tener que decorar cada propiedad <see cref="Ulid"/> individualmente
    /// con <c>HasConversion&lt;UlidToStringConverterNonNullable&gt;()</c> en el mapeo de cada entidad.</para>
    /// </remarks>
    /// <param name="configurationBuilder">Constructor de configuración de convenciones del modelo.</param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Ulid>()
            .HaveConversion<UlidToStringConverterNonNullable>();
    }

    /// <summary>
    /// Configura opciones adicionales del DbContext en tiempo de construcción.
    /// Cuando <see cref="DisableTransactions"/> está activo, ignora los warnings de
    /// <c>SaveChangesStarting</c> para entornos de test sin réplicas de base de datos.
    /// </summary>
    /// <remarks>
    /// Esta configuración es necesaria porque en modo test (InMemory o SQLite)
    /// no se pueden usar transacciones distribuidas con réplica sets,
    /// lo que genera warnings de EF Core que pueden interferir con la ejecución de tests.
    /// </remarks>
    /// <param name="optionsBuilder">Constructor de opciones del DbContext.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // Only applied when DisableTransactions is true (test mode)
        if (DisableTransactions)
        {
            optionsBuilder.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.SaveChangesStarting)
            );
        }
    }

    /// <summary>
    /// Configura el modelo de datos de EF Core para las entidades <see cref="Pedidos"/> y <see cref="PedidoLinea"/>.
    /// Delega en <see cref="ConfigurePedidos"/>, <see cref="ConfigurePedidoLinea"/> y <see cref="SeedData"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo de configuración:</b></para>
    /// <list type="number">
    ///   <item><description><c>ConfigurePedidos(modelBuilder)</c> — mapea la entidad <see cref="Pedidos"/> con sus propiedades, relaciones e índices.</description></item>
    ///   <item><description><c>ConfigurePedidoLinea(modelBuilder)</c> — mapea <see cref="PedidoLinea"/> con su owned entity <see cref="StringSetup"/> e índices.</description></item>
    ///   <item><description><c>SeedData(modelBuilder)</c> — inserta datos de semilla.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="modelBuilder">Instancia de <see cref="ModelBuilder"/> para configurar el mapeo ORM.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurePedidos(modelBuilder);
        ConfigurePedidoLinea(modelBuilder);
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Configura el mapeo de la entidad <see cref="Pedidos"/> (tabla <c>Pedidos</c>).
    /// Define clave primaria, propiedades requeridas, relaciones, timestamps e índices.
    /// </summary>
    /// <remarks>
    /// <para><b>Propiedades configuradas:</b></para>
    /// <list type="bullet">
    ///   <item><description><c>Id</c> — clave primaria, autogenerada vía <see cref="UlidValueGenerator"/>.</description></item>
    ///   <item><description><c>TournamentId</c> — requerido, asocia el pedido a un torneo.</description></item>
    ///   <item><description><c>PlayerId</c> — requerido, jugador que solicita el servicio.</description></item>
    ///   <item><description><c>AssignedTo</c> — requerido, encordador asignado.</description></item>
    ///   <item><description><c>Machine</c> — requerido, máximo 100 caracteres. Máquina usada.</description></item>
    ///   <item><description><c>Comments</c> — opcional, máximo 1000 caracteres. Notas del pedido.</description></item>
    ///   <item><description><c>PayStatus</c> — requerido, enum <see cref="PaymentStatus"/> (PENDING_PAYMENT, PAID, CANCELED, etc.).</description></item>
    ///   <item><description><c>Price</c> — requerido, tipo <c>decimal(10, 2)</c>. Precio total.</description></item>
    ///   <item><description><c>CreatedAt</c> / <c>UpdatedAt</c> — timestamps con valor por defecto <c>CURRENT_TIMESTAMP</c>.</description></item>
    /// </list>
    /// <para><b>Relaciones:</b></para>
    /// <list type="bullet">
    ///   <item><description>1:N con <see cref="PedidoLinea"/> mediante <c>Lineas</c> → <c>PedidoId</c>, borrado en cascada.</description></item>
    /// </list>
    /// <para><b>Índices:</b></para>
    /// <list type="bullet">
    ///   <item><description><c>PlayerId</c> — búsqueda por jugador.</description></item>
    ///   <item><description><c>AssignedTo</c> — búsqueda por encordador.</description></item>
    ///   <item><description><c>PayStatus</c> — filtro por estado de pago.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="modelBuilder">Constructor del modelo de EF Core.</param>
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

    /// <summary>
    /// Configura el mapeo de la entidad <see cref="PedidoLinea"/> (tabla <c>PedidoLineas</c>).
    /// Define clave primaria, propiedades requeridas, owned entity <see cref="StringSetup"/>, timestamps e índices.
    /// </summary>
    /// <remarks>
    /// <para><b>Propiedades configuradas:</b></para>
    /// <list type="bullet">
    ///   <item><description><c>Id</c> — clave primaria, autogenerada vía <see cref="UlidValueGenerator"/>.</description></item>
    ///   <item><description><c>PedidoId</c> — requerido, FK hacia <see cref="Pedidos"/>.</description></item>
    ///   <item><description><c>RaquetModel</c> — requerido, máximo 200 caracteres. Modelo de raqueta.</description></item>
    ///   <item><description><c>Nudos</c> — requerido (<c>byte</c>). Tipo de nudo (0, 1, 2).</description></item>
    ///   <item><description><c>DateString</c> — requerido. Fecha del servicio.</description></item>
    ///   <item><description><c>Logotype</c> — requerido (<c>bool</c>). Indica si incluye logotipo.</description></item>
    ///   <item><description><c>Color</c> — opcional, máximo 100 caracteres. Color del logotipo/hilo.</description></item>
    ///   <item><description><c>Status</c> — requerido, enum <see cref="Status"/> (PENDING, IN_PROGRESS, COMPLETED, etc.).</description></item>
    ///   <item><description><c>CreatedAt</c> / <c>UpdatedAt</c> — timestamps con valor por defecto <c>CURRENT_TIMESTAMP</c>.</description></item>
    /// </list>
    /// <para><b>Owned entity <see cref="StringSetup"/> (columnas prefijadas <c>StringSetup_</c>):</b></para>
    /// <list type="bullet">
    ///   <item><description><c>StringSetup_StringV</c> — cuerda vertical.</description></item>
    ///   <item><description><c>StringSetup_TensionV</c> — tensión vertical.</description></item>
    ///   <item><description><c>StringSetup_PreStetchV</c> — preestirado vertical.</description></item>
    ///   <item><description><c>StringSetup_StringH</c> — cuerda horizontal.</description></item>
    ///   <item><description><c>StringSetup_TensionH</c> — tensión horizontal.</description></item>
    ///   <item><description><c>StringSetup_PreStetchH</c> — preestirado horizontal.</description></item>
    /// </list>
    /// <para><b>Índices:</b></para>
    /// <list type="bullet">
    ///   <item><description><c>PedidoId</c> — búsqueda por pedido padre.</description></item>
    ///   <item><description><c>Status</c> — filtro por estado de la línea.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="modelBuilder">Constructor del modelo de EF Core.</param>
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

    /// <summary>Colección de pedidos de encordado. Cada pedido agrupa una o más líneas con máquina, precio y estado de pago.</summary>
    public DbSet<Pedidos> Pedidos { get; set; } = null!;
    /// <summary>Colección de líneas de pedido. Cada línea representa una raqueta a encordar con su configuración de cuerdas, tensión y colores.</summary>
    public DbSet<PedidoLinea> PedidoLineas { get; set; } = null!;

    /// <summary>
    /// Inserta datos de semilla en las tablas <c>Pedidos</c>, <c>PedidoLineas</c>
    /// y su owned entity <c>StringSetup</c> para los torneos de prueba.
    /// </summary>
    /// <remarks>
    /// <para><b>Usuarios referenciados en los datos:</b></para>
    /// <list type="table">
    ///   <item><term>Juan (01KS0Q28TD6SAPN0GN0XKRPK5D)</term><description>Jugador, 3 pedidos (1 cancelado).</description></item>
    ///   <item><term>Ana (01KS0Q28TE3RJTW6W35NJRMTZ4)</term><description>Jugadora, 2 pedidos + 2 Excel.</description></item>
    ///   <item><term>Pedro (01KS0Q28TED4PWJPT7DMJ46WBN)</term><description>Jugador, 1 pedido + 1 Excel.</description></item>
    ///   <item><term>Carlos (01KS0Q28TE7CMWS2D8RVDFA7YJ)</term><description>Encordador asignado a 5 pedidos.</description></item>
    ///   <item><term>María (01KS0Q28TE6CVB0NYYANTWEK7B)</term><description>Encordadora asignada a 3 pedidos.</description></item>
    /// </list>
    /// <para><b>Torneos:</b></para>
    /// <list type="table">
    ///   <item><term>t1 (01KS0Q28TEJ0SYA6JJ5H4W4CMP)</term><description>Torneo Madrileño 2025 — 4 pedidos.</description></item>
    ///   <item><term>t2 (01KS0Q28TE9N7TG55K98TCB4X0)</term><description>Open Circuit Barcelona — 2 pedidos.</description></item>
    ///   <item><term>t3 (01KS0Q28TEVEYS4303TXP202N4)</term><description>Campeonato Regional Valencia — 2 pedidos Excel.</description></item>
    /// </list>
    /// <para><b>Datos insertados:</b></para>
    /// <list type="bullet">
    ///   <item><description><b>Pedidos (8 registros):</b> Combinaciones de jugadores, encordadores, máquinas (Alpha-1, Beta-2, Gamma-3, Banco de trabajo) con estados PENDING_PAYMENT, PAID y CANCELED.</description></item>
    ///   <item><description><b>PedidoLineas (8 registros):</b> Modelos de raqueta (Wilson Pro H, Head Graphene 360, Tecnifibre TF-X, etc.) con estados PENDING a DELIVERED_TOpLAYER.</description></item>
    ///   <item><description><b>StringSetup (8 registros):</b> Configuraciones de cuerda vertical/horizontal con tensiones de 23.0 a 26.5 kg y preestirado de 0 a 2.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="modelBuilder">Instancia de <see cref="ModelBuilder"/> usada para configurar los datos iniciales mediante <c>HasData()</c>.</param>
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