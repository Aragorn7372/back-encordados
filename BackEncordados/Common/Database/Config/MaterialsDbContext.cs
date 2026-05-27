using BackEncordados.Common.Database.Helpers;
using BackEncordados.Materials.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.Database.Config;

/// <summary>
/// DbContext de EF Core para la gestión del catálogo de materiales e insumos de encordados.
/// Administra las tablas <c>Cuerdas</c> (cuerdas/tensores para raquetas) y <c>Materiales</c>
/// (grips, overgrips, lead tape, siliconas y otros accesorios).
/// </summary>
/// <remarks>
/// <para><b>Responsabilidades:</b></para>
/// <list type="bullet">
///   <item>
///     <description>Configurar el mapeo de las entidades <see cref="Cuerdas"/> y <see cref="Material"/> a sus tablas correspondientes.</description>
///   </item>
///   <item>
///     <description>Establecer conversión global de <see cref="Ulid"/> a <c>string</c> para todas las propiedades del modelo.</description>
///   </item>
///   <item>
///     <description>Aplicar soft-delete mediante <c>HasQueryFilter(u =&gt; !u.IsDeleted)</c> en ambas entidades.</description>
///   </item>
///   <item>
///     <description>Configurar timestamps automáticos (<c>CreatedAt</c>, <c>UpdatedAt</c>) a través de <c>ConfigureTimestamps()</c>.</description>
///   </item>
///   <item>
///     <description>Poblar datos de semilla para 8 cuerdas y 8 materiales en 5 torneos de prueba.</description>
///   </item>
/// </list>
/// <para><b>Entidades administradas:</b></para>
/// <list type="bullet">
///   <item><description><see cref="Cuerdas"/> → Tabla <c>Cuerdas</c>: cuerdas para raquetas (poliéster, tripa natural, híbridos, etc.).</description></item>
///   <item><description><see cref="Material"/> → Tabla <c>Materiales</c>: grips, overgrips, lead tape, siliconas y otros.</description></item>
/// </list>
/// <para><b>Convenciones globales:</b></para>
/// <list type="bullet">
///   <item><description>Conversión automática de <see cref="Ulid"/> a <c>string</c> mediante <see cref="UlidToStringConverterNonNullable"/>.</description></item>
/// </list>
/// </remarks>
/// <param name="options">Opciones de configuración del DbContext. Inyectadas a través del contenedor de dependencias (DI) con <c>AddDbContext&lt;MaterialsDbContext&gt;()</c>.</param>
public class MaterialsDbContext(DbContextOptions<MaterialsDbContext> options): DbContext(options)
{
    /// <summary>Catálogo de cuerdas y tensores para raquetas de tenis. Cada registro representa un modelo específico con marca, formato (reel/set) y tipo de material.</summary>
    public DbSet<Cuerdas> Cuerdas { get; set; }
    /// <summary>Catálogo de materiales complementarios para raquetas: grips, overgrips, lead tape, siliconas y otros accesorios.</summary>
    public DbSet<Material> Materiales { get; set; }

    /// <summary>
    /// Configura las convenciones globales de propiedades para todo el modelo.
    /// Establece que todas las propiedades de tipo <see cref="Ulid"/> se conviertan automáticamente
    /// a <c>string</c> para su almacenamiento en la base de datos.
    /// </summary>
    /// <remarks>
    /// <para>Esta convención evita tener que decorar cada propiedad <see cref="Ulid"/> individualmente
    /// con <c>HasConversion&lt;UlidToStringConverterNonNullable&gt;()</c> en el mapeo de cada entidad.</para>
    /// <para>Sin esta conversión, EF Core intentaría mapear <see cref="Ulid"/> como un tipo complejo,
    /// lo que causaría errores de esquema en bases de datos relacionales.</para>
    /// </remarks>
    /// <param name="configurationBuilder">
    /// Constructor de configuración de convenciones del modelo.
    /// Proporciona métodos fluidos para aplicar convenciones globales a todas las propiedades
    /// que coincidan con un tipo o criterio determinado.
    /// </param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Ulid>()
            .HaveConversion<UlidToStringConverterNonNullable>();
    }

    /// <summary>
    /// Configura el modelo de datos de EF Core para las entidades <see cref="Cuerdas"/> y <see cref="Material"/>.
    /// Define la estructura de tablas, restricciones de propiedades, conversiones de tipos,
    /// filtros de soft-delete, timestamps automáticos e inserta los datos de semilla.
    /// </summary>
    /// <remarks>
    /// <para><b>Configuración de <see cref="Cuerdas"/> (tabla <c>Cuerdas</c>):</b></para>
    /// <list type="bullet">
    ///   <item><description>Clave primaria: <c>Id</c> (tipo <c>long</c>, autogenerado).</description></item>
    ///   <item><description><c>TournamentId</c> requerido — asocia la cuerda a un torneo.</description></item>
    ///   <item><description><c>Marca</c> requerido, máximo 100 caracteres.</description></item>
    ///   <item><description><c>Modelo</c> requerido, máximo 100 caracteres.</description></item>
    ///   <item><description><c>Stock</c> requerido — cantidad disponible en inventario.</description></item>
    ///   <item><description><c>Precio</c> requerido — precio unitario en euros.</description></item>
    ///   <item><description><c>StringFormat</c> requerido — convertido a string desde enum <see cref="FormatoCuerda"/> (Reel/Set).</description></item>
    ///   <item><description><c>StringsType</c> requerido — convertido a string desde enum <see cref="StringsType"/> (Polyester, Multifilament, etc.).</description></item>
    ///   <item><description>Timestamps automáticos via <c>ConfigureTimestamps()</c>.</description></item>
    ///   <item><description>Filtro global de soft-delete: <c>HasQueryFilter(u =&gt; !u.IsDeleted)</c>.</description></item>
    /// </list>
    /// <para><b>Configuración de <see cref="Material"/> (tabla <c>Materiales</c>):</b></para>
    /// <list type="bullet">
    ///   <item><description>Clave primaria: <c>Id</c> (tipo <c>long</c>, autogenerado).</description></item>
    ///   <item><description><c>TournamentId</c> requerido — asocia el material a un torneo.</description></item>
    ///   <item><description><c>Marca</c> requerido, máximo 100 caracteres.</description></item>
    ///   <item><description><c>Modelo</c> requerido, máximo 100 caracteres.</description></item>
    ///   <item><description><c>Stock</c> requerido — cantidad disponible en inventario.</description></item>
    ///   <item><description><c>Precio</c> requerido — precio unitario en euros.</description></item>
    ///   <item><description><c>Type</c> requerido — convertido a string desde enum <see cref="MaterialType"/> (Grip, Overgrip, LeadTape, etc.).</description></item>
    ///   <item><description>Timestamps automáticos via <c>ConfigureTimestamps()</c>.</description></item>
    ///   <item><description>Filtro global de soft-delete: <c>HasQueryFilter(u =&gt; !u.IsDeleted)</c>.</description></item>
    /// </list>
    /// <para><b>Configuraciones compartidas:</b></para>
    /// <list type="bullet">
    ///   <item><description>Conversión de enums a string para <c>StringFormat</c>, <c>StringsType</c> y <c>Type</c> mediante <c>HasConversion&lt;string&gt;()</c>.</description></item>
    ///   <item><description>Inserción de datos de semilla (8 cuerdas + 8 materiales) vía <c>SeedData()</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="modelBuilder">
    /// Instancia de <see cref="ModelBuilder"/> que proporciona la API fluida para configurar
    /// el mapeo objeto-relacional de las entidades del dominio.
    /// </param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        SeedData(modelBuilder);

        modelBuilder.Entity<Cuerdas>(builder =>
        {
            builder.ToTable("Cuerdas"); 
            builder.HasKey(c => c.Id);  
            builder.Property(c => c.TournamentId).IsRequired();

            builder.Property(c => c.Marca).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Modelo).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Stock).IsRequired();
            builder.Property(c => c.Precio).IsRequired();
            builder.Property(c => c.Calibre).IsRequired();

            builder.Property(c => c.StringFormat)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(c => c.StringsType)
                .HasConversion<string>()
                .IsRequired();
            builder.ConfigureTimestamps();
            builder.HasQueryFilter(u => !u.IsDeleted);
        });

        modelBuilder.Entity<Material>(builder =>
        {
            builder.ToTable("Materiales");
            builder.HasKey(m => m.Id); 
            builder.Property(c => c.TournamentId).IsRequired();
            builder.Property(m => m.Marca).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Modelo).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Stock).IsRequired();
            builder.Property(m => m.Precio).IsRequired();

            builder.Property(m => m.Type)
                .HasConversion<string>()
                .IsRequired();

            builder.ConfigureTimestamps();
            
            builder.HasQueryFilter(u => !u.IsDeleted);
        });
    }

    /// <summary>
    /// Inserta datos de semilla en las tablas <c>Cuerdas</c> y <c>Materiales</c>
    /// para los 5 torneos de prueba predefinidos.
    /// Los datos representan un inventario realista de productos para encordados.
    /// </summary>
    /// <remarks>
    /// <para>Los TournamentId corresponden a los siguientes torneos:</para>
    /// <list type="table">
    ///   <item>
    ///     <term><c>t1</c> (01KS0Q28TEJ0SYA6JJ5H4W4CMP)</term>
    ///     <description>Torneo Madrileño 2025</description>
    ///   </item>
    ///   <item>
    ///     <term><c>t2</c> (01KS0Q28TE9N7TG55K98TCB4X0)</term>
    ///     <description>Open Circuit Barcelona</description>
    ///   </item>
    ///   <item>
    ///     <term><c>t3</c> (01KS0Q28TEVEYS4303TXP202N4)</term>
    ///     <description>Campeonato Regional Valencia</description>
    ///   </item>
    ///   <item>
    ///     <term><c>t4</c> (01KS0Q28TET0JHJV4T5YFDJWBW)</term>
    ///     <description>Cup Toledo - Elite</description>
    ///   </item>
    ///   <item>
    ///     <term><c>t5</c> (01KS0Q28TE5BA449NS2EVCBTDQ)</term>
    ///     <description>Torneo Prueba Excel</description>
    ///   </item>
    /// </list>
    /// <para><b>Datos insertados:</b></para>
    /// <list type="bullet">
    ///   <item><description><b>Cuerdas (8 registros):</b> Babolat RPM Blast, Wilson Champion's Choice, Luxilon ALU Power, Head Intellitour, Signum Pro Plasma, Kirschbaum Pro Line, Babolat VS Team, Technifibre XR2.</description></item>
    ///   <item><description><b>Materiales (8 registros):</b> Head Hydrosorb Comfort, Babolat Pro Overgrip, Tourna Lead Tape, Wilson ShockShield, Head Graphene 360+, Babolat Syn Pro, Head Super Soft, Tourna Grip Boost.</description></item>
    /// </list>
    /// <para>Todos los registros se crean con <c>IsDeleted = false</c> y timestamps
    /// relativos a la fecha actual (<c>DateTime.UtcNow</c>).</para>
    /// </remarks>
    /// <param name="modelBuilder">
    /// Instancia de <see cref="ModelBuilder"/> usada para configurar los datos iniciales
    /// mediante <c>HasData()</c>.
    /// </param>
    private void SeedData(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var t1 = Ulid.Parse("01KS0Q28TEJ0SYA6JJ5H4W4CMP");
        var t2 = Ulid.Parse("01KS0Q28TE9N7TG55K98TCB4X0");
        var t3= Ulid.Parse("01KS0Q28TEVEYS4303TXP202N4");
        var t4 = Ulid.Parse("01KS0Q28TET0JHJV4T5YFDJWBW");
        var t5 = Ulid.Parse("01KS0Q28TE5BA449NS2EVCBTDQ");
        modelBuilder.Entity<Cuerdas>().HasData(
            new Cuerdas
            {
                Id = 1,
                TournamentId = t1,
                Marca = "Babolat",
                Modelo = "RPM Blast",
                Stock = 50,
                Precio = 19.99,
                Calibre = 1.25,
                StringFormat = FormatoCuerda.Reel,
                StringsType = StringsType.Polyester,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-2),
                UpdatedAt = now.AddMonths(-2)
            },
            new Cuerdas
            {
                Id = 2,
                TournamentId = t1,
                Marca = "Wilson",
                Modelo = "Champion's Choice",
                Stock = 20,
                Precio = 24.99,
                Calibre = 1.30,
                StringFormat = FormatoCuerda.Set,
                StringsType = StringsType.NaturalGut,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-2),
                UpdatedAt = now.AddMonths(-2)
            },
            new Cuerdas
            {
                Id = 3,
                TournamentId = t2,
                Marca = "Luxilon",
                Modelo = "ALU Power",
                Stock = 45,
                Precio = 21.99,
                Calibre = 1.25,
                StringFormat = FormatoCuerda.Reel,
                StringsType = StringsType.Polyester,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            },
            new Cuerdas
            {
                Id = 4,
                TournamentId = t2,
                Marca = "Head",
                Modelo = "Intellitour",
                Stock = 30,
                Precio = 18.50,
                Calibre = 1.30,
                StringFormat = FormatoCuerda.Set,
                StringsType = StringsType.Multifilament,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            },
            new Cuerdas
            {
                Id = 5,
                TournamentId = t3,
                Marca = "Signum Pro",
                Modelo = "Plasma",
                Stock = 25,
                Precio = 17.99,
                Calibre = 1.28,
                StringFormat = FormatoCuerda.Reel,
                StringsType = StringsType.Hybrid,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-4),
                UpdatedAt = now.AddMonths(-3)
            },
            new Cuerdas
            {
                Id = 6,
                TournamentId = t4,
                Marca = "Kirschbaum",
                Modelo = "Pro Line",
                Stock = 35,
                Precio = 15.99,
                Calibre = 1.25,
                StringFormat = FormatoCuerda.Set,
                StringsType = StringsType.SyntheticGut,
                IsDeleted = false,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-30)
            },
            new Cuerdas
            {
                Id = 7,
                TournamentId = t5,
                Marca = "Babolat",
                Modelo = "VS Team",
                Stock = 40,
                Precio = 22.00,
                Calibre = 1.30,
                StringFormat = FormatoCuerda.Reel,
                StringsType = StringsType.NaturalGut,
                IsDeleted = false,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5)
            },
            new Cuerdas
            {
                Id = 8,
                TournamentId = t5,
                Marca = "Technifibre",
                Modelo = "XR2",
                Stock = 60,
                Precio = 18.50,
                Calibre = 1.30,
                StringFormat = FormatoCuerda.Set,
                StringsType = StringsType.Polyester,
                IsDeleted = false,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5)
            }
        );

        modelBuilder.Entity<Material>().HasData(
            new Material
            {
                Id = 1,
                TournamentId = t1,
                Marca = "Head",
                Modelo = "Hydrosorb Comfort",
                Stock = 100,
                Precio = 8.99,
                Type = MaterialType.Grip,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-2),
                UpdatedAt = now.AddMonths(-2)
            },
            new Material
            {
                Id = 2,
                TournamentId = t1,
                Marca = "Babolat",
                Modelo = "Pro Overgrip",
                Stock = 200,
                Precio = 3.99,
                Type = MaterialType.Overgrip,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-2),
                UpdatedAt = now.AddMonths(-2)
            },
            new Material
            {
                Id = 3,
                TournamentId = t2,
                Marca = "Tourna",
                Modelo = "Lead Tape 1/2\"",
                Stock = 80,
                Precio = 12.99,
                Type = MaterialType.LeadTape,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            },
            new Material
            {
                Id = 4,
                TournamentId = t2,
                Marca = "Wilson",
                Modelo = "ShockShield",
                Stock = 50,
                Precio = 9.99,
                Type = MaterialType.Silicone,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddMonths(-1)
            },
            new Material
            {
                Id = 5,
                TournamentId = t3,
                Marca = "Head",
                Modelo = "Graphene 360+",
                Stock = 10,
                Precio = 199.99,
                Type = MaterialType.Otro,
                IsDeleted = false,
                CreatedAt = now.AddMonths(-4),
                UpdatedAt = now.AddMonths(-3)
            },
            new Material
            {
                Id = 6,
                TournamentId = t4,
                Marca = "Babolat",
                Modelo = "Syn Pro",
                Stock = 75,
                Precio = 6.99,
                Type = MaterialType.Grip,
                IsDeleted = false,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-30)
            },
            new Material
            {
                Id = 7,
                TournamentId = t5,
                Marca = "Head",
                Modelo = "Super Soft",
                Stock = 150,
                Precio = 7.50,
                Type = MaterialType.Grip,
                IsDeleted = false,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5)
            },
            new Material
            {
                Id = 8,
                TournamentId = t5,
                Marca = "Tourna",
                Modelo = "Grip Boost",
                Stock = 100,
                Precio = 4.99,
                Type = MaterialType.Overgrip,
                IsDeleted = false,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5)
            }
        );
    }
}
