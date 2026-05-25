using BackEncordados.Common.Database.Helpers;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Usuarios.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.Database.Config;

/// <summary>
/// DbContext de EF Core para la gestión de usuarios del sistema.
/// Administra la tabla <c>users</c> con autenticación basada en email/password
/// hasheado con BCrypt y control de concurrencia optimista mediante Version.
/// </summary>
/// <remarks>
/// <para><b>Responsabilidades:</b></para>
/// <list type="bullet">
///   <item><description>Configurar el mapeo de la entidad <see cref="User"/> a la tabla <c>users</c>.</description></item>
///   <item><description>Establecer la conversión de <see cref="Ulid"/> a <c>string</c> para <c>Id</c> (no-nullable) y <c>TournamentId</c> (nullable).</description></item>
///   <item><description>Configurar índices únicos en los campos <c>Email</c> y <c>Username</c>.</description></item>
///   <item><description>Aplicar control de concurrencia optimista mediante la propiedad <c>Version</c> como token de concurrencia.</description></item>
///   <item><description>Aplicar soft-delete mediante <c>HasQueryFilter(u =&gt; !u.IsDeleted)</c>.</description></item>
///   <item><description>Poblar datos de semilla para 9 usuarios (ADMIN, OWNER, ENCORDER×2, USER×4, SUPERVISOR×2) con contraseñas hasheadas.</description></item>
/// </list>
/// <para><b>Características distintivas frente a otros DbContexts:</b></para>
/// <list type="bullet">
///   <item><description>No registra convenciones globales en <c>ConfigureConventions</c>: las conversiones de <see cref="Ulid"/> se configuran inline en el mapeo de <c>User</c>.</description></item>
///   <item><description>Utiliza <see cref="UlidToStringConverter"/> (nullable) para <c>TournamentId</c> y <see cref="UlidToStringConverterNonNullable"/> para <c>Id</c>.</description></item>
///   <item><description>Incluye <c>IsConcurrencyToken()</c> en la propiedad <c>Version</c> para detección de conflictos de escritura concurrente.</description></item>
///   <item><description>Define nombres explícitos de índice: <c>IX_users_email_unique</c> y <c>IX_users_username_unique</c>.</description></item>
/// </list>
/// </remarks>
/// <param name="options">Opciones de configuración del DbContext. Inyectadas a través del contenedor de dependencias (DI) con <c>AddDbContext&lt;UserDbContext&gt;()</c>.</param>
public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Configuración de convenciones globales del modelo.
    /// Intencionalmente vacío: las conversiones de tipo se definen inline en el mapeo
    /// de <see cref="User"/> dentro de <see cref="OnModelCreating"/>.
    /// </summary>
    /// <remarks>
    /// A diferencia de otros DbContexts del proyecto (<see cref="MaterialsDbContext"/>,
    /// <see cref="PedidosDbContext"/>, <see cref="TalleresDbContext"/>), este DbContext
    /// no registra una conversión global de <see cref="Ulid"/> a <c>string</c>.
    /// En su lugar, cada propiedad <see cref="Ulid"/> de <c>User</c> se convierte
    /// individualmente usando <c>HasConversion</c>:
    /// <list type="bullet">
    ///   <item><description><c>Id</c> → <see cref="UlidToStringConverterNonNullable"/></description></item>
    ///   <item><description><c>TournamentId</c> → <see cref="UlidToStringConverter"/> (nullable)</description></item>
    /// </list>
    /// </remarks>
    /// <param name="configurationBuilder">Constructor de configuración de convenciones del modelo.</param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
    }

    /// <summary>
    /// Configura el modelo de datos de EF Core para la entidad <see cref="User"/>.
    /// Define la estructura de la tabla <c>users</c>, restricciones de propiedades,
    /// conversiones de Ulid, índices únicos, token de concurrencia, timestamps,
    /// filtro de soft-delete e inserta los datos de semilla.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo de configuración:</b></para>
    /// <list type="number">
    ///   <item><description><c>base.OnModelCreating(modelBuilder)</c> — ejecuta configuraciones base de EF Core.</description></item>
    ///   <item><description><c>SeedData(modelBuilder)</c> — inserta datos de semilla.</description></item>
    ///   <item><description>Configuración inline de <see cref="User"/> — mapeo completo de propiedades, índices y filtros.</description></item>
    /// </list>
    /// <para><b>Propiedades configuradas:</b></para>
    /// <list type="bullet">
    ///   <item><description><c>Id</c> — clave primaria autogenerada (<see cref="UlidValueGenerator"/>), convertida a string con <see cref="UlidToStringConverterNonNullable"/>.</description></item>
    ///   <item><description><c>Username</c> — requerido, máximo 50 caracteres, Unicode <c>false</c> (ASCII). Índice único.</description></item>
    ///   <item><description><c>Email</c> — requerido, máximo 100 caracteres, Unicode <c>false</c> (ASCII). Índice único.</description></item>
    ///   <item><description><c>PasswordHash</c> — requerido. Hash BCrypt de la contraseña.</description></item>
    ///   <item><description><c>Role</c> — requerido, máximo 20 caracteres. Valores: ADMIN, OWNER, ENCORDER, SUPERVISOR, USER.</description></item>
    ///   <item><description><c>Phone</c> — opcional, máximo 20 caracteres.</description></item>
    ///   <item><description><c>ImageUrl</c> — requerido, máximo 500 caracteres. URL de avatar en Cloudinary.</description></item>
    ///   <item><description><c>CloudinaryPublicId</c> — opcional, máximo 300 caracteres. ID público en Cloudinary.</description></item>
    ///   <item><description><c>IsDeleted</c> — valor por defecto <c>false</c>. Soft-delete.</description></item>
    ///   <item><description><c>Bonos</c> — valor por defecto <c>0</c>. Bono disponible del jugador.</description></item>
    ///   <item><description><c>TournamentId</c> — opcional (nullable), convertido a string con <see cref="UlidToStringConverter"/>.</description></item>
    ///   <item><description><c>Version</c> — token de concurrencia (<c>IsConcurrencyToken()</c>). Se incrementa automáticamente vía <see cref="VersionInterceptor"/>.</description></item>
    ///   <item><description><c>CreatedAt</c> / <c>UpdatedAt</c> — timestamps automáticos vía <c>ConfigureTimestamps()</c>.</description></item>
    /// </list>
    /// <para><b>Índices únicos:</b></para>
    /// <list type="bullet">
    ///   <item><description><c>IX_users_email_unique</c> — único, sobre propiedad <c>Email</c>.</description></item>
    ///   <item><description><c>IX_users_username_unique</c> — único, sobre propiedad <c>Username</c>.</description></item>
    /// </list>
    /// <para><b>Filtros:</b></para>
    /// <list type="bullet">
    ///   <item><description>Soft-delete global: <c>HasQueryFilter(u =&gt; !u.IsDeleted)</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="modelBuilder">Instancia de <see cref="ModelBuilder"/> para configurar el mapeo ORM.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        SeedData(modelBuilder);
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasConversion<UlidToStringConverterNonNullable>().HasValueGenerator<UlidValueGenerator>();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100).IsUnicode(false);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
            entity.Property(u => u.Phone).HasMaxLength(20);
            entity.Property(u => u.ImageUrl).IsRequired().HasMaxLength(500);
            entity.Property(u => u.CloudinaryPublicId).HasMaxLength(300);
            entity.Property(u => u.IsDeleted).HasDefaultValue(false);
            entity.Property(u => u.Bonos).HasDefaultValue(0);
            entity.Property(u => u.Version).IsConcurrencyToken();
            entity.ConfigureTimestamps();
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_users_email_unique");
            entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("IX_users_username_unique");
            entity.HasQueryFilter(u => !u.IsDeleted);
            entity.Property(u => u.TournamentId).HasConversion<UlidToStringConverter>().IsRequired(false);
        });
    }

    /// <summary>Colección de usuarios del sistema: administradores, propietarios, encordadores, supervisores y jugadores.</summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Inserta datos de semilla en la tabla <c>users</c> con 9 usuarios de prueba
    /// que cubren todos los roles del sistema.
    /// Las contraseñas se hashean con BCrypt (workFactor: 11) para simular el comportamiento real.
    /// </summary>
    /// <remarks>
    /// <para><b>Roles insertados:</b></para>
    /// <list type="table">
    ///   <item><term>ADMIN</term><description><c>admin_encordados</c> — acceso total al sistema.</description></item>
    ///   <item><term>OWNER</term><description><c>owner_principal</c> — propietario de torneos.</description></item>
    ///   <item><term>ENCORDER</term><description><c>carlos_encordador</c>, <c>maria_encordadora</c> — encordadores asignados a pedidos.</description></item>
    ///   <item><term>USER</term><description><c>jugador_juan</c> (bono 100), <c>jugador_ana</c> (bono 25), <c>jugador_pedro</c> (bono 0), <c>user</c> (bono 0).</description></item>
    ///   <item><term>SUPERVISOR</term><description><c>supervisor_luis</c>, <c>supervisor_pablo</c> — supervisores de torneos.</description></item>
    /// </list>
    /// <para><b>Credenciales de prueba:</b></para>
    /// <list type="table">
    ///   <item><term>admin@encordados.com</term><description>admin123</description></item>
    ///   <item><term>owner@encordados.com</term><description>owner123</description></item>
    ///   <item><term>carlos@encordados.com / maria@encordados.com</term><description>encorder123</description></item>
    ///   <item><term>juan@tenis.com / ana@tenis.com / pedro@tenis.com / user@example.com</term><description>user123</description></item>
    ///   <item><term>luis@encordados.com / pablo@encordados.com</term><description>supervisor123</description></item>
    /// </list>
    /// <para><b>Datos insertados:</b></para>
    /// <list type="bullet">
    ///   <item><description><b>9 usuarios</b> con contraseñas hasheadas, imágenes por defecto de Cloudinary y timestamps relativos a la fecha actual.</description></item>
    ///   <item><description>Los jugadores <c>juan</c> y <c>ana</c> están asignados al torneo t1 (Torneo Madrileño 2025) y tienen bonos.</description></item>
    ///   <item><description>El jugador <c>pedro</c> está asignado al torneo t2 (Open Circuit Barcelona).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="modelBuilder">Instancia de <see cref="ModelBuilder"/> usada para configurar los datos iniciales mediante <c>HasData()</c>.</param>
    private void SeedData(ModelBuilder modelBuilder)
    {
        // Generar hashes de contraseñas para los datos de prueba
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 11);
        var userPasswordHash = BCrypt.Net.BCrypt.HashPassword("user123", workFactor: 11);
        var encorderPasswordHash = BCrypt.Net.BCrypt.HashPassword("encorder123", workFactor: 11);
        var ownerPasswordHash = BCrypt.Net.BCrypt.HashPassword("owner123", workFactor: 11);
        var supervisorPasswordHash = BCrypt.Net.BCrypt.HashPassword("supervisor123", workFactor: 11);
        var juan = Ulid.Parse("01KS0Q28TD6SAPN0GN0XKRPK5D");
        var ana = Ulid.Parse("01KS0Q28TE3RJTW6W35NJRMTZ4");
        var pedro = Ulid.Parse("01KS0Q28TED4PWJPT7DMJ46WBN");
        var carlos = Ulid.Parse("01KS0Q28TE7CMWS2D8RVDFA7YJ");
        var maria = Ulid.Parse("01KS0Q28TE6CVB0NYYANTWEK7B");
        var admin= Ulid.Parse("01KS0Q28TESE956013XYJKP6ST");
        var owner= Ulid.Parse("01KS0Q28TE2EFVQTCW8EN0W0MF");
        var supervisor1 = Ulid.Parse("01KS0Q28TEHA2KF5YM3J6QS5Z9");
        var supervisor2 = Ulid.Parse("01KS0Q28TEXTDY9TQNRAXKAJ81");
        var t1 = Ulid.Parse("01KS0Q28TEJ0SYA6JJ5H4W4CMP");
        var t2 = Ulid.Parse("01KS0Q28TE9N7TG55K98TCB4X0");
        var user = Ulid.Parse("01KS0Q28TE5BA449NS2EVCBTDQ");
        var users = new List<User>
        {
            
            // Usuario ADMIN
            new() {
                Id = admin,
                Username = "admin_encordados",
                Name = "Administrador",
                Email = "admin@encordados.com",
                Phone = "912345678",
                PasswordHash = adminPasswordHash,
                Role = User.UserRoles.ADMIN,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow.AddMonths(-6),
                TournamentId = null,
                
            },

            // Usuario OWNER
            new() {
                Id = owner,
                Username = "owner_principal",
                Name = "Propietario",
                Email = "owner@encordados.com",
                Phone = "923456789",
                PasswordHash = ownerPasswordHash,
                Role = User.UserRoles.OWNER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-5),
                UpdatedAt = DateTime.UtcNow.AddMonths(-5),
                TournamentId = null,
                
            },

              
            // Usuario ENCORDER 1
            new() {
                Id = carlos,
                Username = "carlos_encordador",
                Name = "Carlos García",
                Email = "carlos@encordados.com",
                Phone = "934567890",
                PasswordHash = encorderPasswordHash,
                Role = User.UserRoles.ENCORDER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                UpdatedAt = DateTime.UtcNow.AddMonths(-4),
                TournamentId = null,
                
            },

            // Usuario ENCORDER 2
            new() {
                Id = maria,
                Username = "maria_encordadora",
                Name = "María López",
                Email = "maria@encordados.com",
                Phone = "945678901",
                PasswordHash = encorderPasswordHash,
                Role = User.UserRoles.ENCORDER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UpdatedAt = DateTime.UtcNow.AddMonths(-3),
                TournamentId = null,
                
            },

            // Usuario JUGADOR 1
            new() {
                Id = juan,
                Username = "jugador_juan",
                Name = "Juan Martínez",
                Email = "juan@tenis.com",
                Phone = "956789012",
                PasswordHash = userPasswordHash,
                Role = User.UserRoles.USER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                UpdatedAt = DateTime.UtcNow.AddMonths(-2),
                TournamentId = t1,
                Bonos = 100.0,
                
            },

            // Usuario JUGADOR 2
            new() {
                Id = ana,
                Username = "jugador_ana",
                Name = "Ana Pérez",
                Email = "ana@tenis.com",
                Phone = "967890123",
                PasswordHash = userPasswordHash,
                Role = User.UserRoles.USER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                UpdatedAt = DateTime.UtcNow.AddMonths(-1),
                TournamentId = t1,
                Bonos = 25.0,
                
            },

            // Usuario JUGADOR 3
            new() {
                Id = pedro,
                Username = "jugador_pedro",
                Name = "Pedro Rodríguez",
                Email = "pedro@tenis.com",
                Phone = "978901234",
                PasswordHash = userPasswordHash,
                Role = User.UserRoles.USER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TournamentId = t2,
                Bonos = 0,
                
            },

            // Usuario SUPERVISOR 1
            new() {
                Id = supervisor1,
                    Username = "supervisor_luis",
                Name = "Luis Fernández",
                Email = "luis@encordados.com",
                Phone = "989012345",
                PasswordHash = supervisorPasswordHash,
                Role = User.UserRoles.SUPERVISOR,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UpdatedAt = DateTime.UtcNow.AddMonths(-3),
                TournamentId = null,
                
            },

            // Usuario SUPERVISOR 2
            new() {
                Id = supervisor2,
                Username = "supervisor_pablo",
                Name = "Pablo Sánchez",
                Email = "pablo@encordados.com",
                Phone = "990123456",
                PasswordHash = supervisorPasswordHash,
                Role = User.UserRoles.SUPERVISOR,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                UpdatedAt = DateTime.UtcNow.AddMonths(-2),
                TournamentId = null,
                
            },
            new() {
                Id = user,
                Username = "user",
                Name = "User",
                Email = "user@example.com",
                Phone = "999999000",
                PasswordHash = userPasswordHash,
                Role = User.UserRoles.USER,
                IsDeleted = false,
                ImageUrl = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                CloudinaryPublicId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TournamentId = null,
            }
        };

        modelBuilder.Entity<User>().HasData(users);
    }
}
