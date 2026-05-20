using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Mapper;
using BackEncordados.Usuarios.Model;
using FluentAssertions;
using Moq;
using TestEncordados.Unit.Fixtures;

namespace TestEncordados.Unit.Mappers;

public class UserMapperTests
{
    private static readonly Mock<ICloudinaryService> MockCloudinary = new();

    static UserMapperTests()
    {
        MockCloudinary.Setup(c => c.ResolveImageUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string url, string folder) => $"resolved://{url}");
    }

    [Test]
    public void ToDto_ValidUser_ReturnsCorrectDto()
    {
        var user = UserBuilder.Create(
            username: "testuser",
            name: "Test User",
            imageUrl: "original_url",
            bonos: 50.0);

        var result = user.ToDto(MockCloudinary.Object);

        result.Username.Should().Be("testuser");
        result.Name.Should().Be("Test User");
        result.Bonos.Should().Be(50.0);
    }

    [Test]
    public void ToDtoWithId_ValidUser_ReturnsCorrectDto()
    {
        var userId = Ulid.NewUlid();
        var user = UserBuilder.Create(
            id: userId,
            username: "testuser",
            name: "Test User");

        var result = user.ToDtoWithId(MockCloudinary.Object);

        result.UserId.Should().Be(userId.ToString());
        result.Username.Should().Be("testuser");
        result.Name.Should().Be("Test User");
    }

    [Test]
    public void ToModel_ContactoPostRequestDto_ReturnsUserWithDefaults()
    {
        var tournamentId = Ulid.NewUlid();
        var dto = new ContactoPostRequestDto
        {
            Name = "New Contact",
            Email = "contact@example.com",
            Phone = "+1234567890",
            TournamentId = tournamentId
        };

        var result = dto.ToModel();

        result.Name.Should().Be("New Contact");
        result.Email.Should().Be("contact@example.com");
        result.Phone.Should().Be("+1234567890");
        result.TournamentId.Should().Be(tournamentId);
        result.Username.Should().NotBeNullOrEmpty();
        result.Role.Should().Be(User.UserRoles.USER);
        result.PasswordHash.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void ToModel_ContactoPostRequestDto_WithoutEmail_GeneratesGuidEmail()
    {
        var dto = new ContactoPostRequestDto
        {
            Name = "No Email Contact",
            Email = null,
            Phone = "+9876543210",
            TournamentId = Ulid.NewUlid()
        };

        var result = dto.ToModel();

        result.Email.Should().NotBeNullOrEmpty();
        Guid.TryParse(result.Email, out _).Should().BeTrue();
    }

    [Test]
    public void ToModel_AllRoles_CreatesCorrectUser()
    {
        var roles = new[] { User.UserRoles.ADMIN, User.UserRoles.OWNER, User.UserRoles.ENCORDER };

        foreach (var role in roles)
        {
            var dto = new ContactoPostRequestDto
            {
                Name = $"User {role}",
                TournamentId = Ulid.NewUlid()
            };

            var result = dto.ToModel();
            result.Role.Should().Be(User.UserRoles.USER);
        }
    }

    [Test]
    public void ToModel_ContactoPostRequestDto_AlwaysCreatesUserRole()
    {
        var dto = new ContactoPostRequestDto
        {
            Name = "Test Contact",
            TournamentId = Ulid.NewUlid()
        };

        var result = dto.ToModel();

        result.Role.Should().Be(User.UserRoles.USER);
    }
}