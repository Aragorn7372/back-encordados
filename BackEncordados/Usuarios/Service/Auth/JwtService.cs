using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BackEncordados.Usuarios.Model;
using Microsoft.IdentityModel.Tokens;

namespace BackEncordados.Usuarios.Service.Auth;

/// <summary>
/// Implementación de <see cref="IJwtService"/> para generación y validación de tokens JWT
/// usando HMAC-SHA256 con clave simétrica.
/// </summary>
/// <remarks>
/// <para><b>Configuración requerida (appsettings.json):</b></para>
/// <list type="table">
///   <listheader>
///     <term>Clave</term>
///     <description>Propósito</description>
///     <description>Valor por defecto</description>
///   </listheader>
///   <item>
///     <term><c>Jwt:Key</c></term>
///     <description>Clave secreta para firmar los tokens (obligatorio).</description>
///     <description><em>(sin valor por defecto — lanza <see cref="InvalidOperationException"/>)</em></description>
///   </item>
///   <item>
///     <term><c>Jwt:Issuer</c></term>
///     <description>Emisor del token.</description>
///     <description><c>"Encordados"</c></description>
///   </item>
///   <item>
///     <term><c>Jwt:Audience</c></term>
///     <description>Audiencia del token.</description>
///     <description><c>"Encorders"</c></description>
///   </item>
///   <item>
///     <term><c>Jwt:ExpireMinutes</c></term>
///     <description>Tiempo de expiración en minutos.</description>
///     <description><c>60</c></description>
///   </item>
/// </list>
/// <para>Claims incluidos en el token: Sub (username), Email, Role, NameIdentifier (ULID), Jti (UUID único).</para>
/// <para>Algoritmo de firma: <c>SecurityAlgorithms.HmacSha256</c>.</para>
/// </remarks>
public class JwtService(
    IConfiguration configuration,
    ILogger<JwtService> logger
) : IJwtService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<JwtService> _logger = logger;

    /// <summary>
    /// Genera un token JWT firmado con la información del usuario.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Lee la configuración JWT (Key, Issuer, Audience, ExpireMinutes) del appsettings.</description></item>
    ///   <item><description>Crea la clave simétrica y las credenciales de firma HMAC-SHA256.</description></item>
    ///   <item><description>Construye los claims: Sub (username), Email, Role, NameIdentifier (ULID del usuario), Jti (UUID).</description></item>
    ///   <item><description>Genera el JWT con el handler estándar y lo serializa a string.</description></item>
    /// </list>
    /// <para><b>Casos borde:</b> Si <c>Jwt:Key</c> no está configurada, lanza <see cref="InvalidOperationException"/>.</para>
    /// </remarks>
    /// <param name="user">Usuario para el que se genera el token.</param>
    /// <returns>Token JWT firmado como string.</returns>
    /// <exception cref="InvalidOperationException">Si la clave JWT no está configurada en appsettings.</exception>
    public string GenerateToken(User user)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada");
        var issuer = _configuration["Jwt:Issuer"] ?? "Encordados";
        var audience = _configuration["Jwt:Audience"] ?? "Encorders";
        var expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        
        _logger.LogInformation("Token JWT generado para usuario: {Username}", user.Username);
        
        return tokenString;
    }

    /// <summary>
    /// Valida un token JWT y extrae el nombre de usuario (Sub claim).
    /// </summary>
    /// <remarks>
    /// <para><b>Validaciones aplicadas:</b></para>
    /// <list type="bullet">
    ///   <item><description>Validez de la clave de firma (<c>ValidateIssuerSigningKey</c>).</description></item>
    ///   <item><description>Emisor (<c>ValidateIssuer</c>).</description></item>
    ///   <item><description>Audiencia (<c>ValidateAudience</c>).</description></item>
    ///   <item><description>Tiempo de vida (<c>ValidateLifetime</c>) con <c>ClockSkew = TimeSpan.Zero</c>.</description></item>
    /// </list>
    /// <para>Si la validación falla por cualquier razón (token expirado, firma inválida, etc.),
    /// retorna <c>null</c> en lugar de lanzar excepción.</para>
    /// </remarks>
    /// <param name="token">Token JWT a validar.</param>
    /// <returns>Username extraído del claim Sub, o <c>null</c> si el token es inválido.</returns>
    public string? ValidateToken(string token)
    {
        try
        {
            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key no configurada");
            var issuer = _configuration["Jwt:Issuer"] ?? "Encordados";
            var audience = _configuration["Jwt:Audience"] ?? "Encorders";

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var username = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;

            return username;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Validación de token JWT fallida");
            return null;
        }
    }
}