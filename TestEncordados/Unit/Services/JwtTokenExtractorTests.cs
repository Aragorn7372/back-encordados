using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BackEncordados.Usuarios.Service.Auth;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace TestEncordados.Unit.Services;

public class JwtTokenExtractorTests
{
    private readonly Mock<ILogger<JwtTokenExtractor>> _mockLogger;
    private readonly JwtTokenExtractor _extractor;

    private static readonly string SecretKey = "this-is-a-test-secret-key-that-is-at-least-32-characters!";

    public JwtTokenExtractorTests()
    {
        _mockLogger = new Mock<ILogger<JwtTokenExtractor>>();
        _extractor = new JwtTokenExtractor(_mockLogger.Object);
    }

    [Test]
    public void ExtractUserId_WhenTokenHasNameIdentifierClaim_ReturnsUserId()
    {
        var token = GenerateToken(claims: [new Claim(ClaimTypes.NameIdentifier, "42")]);

        var userId = _extractor.ExtractUserId(token);

        userId.Should().Be(42);
    }

    [Test]
    public void ExtractUserId_WhenTokenHasNameidClaim_ReturnsUserId()
    {
        var token = GenerateToken(claims: [new Claim("nameid", "99")]);

        var userId = _extractor.ExtractUserId(token);

        userId.Should().Be(99);
    }

    [Test]
    public void ExtractUserId_WhenTokenHasSubClaim_ReturnsUserId()
    {
        var token = GenerateToken(claims: [new Claim(JwtRegisteredClaimNames.Sub, "77")]);

        var userId = _extractor.ExtractUserId(token);

        userId.Should().Be(77);
    }

    [Test]
    public void ExtractUserId_WhenTokenHasNoUserIdClaim_ReturnsNull()
    {
        var token = GenerateToken(claims: [new Claim("email", "test@test.com")]);

        var userId = _extractor.ExtractUserId(token);

        userId.Should().BeNull();
    }

    [Test]
    public void ExtractUserId_WhenUserIdNotValidLong_ReturnsNull()
    {
        var token = GenerateToken(claims: [new Claim(ClaimTypes.NameIdentifier, "not-a-number")]);

        var userId = _extractor.ExtractUserId(token);

        userId.Should().BeNull();
    }

    [Test]
    public void ExtractUserId_WhenTokenIsInvalid_ReturnsNull()
    {
        var userId = _extractor.ExtractUserId("invalid-token");

        userId.Should().BeNull();
    }

    [Test]
    public void ExtractUserId_WhenTokenIsEmpty_ReturnsNull()
    {
        var userId = _extractor.ExtractUserId("");

        userId.Should().BeNull();
    }

    [Test]
    public void ExtractRole_WhenTokenHasRoleClaim_ReturnsRole()
    {
        var token = GenerateToken(claims: [new Claim(ClaimTypes.Role, "ADMIN")]);

        var role = _extractor.ExtractRole(token);

        role.Should().Be("ADMIN");
    }

    [Test]
    public void ExtractRole_WhenTokenHasRoleClaimLowerCase_ReturnsRole()
    {
        var token = GenerateToken(claims: [new Claim("role", "USER")]);

        var role = _extractor.ExtractRole(token);

        role.Should().Be("USER");
    }

    [Test]
    public void ExtractRole_WhenTokenHasNoRole_ReturnsNull()
    {
        var token = GenerateToken(claims: [new Claim("email", "test@test.com")]);

        var role = _extractor.ExtractRole(token);

        role.Should().BeNull();
    }

    [Test]
    public void ExtractRole_WhenTokenIsInvalid_ReturnsNull()
    {
        var role = _extractor.ExtractRole("bad.token");

        role.Should().BeNull();
    }

    [Test]
    public void IsAdmin_WhenRoleIsAdmin_ReturnsTrue()
    {
        var token = GenerateToken(claims: [new Claim(ClaimTypes.Role, "admin")]);

        var result = _extractor.IsAdmin(token);

        result.Should().BeTrue();
    }

    [Test]
    public void IsAdmin_WhenRoleIsAdminUpperCase_ReturnsTrue()
    {
        var token = GenerateToken(claims: [new Claim(ClaimTypes.Role, "ADMIN")]);

        var result = _extractor.IsAdmin(token);

        result.Should().BeTrue();
    }

    [Test]
    public void IsAdmin_WhenRoleIsUser_ReturnsFalse()
    {
        var token = GenerateToken(claims: [new Claim(ClaimTypes.Role, "USER")]);

        var result = _extractor.IsAdmin(token);

        result.Should().BeFalse();
    }

    [Test]
    public void IsAdmin_WhenNoRole_ReturnsFalse()
    {
        var token = GenerateToken(claims: [new Claim("email", "test@test.com")]);

        var result = _extractor.IsAdmin(token);

        result.Should().BeFalse();
    }

    [Test]
    public void ExtractUserInfo_ReturnsCorrectTuple()
    {
        var token = GenerateToken(claims:
        [
            new Claim(ClaimTypes.NameIdentifier, "55"),
            new Claim(ClaimTypes.Role, "ADMIN")
        ]);

        var (userId, isAdmin, role) = _extractor.ExtractUserInfo(token);

        userId.Should().Be(55);
        isAdmin.Should().BeTrue();
        role.Should().Be("ADMIN");
    }

    [Test]
    public void ExtractUserInfo_WhenNoUserId_ReturnsNullAndFalse()
    {
        var token = GenerateToken(claims: [new Claim(ClaimTypes.Role, "USER")]);

        var (userId, isAdmin, role) = _extractor.ExtractUserInfo(token);

        userId.Should().BeNull();
        isAdmin.Should().BeFalse();
        role.Should().Be("USER");
    }

    [Test]
    public void ExtractClaims_WhenValidToken_ReturnsClaimsPrincipal()
    {
        var token = GenerateToken(claims:
        [
            new Claim(ClaimTypes.NameIdentifier, "123"),
            new Claim(ClaimTypes.Role, "ADMIN"),
            new Claim(ClaimTypes.Email, "admin@test.com"),
            new Claim(ClaimTypes.Name, "Admin User")
        ]);

        var principal = _extractor.ExtractClaims(token);

        principal.Should().NotBeNull();
        principal!.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "123");
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "ADMIN");
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "admin@test.com");
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "Admin User");
    }

    [Test]
    public void ExtractClaims_WhenTokenWithNonStandardClaims_NormalizesClaimTypes()
    {
        var token = GenerateToken(claims:
        [
            new Claim("nameid", "456"),
            new Claim("role", "USER"),
            new Claim("email", "user@test.com"),
            new Claim("name", "Regular User")
        ]);

        var principal = _extractor.ExtractClaims(token);

        principal.Should().NotBeNull();
        principal!.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "456");
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "USER");
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "user@test.com");
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "Regular User");
    }

    [Test]
    public void ExtractClaims_WhenTokenWithRolesClaim_NormalizesToRole()
    {
        var token = GenerateToken(claims: [new Claim("roles", "SUPERVISOR")]);

        var principal = _extractor.ExtractClaims(token);

        principal.Should().NotBeNull();
        principal!.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "SUPERVISOR");
    }

    [Test]
    public void ExtractClaims_WhenTokenIsInvalid_ReturnsNull()
    {
        var principal = _extractor.ExtractClaims("invalid.token.here");

        principal.Should().BeNull();
    }

    [Test]
    public void ExtractClaims_WhenTokenHasNoClaims_ReturnsClaimsPrincipalWithNoClaims()
    {
        var principal = _extractor.ExtractClaims("eyJhbGciOiJub25lIn0.e30.");

        principal.Should().NotBeNull();
        principal!.Claims.Should().BeEmpty();
    }

    [Test]
    public void ExtractEmail_WhenTokenHasEmailClaim_ReturnsEmail()
    {
        var token = GenerateToken(claims: [new Claim(ClaimTypes.Email, "test@example.com")]);

        var email = _extractor.ExtractEmail(token);

        email.Should().Be("test@example.com");
    }

    [Test]
    public void ExtractEmail_WhenTokenHasRegisteredEmailClaim_ReturnsEmail()
    {
        var token = GenerateToken(claims: [new Claim(JwtRegisteredClaimNames.Email, "user@test.com")]);

        var email = _extractor.ExtractEmail(token);

        email.Should().Be("user@test.com");
    }

    [Test]
    public void ExtractEmail_WhenTokenHasNoEmail_ReturnsNull()
    {
        var token = GenerateToken(claims: [new Claim(ClaimTypes.Role, "USER")]);

        var email = _extractor.ExtractEmail(token);

        email.Should().BeNull();
    }

    [Test]
    public void ExtractEmail_WhenTokenIsInvalid_ReturnsNull()
    {
        var email = _extractor.ExtractEmail("bad");

        email.Should().BeNull();
    }

    [Test]
    public void IsValidTokenFormat_WithThreeParts_ReturnsTrue()
    {
        var token = GenerateToken();

        var result = _extractor.IsValidTokenFormat(token);

        result.Should().BeTrue();
    }

    [Test]
    public void IsValidTokenFormat_WithLessThanThreeParts_ReturnsFalse()
    {
        var result = _extractor.IsValidTokenFormat("header.payload");

        result.Should().BeFalse();
    }

    [Test]
    public void IsValidTokenFormat_WithEmptyString_ReturnsFalse()
    {
        var result = _extractor.IsValidTokenFormat("");

        result.Should().BeFalse();
    }

    [Test]
    public void IsValidTokenFormat_WithNull_ReturnsFalse()
    {
        var result = _extractor.IsValidTokenFormat(null!);

        result.Should().BeFalse();
    }

    [Test]
    public void IsValidTokenFormat_WithAlgNone_ReturnsTrue()
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"alg":"none"}"""))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"sub":"123"}"""))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var token = $"{header}.{payload}.";

        var result = _extractor.IsValidTokenFormat(token);

        result.Should().BeTrue();
    }

    [Test]
    public void IsValidTokenFormat_WithAlgNoneWithSpaces_ReturnsTrue()
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"alg" : "none"}"""))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"sub":"123"}"""))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var token = $"{header}.{payload}.";

        var result = _extractor.IsValidTokenFormat(token);

        result.Should().BeTrue();
    }

    [Test]
    public void IsValidTokenFormat_WithEmptyHeader_ReturnsFalse()
    {
        var token = ".payload.signature";

        var result = _extractor.IsValidTokenFormat(token);

        result.Should().BeFalse();
    }

    [Test]
    public void IsValidTokenFormat_WithEmptyPayload_ReturnsFalse()
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"alg":"HS256"}"""))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var token = $"{header}..signature";

        var result = _extractor.IsValidTokenFormat(token);

        result.Should().BeFalse();
    }

    [Test]
    public void IsValidTokenFormat_WhenAlgNoneNotDetectedBecauseInvalidJson_ReturnsFalse()
    {
        var header = "!!!not-json!!!";
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"sub":"123"}"""))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var token = $"{header}.{payload}.";

        var result = _extractor.IsValidTokenFormat(token);

        result.Should().BeFalse();
    }

    private static string GenerateToken(IEnumerable<Claim>? claims = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims ?? [new Claim("sub", "1")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
