using BackEncordados.Common.Database.Helpers;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Talleres.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BackEncordados.Common.Database.Config;

/// <summary>
/// DbContext de EF Core para la gestión de torneos y talleres de encordado.
/// Administra la tabla <c>Tournaments</c> (torneos) y su colección owned <c>WorkerMachineAssignments</c>
/// (asignación de máquinas a trabajadores por torneo).
/// </summary>
/// <remarks>
/// <para><b>Responsabilidades:</b></para>
/// <list type="bullet">
///   <item><description>Configurar el mapeo de la entidad <see cref="Tournaments"/> a la tabla <c>Tournaments</c>.</description></item>
///   <item><description>Establecer la conversión de <c>List&lt;Ulid&gt;</c> a <c>string</c> (separada por punto y coma) para las listas de trabajadores y supervisores.</description></item>
///   <item><description>Configurar la colección owned <c>WorkerMachineAssignments</c> en su tabla propia <c>TournamentWorkerMachineAssignments</c>.</description></item>
///   <item><description>Aplicar soft-delete mediante <c>HasQueryFilter(u =&gt; !u.IsDeleted)</c> con valor por defecto <c>false</c>.</description></item>
///   <item><description>Poblar datos de semilla para 5 torneos con 7 asignaciones de máquina.</description></item>
/// </list>
/// <para><b>Entidad administrada:</b></para>
/// <list type="bullet">
///   <item><description><see cref="Tournaments"/> → Tabla <c>Tournaments</c>: torneos con título, fechas, owner, listas de workers/supervisores y asignación de máquinas.</description></item>
/// </list>
/// <para><b>Característica distintiva:</b></para>
/// <list type="bullet">
///   <item><description>Usa un <c>ValueConverter</c> personalizado (<c>ulidListConverter</c>) para serializar <c>List&lt;Ulid&gt;</c> como texto delimitado por punto y coma en columnas de tipo <c>text</c>.</description></item>
///   <item><description>La colección owned <c>WorkerMachineAssignments</c> se almacena en una tabla separada con FK hacia <c>TournamentId</c>.</description></item>
/// </list>
/// </remarks>
/// <param name="options">Opciones de configuración del DbContext. Inyectadas a través del contenedor de dependencias (DI) con <c>AddDbContext&lt;TalleresDbContext&gt;()</c>.</param>
public class TalleresDbContext(DbContextOptions<TalleresDbContext> options)
    : DbContext(options)
{
    /// <summary>Catálogo de torneos. Cada torneo incluye fechas, owner, listas de trabajadores y supervisores, y asignaciones de máquinas.</summary>
    public DbSet<Tournaments> Partidos { get; set; } = null!;

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
    /// Configura el modelo de datos de EF Core para la entidad <see cref="Tournaments"/>.
    /// Delega en <see cref="ConfigureTournaments"/> y <see cref="SeedData"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo de configuración:</b></para>
    /// <list type="number">
    ///   <item><description><c>ConfigureTournaments(modelBuilder)</c> — mapea <see cref="Tournaments"/> con su conversión de listas, owned collection e índices.</description></item>
    ///   <item><description><c>SeedData(modelBuilder)</c> — inserta datos de semilla.</description></item>
    ///   <item><description><c>base.OnModelCreating(modelBuilder)</c> — ejecuta configuraciones base de EF Core.</description></item>
    /// </list>
    /// <para>A diferencia de otros DbContexts del proyecto, <c>base.OnModelCreating</c> se invoca al final
    /// del método, después de las configuraciones personalizadas y el seed.</para>
    /// </remarks>
    /// <param name="modelBuilder">Instancia de <see cref="ModelBuilder"/> para configurar el mapeo ORM.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureTournaments(modelBuilder);

        SeedData(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Configura el mapeo de la entidad <see cref="Tournaments"/> (tabla <c>Tournaments</c>).
    /// Define clave primaria, propiedades, conversiones personalizadas para listas de Ulid,
    /// owned collection <c>WorkerMachineAssignments</c>, timestamps, soft-delete y filtro global.
    /// </summary>
    /// <remarks>
    /// <para><b>Propiedades configuradas:</b></para>
    /// <list type="bullet">
    ///   <item><description><c>Id</c> — clave primaria, autogenerada vía <see cref="UlidValueGenerator"/>.</description></item>
    ///   <item><description><c>Title</c> — requerido, máximo 200 caracteres. Nombre del torneo.</description></item>
    ///   <item><description><c>Logotype</c> — opcional, máximo 500 caracteres. URL del logotipo en Cloudinary.</description></item>
    ///   <item><description><c>LogotypePublicId</c> — opcional, máximo 300 caracteres. ID público del logotipo en Cloudinary.</description></item>
    ///   <item><description><c>StartTournament</c> — requerido. Fecha de inicio del torneo.</description></item>
    ///   <item><description><c>EndTournament</c> — requerido. Fecha de fin del torneo.</description></item>
    ///   <item><description><c>Owner</c> — requerido. Usuario propietario del torneo.</description></item>
    ///   <item><description><c>WorkersList</c> — lista de <see cref="Ulid"/> de trabajadores, serializada como texto delimitado por punto y coma (columna <c>text</c>).</description></item>
    ///   <item><description><c>SupervisorList</c> — lista de <see cref="Ulid"/> de supervisores, serializada como texto delimitado por punto y coma (columna <c>text</c>).</description></item>
    ///   <item><description><c>IsDeleted</c> — requerido, valor por defecto <c>false</c>. Soft-delete.</description></item>
    ///   <item><description><c>CreatedAt</c> / <c>UpdatedAt</c> — timestamps automáticos vía <c>ConfigureTimestamps()</c>.</description></item>
    /// </list>
    /// <para><b>ValueConverter personalizado (ulidListConverter):</b></para>
    /// <list type="bullet">
    ///   <item><description>Convierte <c>List&lt;Ulid&gt;</c> a <c>string</c> usando <c>string.Join(";", values)</c>.</description></item>
    ///   <item><description>Convierte <c>string</c> a <c>List&lt;Ulid&gt;</c> usando <c>Split(";")</c> y <c>Ulid.Parse</c>.</description></item>
    ///   <item><description>Utiliza <see cref="ValueComparer{T}"/> personalizado para comparación secuencial y generación de hash.</description></item>
    /// </list>
    /// <para><b>Owned collection <c>WorkerMachineAssignments</c> (tabla <c>TournamentWorkerMachineAssignments</c>):</b></para>
    /// <list type="bullet">
    ///   <item><description>FK <c>TournamentId</c> hacia <c>Tournaments</c>.</description></item>
    ///   <item><description>Clave primaria: <c>Id</c> (autogenerado como <c>long</c>).</description></item>
    ///   <item><description><c>WorkerId</c> — requerido. Trabajador asignado.</description></item>
    ///   <item><description><c>MachineName</c> — requerido, máximo 100 caracteres. Máquina asignada.</description></item>
    /// </list>
    /// <para><b>Filtros:</b></para>
    /// <list type="bullet">
    ///   <item><description>Soft-delete global: <c>HasQueryFilter(u =&gt; !u.IsDeleted)</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="modelBuilder">Constructor del modelo de EF Core.</param>
    private static void ConfigureTournaments(ModelBuilder modelBuilder)
    {
        var ulidListConverter = new ValueConverter<List<Ulid>, string>(
            v => string.Join(";", v),
            v => string.IsNullOrWhiteSpace(v)
                ? new List<Ulid>()
                : v.Split(";", StringSplitOptions.RemoveEmptyEntries)
                    .Select(Ulid.Parse)
                    .ToList()
        );

        modelBuilder.Entity<Tournaments>(entity =>
        {
            entity.ToTable("Tournaments");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasValueGenerator<UlidValueGenerator>();

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Logotype)
                .HasMaxLength(500);

            entity.Property(x => x.LogotypePublicId)
                .HasMaxLength(300);

            entity.ConfigureTimestamps();

            entity.Property(x => x.StartTournament)
                .IsRequired();

            entity.Property(x => x.EndTournament)
                .IsRequired();

            entity.Property(x => x.Owner)
                .IsRequired();

            entity.Property(x => x.WorkersList)
                .HasConversion(
                    ulidListConverter,
                    new ValueComparer<List<Ulid>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c == null ? 0 : c.Aggregate(0, (acc, v) => HashCode.Combine(acc, v)),
                        c => c == null ? new List<Ulid>() : c.ToList()))
                .HasColumnType("text");

            entity.Property(x => x.SupervisorList)
                .HasConversion(
                    ulidListConverter,
                    new ValueComparer<List<Ulid>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c == null ? 0 : c.Aggregate(0, (acc, v) => HashCode.Combine(acc, v)),
                        c => c == null ? new List<Ulid>() : c.ToList()))
                .HasColumnType("text");

            entity.Property(x => x.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
            entity.HasQueryFilter(u => !u.IsDeleted);

            // Owned collection embebida
            entity.OwnsMany(x => x.WorkerMachineAssignments, owned =>
            {
                owned.ToTable("TournamentWorkerMachineAssignments");

                owned.WithOwner()
                    .HasForeignKey("TournamentId");

                owned.Property<long>("Id");
                owned.HasKey("Id");

                owned.Property(x => x.WorkerId)
                    .IsRequired();

                owned.Property(x => x.MachineName)
                    .IsRequired()
                    .HasMaxLength(100);
            });
        });
    }

    /// <summary>
    /// Inserta datos de semilla en las tablas <c>Tournaments</c> y <c>TournamentWorkerMachineAssignments</c>
    /// para los 5 torneos de prueba y sus asignaciones de máquina.
    /// </summary>
    /// <remarks>
    /// <para><b>Torneos insertados:</b></para>
    /// <list type="table">
    ///   <item><term>t1 (01KS0Q28TEJ0SYA6JJ5H4W4CMP)</term><description>Torneo Madrileño 2025 — en curso. 2 workers, 1 supervisor. 2 máquinas asignadas.</description></item>
    ///   <item><term>t2 (01KS0Q28TE9N7TG55K98TCB4X0)</term><description>Open Circuit Barcelona — futuro. 1 worker, 1 supervisor. 1 máquina asignada.</description></item>
    ///   <item><term>t3 (01KS0Q28TEVEYS4303TXP202N4)</term><description>Campeonato Regional Valencia — pasado. 1 worker, 0 supervisores. 1 máquina asignada.</description></item>
    ///   <item><term>t4 (01KS0Q28TET0JHJV4T5YFDJWBW)</term><description>Cup Toledo - Elite — futuro. 2 workers, 2 supervisores. 3 máquinas asignadas.</description></item>
    ///   <item><term>t5 (01KS0Q28TE5BA449NS2EVCBTDQ)</term><description>Torneo Prueba Excel — en curso. 2 workers, 1 supervisor. Sin máquinas.</description></item>
    /// </list>
    /// <para><b>Usuarios referenciados:</b></para>
    /// <list type="table">
    ///   <item><term>Carlos (01KS0Q28TE7CMWS2D8RVDFA7YJ)</term><description>Encordador — asignado a t1, t2, t4, t5.</description></item>
    ///   <item><term>María (01KS0Q28TE6CVB0NYYANTWEK7B)</term><description>Encordadora — asignada a t1, t3, t4, t5.</description></item>
    ///   <item><term>Owner (01KS0Q28TE2EFVQTCW8EN0W0MF)</term><description>Propietario de todos los torneos.</description></item>
    ///   <item><term>Supervisor1 (01KS0Q28TEHA2KF5YM3J6QS5Z9)</term><description>Supervisor en t1, t4, t5.</description></item>
    ///   <item><term>Supervisor2 (01KS0Q28TEXTDY9TQNRAXKAJ81)</term><description>Supervisor en t2, t4.</description></item>
    /// </list>
    /// <para><b>Asignaciones de máquina (7 registros):</b></para>
    /// <list type="bullet">
    ///   <item><description>Máquina Alpha-1 → Carlos (t1, t2, t4).</description></item>
    ///   <item><description>Máquina Beta-2 → María (t1, t3, t4).</description></item>
    ///   <item><description>Máquina Gamma-3 → María (t4).</description></item>
    /// </list>
    /// <para>Todos los registros se crean con <c>IsDeleted = false</c> y timestamps relativos.</para>
    /// </remarks>
    /// <param name="modelBuilder">Instancia de <see cref="ModelBuilder"/> usada para configurar los datos iniciales mediante <c>HasData()</c>.</param>
    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;

        var carlos = Ulid.Parse("01KS0Q28TE7CMWS2D8RVDFA7YJ");
        var maria = Ulid.Parse("01KS0Q28TE6CVB0NYYANTWEK7B");
        var owner= Ulid.Parse("01KS0Q28TE2EFVQTCW8EN0W0MF");
        var supervisor1 = Ulid.Parse("01KS0Q28TEHA2KF5YM3J6QS5Z9");
        var supervisor2 = Ulid.Parse("01KS0Q28TEXTDY9TQNRAXKAJ81");
        var t1 = Ulid.Parse("01KS0Q28TEJ0SYA6JJ5H4W4CMP");
        var t2 = Ulid.Parse("01KS0Q28TE9N7TG55K98TCB4X0");
        var t3= Ulid.Parse("01KS0Q28TEVEYS4303TXP202N4");
        var t4 = Ulid.Parse("01KS0Q28TET0JHJV4T5YFDJWBW");
        var t5 = Ulid.Parse("01KS0Q28TE5BA449NS2EVCBTDQ");

        modelBuilder.Entity<Tournaments>().HasData(
            new Tournaments
            {
                Id = t1,
                Title = "Torneo Madrileño 2025",
                Logotype = CloudinaryConstants.DEFAULT_IMAGE_TALLERES,
                LogotypePublicId = null,
                StartTournament = now.AddDays(-7),
                EndTournament = now.AddDays(7),
                WorkersList = new List<Ulid> { carlos, maria },
                SupervisorList = new List<Ulid> { supervisor1 },
                Owner = owner,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-2),
                UpdatedAt = now.AddMonths(-2)
            },
            
            new Tournaments
            {
                Id = t2,
                Title = "Open Circuit Barcelona",
                Logotype = CloudinaryConstants.DEFAULT_IMAGE_TALLERES,
                LogotypePublicId = null,
                StartTournament = now.AddDays(14),
                EndTournament = now.AddDays(28),
                WorkersList = new List<Ulid> { carlos },
                SupervisorList = new List<Ulid> { supervisor2 },
                Owner = owner,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            },
            new Tournaments
            {
                Id = t3,
                Title = "Campeonato Regional Valencia",
                Logotype = CloudinaryConstants.DEFAULT_IMAGE_TALLERES,
                LogotypePublicId = null,
                StartTournament = now.AddDays(-60),
                EndTournament = now.AddDays(-45),
                WorkersList = new List<Ulid> { maria },
                Owner = owner,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-4),
                UpdatedAt = now.AddMonths(-3)
            },
            new Tournaments
            {
                Id = t4,
                Title = "Cup Toledo - Elite",
                Logotype = CloudinaryConstants.DEFAULT_IMAGE_TALLERES,
                LogotypePublicId = null,
                StartTournament = now.AddDays(35),
                EndTournament = now.AddDays(42),
                WorkersList = new List<Ulid> { carlos, maria },
                SupervisorList = new List<Ulid> { supervisor1, supervisor2 },
                Owner = owner,
                IsDeleted = false,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-30)
            },

            new Tournaments
            {
                Id = t5,
                Title = "Torneo Prueba Excel",
                Logotype = CloudinaryConstants.DEFAULT_IMAGE_TALLERES,
                LogotypePublicId = null,
                StartTournament = now.AddDays(-10),
                EndTournament = now.AddDays(20),
                WorkersList = new List<Ulid> { carlos, maria },
                SupervisorList = new List<Ulid> { supervisor1 },
                Owner = owner,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            }
        );

        modelBuilder.Entity<Tournaments>()
            .OwnsMany(t => t.WorkerMachineAssignments)
            .HasData(
                new { Id = 1L, TournamentId = t1, WorkerId = carlos, MachineName = "Máquina Alpha-1" },
                new { Id = 2L, TournamentId = t1, WorkerId = maria, MachineName = "Máquina Beta-2" },
                new { Id = 3L, TournamentId = t2, WorkerId = carlos, MachineName = "Máquina Alpha-1" },
                new { Id = 4L, TournamentId = t3, WorkerId = maria, MachineName = "Máquina Beta-2" },
                new { Id = 5L, TournamentId = t4, WorkerId = carlos, MachineName = "Máquina Alpha-1" },
                new { Id = 6L, TournamentId = t4, WorkerId = maria, MachineName = "Máquina Beta-2" },
                new { Id = 7L, TournamentId = t4, WorkerId = maria, MachineName = "Máquina Gamma-3" }
            );
    }
}