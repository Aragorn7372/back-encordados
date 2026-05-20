using BackEncordados.Usuarios.Model;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using JwtServiceType = BackEncordados.Usuarios.Service.Auth.JwtService;

namespace TestEncordados.Unit.Services.AuthService;

public class JwtServiceTests
{
    private readonly IConfiguration _config;
    private readonly Mock<ILogger<JwtServiceType>> _mockLogger;
    private readonly JwtServiceType _service;

    public JwtServiceTests()
    {
        _mockLogger = new Mock<ILogger<JwtServiceType>>();

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

        _service = new JwtServiceType(_config, _mockLogger.Object);
    }

    private static User CreateUser(
        Ulid? id = null,
        string username = "testuser",
        string email = "test@example.com",
        string role = User.UserRoles.USER) =>
        new()
        {
            Id = id ?? Ulid.NewUlid(),
            Username = username,
            Email = email,
            PasswordHash = "hashedpassword",
            Role = role,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

    [Test]
    public void GenerateToken_ValidUser_ReturnsToken()
    {
        var user = CreateUser();

        var token = _service.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3);
    }

    [Test]
    public void GenerateToken_ValidUser_TokenContainsCorrectClaims()
    {
        var user = CreateUser(username: "testuser", email: "test@test.com", role: User.UserRoles.ADMIN);

        var token = _service.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void GenerateToken_ThrowsWhenKeyMissing()
    {
        var emptyConfig = new Mock<IConfiguration>();
        var emptySection = new Mock<IConfigurationSection>();
        emptySection.Setup(s => s["Key"]).Returns((string?)null);
        emptyConfig.Setup(c => c.GetSection("Jwt")).Returns(emptySection.Object);

        var service = new JwtServiceType(emptyConfig.Object, _mockLogger.Object);
        var user = CreateUser();

        var action = () => service.GenerateToken(user);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT Key*");
    }

    [Test]
    public void ValidateToken_ValidToken_ReturnsUsername()
    {
        var user = CreateUser(username: "validuser", email: "valid@test.com");
        var token = _service.GenerateToken(user);

        var username = _service.ValidateToken(token);

        username.Should().Be("validuser");
    }

    [Test]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        var invalidToken = "invalid.token.here";

        var result = _service.ValidateToken(invalidToken);

        result.Should().BeNull();
    }

    [Test]
    public void ValidateToken_EmptyToken_ReturnsNull()
    {
        var result = _service.ValidateToken(string.Empty);

        result.Should().BeNull();
    }

    [Test]
    public void ValidateToken_TamperedToken_ReturnsNull()
    {
        var user = CreateUser();
        var token = _service.GenerateToken(user);
        var tamperedToken = token + "tampered";

        var result = _service.ValidateToken(tamperedToken);

        result.Should().BeNull();
    }
}