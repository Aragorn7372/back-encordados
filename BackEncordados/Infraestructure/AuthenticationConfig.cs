using System.Text;
using BackEncordados.Usuarios.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Configuración de autenticación JWT Bearer y políticas de autorización
/// para la aplicación.
/// </summary>
/// <remarks>
/// <para>Proporciona un método de extensión sobre <c>IServiceCollection</c>
/// que configura la autenticación mediante JWT (JSON Web Tokens) y registra
/// cinco políticas de autorización basadas en roles.</para>
/// <para><b>Resolución de credenciales JWT:</b> Las claves se resuelven con el
/// siguiente orden de precedencia:</para>
/// <list type="number">
///   <item><description><b>Variables de entorno</b> (<c>JWT_KEY</c>, <c>JWT_ISSUER</c>, <c>JWT_AUDIENCE</c>)
///   — ideales para entornos Docker / contenedores.</description></item>
///   <item><description><b>appsettings.json</b> sección <c>Jwt:Key</c>, <c>Jwt:Issuer</c>, <c>Jwt:Audience</c>
///   — para desarrollo local.</description></item>
///   <item><description><b>Valores predeterminados</b> — Issuer y Audience usan <c>"TiendaApi"</c>
///   como fallback si no están configurados. Jwt:Key es obligatorio y lanza
///   excepción si no se encuentra.</description></item>
/// </list>
/// <para>Usar <c>services.AddAuthentication(configuration)</c> en <c>Program.cs</c>:</para>
/// <code>
/// builder.Services.AddAuthentication(builder.Configuration);
/// </code>
/// </remarks>
public static class AuthenticationConfig
{
    /// <summary>
    /// Configura la autenticación JWT Bearer y las políticas de autorización.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Obtiene <c>JWT_KEY</c> desde variable de entorno <c>JWT_KEY</c>;
    ///   si no existe, intenta desde <c>configuration["Jwt:Key"]</c>;
    ///   si ninguna está presente, lanza <c>InvalidOperationException</c>.</description></item>
    ///   <item><description>Obtiene <c>JWT_ISSUER</c> desde entorno o configuración;
    ///   default <c>"TiendaApi"</c>.</description></item>
    ///   <item><description>Obtiene <c>JWT_AUDIENCE</c> desde entorno o configuración;
    ///   default <c>"TiendaApi"</c>.</description></item>
    ///   <item><description>Registra autenticación JWT Bearer con <c>AddJwtBearer</c>:</description></item>
    /// </list>
    /// <para><b>Parámetros de validación del token:</b></para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Parámetro</term>
    ///     <description>Valor</description>
    ///     <description>Propósito</description>
    ///   </listheader>
    ///   <item>
    ///     <term>ValidateIssuerSigningKey</term>
    ///     <description>true</description>
    ///     <description>Valida que la firma del token sea válida.</description>
    ///   </item>
    ///   <item>
    ///     <term>IssuerSigningKey</term>
    ///     <description><c>SymmetricSecurityKey</c> desde UTF8.GetBytes(jwtKey)</description>
    ///     <description>Clave simétrica para verificar la firma HMAC.</description>
    ///   </item>
    ///   <item>
    ///     <term>ValidateIssuer</term>
    ///     <description>true</description>
    ///     <description>Valida que el emisor del token sea el esperado.</description>
    ///   </item>
    ///   <item>
    ///     <term>ValidIssuer</term>
    ///     <description>jwtIssuer</description>
    ///     <description>Emisor válido configurado.</description>
    ///   </item>
    ///   <item>
    ///     <term>ValidateAudience</term>
    ///     <description>true</description>
    ///     <description>Valida que la audiencia del token sea la esperada.</description>
    ///   </item>
    ///   <item>
    ///     <term>ValidAudience</term>
    ///     <description>jwtAudience</description>
    ///     <description>Audiencia válida configurada.</description>
    ///   </item>
    ///   <item>
    ///     <term>ValidateLifetime</term>
    ///     <description>true</description>
    ///     <description>Valida que el token no haya expirado.</description>
    ///   </item>
    ///   <item>
    ///     <term>ClockSkew</term>
    ///     <description>TimeSpan.Zero</description>
    ///     <description>Sin margen de tiempo (el token expira exactamente cuando toca).</description>
    ///   </item>
    ///   <item>
    ///     <term>NameClaimType</term>
    ///     <description>ClaimTypes.NameIdentifier</description>
    ///     <description>Claim que identifica al usuario (User.FindFirst(ClaimTypes.NameIdentifier)).</description>
    ///   </item>
    ///   <item>
    ///     <term>RoleClaimType</term>
    ///     <description>ClaimTypes.Role</description>
    ///     <description>Claim que contiene el rol del usuario.</description>
    ///   </item>
    /// </list>
    /// <para><b>MapInboundClaims = false:</b> Deshabilita el mapeo automático de claims
    /// que realiza ASP.NET Core, preservando los nombres originales de los claims
    /// (especialmente <c>ClaimTypes.NameIdentifier</c> y <c>ClaimTypes.Role</c>).</para>
    /// <para><b>Políticas de autorización registradas:</b></para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Policy</term>
    ///     <description>Roles requeridos</description>
    ///     <description>Uso</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>RequireAdminRole</c></term>
    ///     <description>ADMIN</description>
    ///     <description>Exportación/importación de BD, configuración global.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>RequireOwnerRole</c></term>
    ///     <description>OWNER, ADMIN</description>
    ///     <description>Gestión de torneos, exportación avanzada, importación Excel.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>RequireUserRole</c></term>
    ///     <description>USER, ADMIN, OWNER, ENCORDER</description>
    ///     <description>Operaciones generales de usuario.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>RequireEncorderRole</c></term>
    ///     <description>OWNER, ENCORDER, ADMIN</description>
    ///     <description>Operaciones de encordado.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>RequireSupervisorRole</c></term>
    ///     <description>OWNER, ADMIN, SUPERVISOR, ENCORDER</description>
    ///     <description>Exportación simple de torneos, supervisión.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <returns>La misma colección de servicios para encadenamiento fluido.</returns>
    /// <exception cref="InvalidOperationException">JWT Key no está configurada
    /// ni en variable de entorno ni en appsettings.</exception>
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Information("Configurando autenticación JWT...");

        // Lee primero las variables de entorno directas, luego las jerárquicas
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") 
            ?? configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada");
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
            ?? configuration["Jwt:Issuer"] 
            ?? "TiendaApi";
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
            ?? configuration["Jwt:Audience"] 
            ?? "TiendaApi";

        Log.Debug("JWT Issuer: {Issuer}", jwtIssuer);
        Log.Debug("JWT Audience: {Audience}", jwtAudience);
        Log.Debug("JWT Key length: {Length} chars", jwtKey.Length);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };
            options.MapInboundClaims = false;
        });

        Log.Information("Configurando políticas de autorización...");
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdminRole", policy => policy.RequireRole(User.UserRoles.ADMIN))
            .AddPolicy("RequireOwnerRole", policy => policy.RequireRole(User.UserRoles.OWNER,User.UserRoles.ADMIN))
            .AddPolicy("RequireUserRole", policy => policy.RequireRole(User.UserRoles.USER,User.UserRoles.ADMIN,User.UserRoles.OWNER,User.UserRoles.ENCORDER))
            .AddPolicy("RequireEncorderRole", policy =>policy.RequireRole(User.UserRoles.OWNER,User.UserRoles.ENCORDER,User.UserRoles.ADMIN))
            .AddPolicy("RequireSupervisorRole", policy => policy.RequireRole(User.UserRoles.OWNER,User.UserRoles.ADMIN,User.UserRoles.SUPERVISOR,User.UserRoles.ENCORDER));

        return services;
    }
}