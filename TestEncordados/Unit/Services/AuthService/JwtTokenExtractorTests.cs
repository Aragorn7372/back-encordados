using BackEncordados.Usuarios.Model;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using IJwtTokenExtractorType = BackEncordados.Usuarios.Service.Auth.IJwtTokenExtractor;
using JwtTokenExtractorType = BackEncordados.Usuarios.Service.Auth.JwtTokenExtractor;
using JwtServiceType = BackEncordados.Usuarios.Service.Auth.JwtService;

namespace TestEncordados.Unit.Services.AuthService;

public class JwtTokenExtractorTests
{
    private readonly Mock<ILogger<JwtTokenExtractorType>> _mockLogger;
    private readonly JwtTokenExtractorType _extractor;
    private readonly IConfiguration _config;
    private string _validToken = null!;

    public JwtTokenExtractorTests()
    {
        _mockLogger = new Mock<ILogger<JwtTokenExtractorType>>();
        _extractor = new JwtTokenExtractorType(_mockLogger.Object);

        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "esta_es_una_clave_secreta_muy_segura_para_jwt_que_debe_tener_al_menos_32_caracteres!",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:ExpireMinutes"] = "60"
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    private void SetupValidToken(string username, string role, string email, Ulid userId)
    {
        var mockServiceLogger = new Mock<ILogger<JwtServiceType>>();
        var jwtService = new JwtServiceType(_config, mockServiceLogger.Object);

        var user = new User
        {
            Id = userId,
            Username = username,
            Email = email,
            PasswordHash = "hash",
            Role = role,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _validToken = jwtService.GenerateToken(user);
    }

[Test]
    public void ExtractUserId_ValidToken_ReturnsNullForUlidFormat()
    {
        var userId = Ulid.NewUlid();
        SetupValidToken("testuser", User.UserRoles.USER, "test@test.com", userId);

        var result = _extractor.ExtractUserId(_validToken);

        result.Should().BeNull();
    }

    [Test]
    public void ExtractUserInfo_ValidToken_ReturnsNullForUlidFormat()
    {
        SetupValidToken("testuser", User.UserRoles.ENCORDER, "test@test.com", Ulid.NewUlid());

        var (userId, isAdmin, role) = _extractor.ExtractUserInfo(_validToken);

        userId.Should().BeNull();
        role.Should().Be(User.UserRoles.ENCORDER);
        isAdmin.Should().BeFalse();
    }

    [Test]
    public void ExtractUserId_InvalidToken_ReturnsNull()
    {
        var result = _extractor.ExtractUserId("invalid.token");

        result.Should().BeNull();
    }

    [Test]
    public void ExtractUserId_EmptyToken_ReturnsNull()
    {
        var result = _extractor.ExtractUserId(string.Empty);

        result.Should().BeNull();
    }

    [Test]
    public void ExtractRole_ValidToken_ReturnsRole()
    {
        SetupValidToken("testuser", User.UserRoles.ADMIN, "test@test.com", Ulid.NewUlid());

        var result = _extractor.ExtractRole(_validToken);

        result.Should().Be(User.UserRoles.ADMIN);
    }

    [Test]
    public void ExtractRole_InvalidToken_ReturnsNull()
    {
        var result = _extractor.ExtractRole("invalid.token");

        result.Should().BeNull();
    }

    [Test]
    public void IsAdmin_AdminToken_ReturnsTrue()
    {
        SetupValidToken("adminuser", User.UserRoles.ADMIN, "admin@test.com", Ulid.NewUlid());

        var result = _extractor.IsAdmin(_validToken);

        result.Should().BeTrue();
    }

    [Test]
    public void IsAdmin_NonAdminToken_ReturnsFalse()
    {
        SetupValidToken("normaluser", User.UserRoles.USER, "user@test.com", Ulid.NewUlid());

        var result = _extractor.IsAdmin(_validToken);

        result.Should().BeFalse();
    }

    [Test]
    public void IsAdmin_CaseInsensitive_ReturnsTrue()
    {
        SetupValidToken("adminuser", "Admin", "admin@test.com", Ulid.NewUlid());

        var result = _extractor.IsAdmin(_validToken);

        result.Should().BeTrue();
    }

    [Test]
    public void ExtractUserInfo_ValidToken_ReturnsTuple()
    {
        SetupValidToken("testuser", User.UserRoles.ENCORDER, "test@test.com", Ulid.NewUlid());

        var (userId, isAdmin, role) = _extractor.ExtractUserInfo(_validToken);

        userId.Should().BeNull();
        role.Should().Be(User.UserRoles.ENCORDER);
        isAdmin.Should().BeFalse();
    }

    [Test]
    public void ExtractClaims_ValidToken_ReturnsClaimsPrincipal()
    {
        SetupValidToken("testuser", User.UserRoles.USER, "test@test.com", Ulid.NewUlid());

        var result = _extractor.ExtractClaims(_validToken);

        result.Should().NotBeNull();
        result!.Identity.Should().NotBeNull();
    }

    [Test]
    public void ExtractClaims_InvalidToken_ReturnsNull()
    {
        var result = _extractor.ExtractClaims("invalid.token");

        result.Should().BeNull();
    }

    [Test]
    public void ExtractEmail_ValidToken_ReturnsEmail()
    {
        SetupValidToken("testuser", User.UserRoles.USER, "unique@test.com", Ulid.NewUlid());

        var result = _extractor.ExtractEmail(_validToken);

        result.Should().Be("unique@test.com");
    }

    [Test]
    public void ExtractEmail_InvalidToken_ReturnsNull()
    {
        var result = _extractor.ExtractEmail("invalid.token");

        result.Should().BeNull();
    }

    [Test]
    public void IsValidTokenFormat_ValidToken_ReturnsTrue()
    {
        SetupValidToken("testuser", User.UserRoles.USER, "test@test.com", Ulid.NewUlid());

        var result = _extractor.IsValidTokenFormat(_validToken);

        result.Should().BeTrue();
    }

    [Test]
    public void IsValidTokenFormat_EmptyString_ReturnsFalse()
    {
        var result = _extractor.IsValidTokenFormat(string.Empty);

        result.Should().BeFalse();
    }

    [Test]
    public void IsValidTokenFormat_Whitespace_ReturnsFalse()
    {
        var result = _extractor.IsValidTokenFormat("   ");

        result.Should().BeFalse();
    }

    [Test]
    public void IsValidTokenFormat_InvalidFormat_ReturnsFalse()
    {
        var result = _extractor.IsValidTokenFormat("notavalidjwtformat");

        result.Should().BeFalse();
    }

    [Test]
    public void IsValidTokenFormat_TooFewParts_ReturnsFalse()
    {
        var result = _extractor.IsValidTokenFormat("part1.part2");

        result.Should().BeFalse();
    }

    [Test]
    public void IsValidTokenFormat_MissingParts_ReturnsFalse()
    {
        var result = _extractor.IsValidTokenFormat("part1..part3");

        result.Should().BeFalse();
    }
}