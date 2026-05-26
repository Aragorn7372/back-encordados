using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BackEncordados.Usuarios.Service.Auth;

/// <summary>
/// Implementación de <see cref="IJwtTokenExtractor"/> para extraer información de tokens JWT
/// sin validar la firma, útil para inspección de tokens existentes.
/// </summary>
/// <remarks>
/// <para>Proporciona siete métodos de extracción:</para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>ExtractUserId</c></term>
///     <description>Extrae el ID de usuario (NameIdentifier, nameid o Sub) como <c>long?</c>.</description>
///   </item>
///   <item>
///     <term><c>ExtractRole</c></term>
///     <description>Extrae el rol del usuario (Role o role).</description>
///   </item>
///   <item>
///     <term><c>IsAdmin</c></term>
///     <description>Verifica si el rol es "admin" (case-insensitive).</description>
///   </item>
///   <item>
///     <term><c>ExtractUserInfo</c></term>
///     <description>Tuple con UserId, IsAdmin y Role.</description>
///   </item>
///   <item>
///     <term><c>ExtractClaims</c></term>
///     <description>Extrae todos los claims como <see cref="ClaimsPrincipal"/> (con fallback a parseo manual del payload).</description>
///   </item>
///   <item>
///     <term><c>ExtractEmail</c></term>
///     <description>Extrae el email del token.</description>
///   </item>
///   <item>
///     <term><c>IsValidTokenFormat</c></term>
///     <description>Valida el formato estructural del JWT (3 partes separadas por punto).</description>
///   </item>
/// </list>
/// <para>No realiza validación criptográfica de la firma — solo lectura de claims del token.</para>
/// <para>Normaliza los tipos de claim: <c>nameid</c>/<c>sub</c> → <c>NameIdentifier</c>, <c>email</c> → <c>Email</c>, <c>role</c>/<c>roles</c> → <c>Role</c>, <c>name</c> → <c>Name</c>.</para>
/// </remarks>
public class JwtTokenExtractor : IJwtTokenExtractor
{
    private readonly ILogger<JwtTokenExtractor> _logger;

    /// <summary>
    /// Inicializa el extractor con un logger.
    /// </summary>
    /// <param name="logger">Logger para seguimiento de operaciones de extracción.</param>
    public JwtTokenExtractor(ILogger<JwtTokenExtractor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extrae el ID de usuario del token JWT.
    /// </summary>
    /// <remarks>
    /// <para>Busca el primer claim que coincida con <c>ClaimTypes.NameIdentifier</c>, <c>"nameid"</c> o <c>JwtRegisteredClaimNames.Sub</c>.</para>
    /// <para>Retorna <c>null</c> si el valor no es un <c>long</c> válido o si ocurre cualquier error.</para>
    /// </remarks>
    /// <param name="token">Token JWT del que extraer el userId.</param>
    /// <returns>UserId como <c>long</c>, o <c>null</c> si no se encuentra o el formato es inválido.</returns>
    public long? ExtractUserId(string token)
    {
        try
        {
            var jwtToken = ReadToken(token);
            if (jwtToken == null) return null;

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => 
                c.Type == ClaimTypes.NameIdentifier || 
                c.Type == "nameid" ||
                c.Type == JwtRegisteredClaimNames.Sub);

            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extrayendo userId del token");
            return null;
        }
    }

    /// <summary>
    /// Extrae el rol del usuario del token JWT.
    /// </summary>
    /// <remarks>
    /// <para>Busca el primer claim que coincida con <c>ClaimTypes.Role</c> o <c>"role"</c>.</para>
    /// </remarks>
    /// <param name="token">Token JWT del que extraer el rol.</param>
    /// <returns>Nombre del rol como string, o <c>null</c> si no se encuentra.</returns>
    public string? ExtractRole(string token)
    {
        try
        {
            var jwtToken = ReadToken(token);
            if (jwtToken == null) return null;

            var roleClaim = jwtToken.Claims.FirstOrDefault(c => 
                c.Type == ClaimTypes.Role || 
                c.Type == "role");

            return roleClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extrayendo rol del token");
            return null;
        }
    }

    /// <summary>
    /// Verifica si el token pertenece a un administrador.
    /// </summary>
    /// <param name="token">Token JWT a verificar.</param>
    /// <returns><c>true</c> si el rol es "admin" (case-insensitive), <c>false</c> en caso contrario.</returns>
    public bool IsAdmin(string token)
    {
        var role = ExtractRole(token);
        return role?.Equals("admin", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Extrae información completa del usuario del token: UserId, IsAdmin y Role.
    /// </summary>
    /// <param name="token">Token JWT del que extraer la información.</param>
    /// <returns>Tupla con (UserId, IsAdmin, Role).</returns>
    public (long? UserId, bool IsAdmin, string? Role) ExtractUserInfo(string token)
    {
        var userId = ExtractUserId(token);
        var role = ExtractRole(token);
        var isAdmin = role?.Equals("admin", StringComparison.OrdinalIgnoreCase) ?? false;

        return (userId, isAdmin, role);
    }

    /// <summary>
    /// Extrae todos los claims del token JWT como un <see cref="ClaimsPrincipal"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Estrategia de extracción:</b></para>
    /// <list type="number">
    ///   <item><description>Intenta parsear el token con <c>JwtSecurityTokenHandler.ReadJwtToken</c>.</description></item>
    ///   <item><description>Si el token es válido, extrae claims normalizando los tipos.</description></item>
    ///   <item><description>Si falla el parseo, intenta leer manualmente el payload Base64Url y extraer propiedades de tipo string.</description></item>
    ///   <item><description>Si ambas estrategias fallan, retorna <c>null</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="token">Token JWT del que extraer los claims.</param>
    /// <returns><see cref="ClaimsPrincipal"/> con los claims extraídos, o <c>null</c> si el token es inválido.</returns>
    public ClaimsPrincipal? ExtractClaims(string token)
    {
        try
        {
            var jwtToken = ReadToken(token);
            List<Claim> claims;

            if (jwtToken != null && jwtToken.Claims.Any())
            {
                claims = jwtToken.Claims.Select(c => new Claim(NormalizeClaimType(c.Type), c.Value, c.ValueType, c.Issuer, c.OriginalIssuer)).ToList();
            }
            else
            {
                var parts = token.Split('.');
                if (parts.Length != 3)
                    return null;

                var payload = parts[1];
                if (string.IsNullOrWhiteSpace(payload))
                    return null;

                var payloadJson = Base64UrlDecode(payload);
                var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
                using var doc = System.Text.Json.JsonDocument.Parse(payloadBytes);
                var root = doc.RootElement;

                claims = new List<Claim>();
                
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var name = prop.Name;
                        var value = prop.Value.GetString() ?? "";
                        var claimType = NormalizeClaimType(name);
                        
                        claims.Add(new Claim(claimType, value));
                    }
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extrayendo claims del token");
            return null;
        }
    }

    /// <summary>
    /// Normaliza un tipo de claim JWT a su equivalente en <see cref="ClaimTypes"/>.
    /// </summary>
    /// <param name="type">Nombre del claim en el token.</param>
    /// <returns>Tipo de claim normalizado.</returns>
    private static string NormalizeClaimType(string type)
    {
        var lower = type.ToLowerInvariant();
        return lower switch
        {
            "nameid" or "sub" => ClaimTypes.NameIdentifier,
            "email" => ClaimTypes.Email,
            "role" or "roles" => ClaimTypes.Role,
            "name" => ClaimTypes.Name,
            _ => type
        };
    }

    /// <summary>
    /// Extrae el email del usuario del token JWT.
    /// </summary>
    /// <param name="token">Token JWT del que extraer el email.</param>
    /// <returns>Email como string, o <c>null</c> si no se encuentra.</returns>
    public string? ExtractEmail(string token)
    {
        try
        {
            var jwtToken = ReadToken(token);
            if (jwtToken == null) return null;

            var emailClaim = jwtToken.Claims.FirstOrDefault(c => 
                c.Type == JwtRegisteredClaimNames.Email || 
                c.Type == ClaimTypes.Email);

            return emailClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extrayendo email del token");
            return null;
        }
    }

    /// <summary>
    /// Valida el formato estructural de un token JWT sin verificar la firma.
    /// </summary>
    /// <remarks>
    /// <para>Verifica que el token tenga tres partes separadas por punto (header, payload, signature).</para>
    /// <para>Si no tiene firma, verifica que el algoritmo en el header sea "none" (token sin firmar).</para>
    /// </remarks>
    /// <param name="token">Token JWT a validar estructuralmente.</param>
    /// <returns><c>true</c> si el formato es válido, <c>false</c> en caso contrario.</returns>
    public bool IsValidTokenFormat(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                return false;

            var header = parts[0];
            var payload = parts[1];
            var signature = parts[2];

            if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(payload))
                return false;

            if (!string.IsNullOrWhiteSpace(signature))
                return true;

            var headerJson = Base64UrlDecode(header);
            return headerJson.Contains("\"alg\":\"none\"") || headerJson.Contains("\"alg\" : \"none\"");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Decodifica un string en formato Base64Url a texto plano.
    /// </summary>
    /// <param name="input">String en Base64Url (con caracteres - y _ en lugar de + y /).</param>
    /// <returns>Texto decodificado en UTF-8.</returns>
    private static string Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        
        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Lee un token JWT usando el handler estándar sin validar la firma.
    /// </summary>
    /// <param name="token">Token JWT a leer.</param>
    /// <returns>Objeto <see cref="JwtSecurityToken"/> parseado, o <c>null</c> si el token es inválido o vacío.</returns>
    private JwtSecurityToken? ReadToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogDebug("Token vacío o nulo");
            return null;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadJwtToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error al parsear token JWT");
            return null;
        }
    }
}