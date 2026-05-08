using System.Text;
using BackEncordados.Usuarios.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Extensiones de configuración de autenticación y autorización JWT.
/// </summary>
public static class AuthenticationConfig
{
    /// <summary>
    /// Configura autenticación JWT con tokens Bearer.
    /// </summary>
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
            .AddPolicy("RequireAdminRole", policy => policy.RequireRole(User.UserRoles.ADMIN,User.UserRoles.OWNER))
            .AddPolicy("RequireOwnerRole", policy => policy.RequireRole(User.UserRoles.OWNER))
            .AddPolicy("RequireUserRole", policy => policy.RequireRole(User.UserRoles.USER,User.UserRoles.ADMIN,User.UserRoles.OWNER,User.UserRoles.ENCORDER))
            .AddPolicy("RequireEncorderRole", policy =>policy.RequireRole(User.UserRoles.OWNER,User.UserRoles.ENCORDER,User.UserRoles.ADMIN));
        

        return services;
    }
}