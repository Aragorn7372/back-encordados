using BackEncordados.Common.Utils;
using BackEncordados.Usuarios.Dto;
using FluentAssertions;

namespace TestEncordados.Unit.Dto;

public class UserDtoTests
{
    [Test]
    public void UserWithIdDto_Constructor_SetsAllProperties()
    {
        var userId = Ulid.NewUlid().ToString();
        var tournamentId = Ulid.NewUlid().ToString();

        var dto = new UserWithIdDto(
            UserId: userId,
            Username: "testuser",
            ImageUrl: "https://example.com/avatar.png",
            Name: "Test User",
            Email: "test@example.com",
            Role: "USER",
            TournamentId: tournamentId,
            Bonos: 50.0
        );

        dto.UserId.Should().Be(userId);
        dto.Username.Should().Be("testuser");
        dto.ImageUrl.Should().Be("https://example.com/avatar.png");
        dto.Name.Should().Be("Test User");
        dto.Email.Should().Be("test@example.com");
        dto.Role.Should().Be("USER");
        dto.TournamentId.Should().Be(tournamentId);
        dto.Bonos.Should().Be(50.0);
    }

    [Test]
    public void UserWithIdDto_DefaultBonos_ReturnsZero()
    {
        var dto = new UserWithIdDto(
            UserId: Ulid.NewUlid().ToString(),
            Username: "testuser",
            ImageUrl: "https://example.com/avatar.png",
            Name: "Test User",
            Email: "test@example.com",
            Role: "USER",
            TournamentId: null
        );

        dto.Bonos.Should().Be(0);
        dto.TournamentId.Should().BeNull();
    }

    [Test]
    public void UserDto_Constructor_SetsAllProperties()
    {
        var id = Ulid.NewUlid();
        var createdAt = DateTime.UtcNow;

        var dto = new UserDto(
            Id: id,
            Username: "juanperez",
            Email: "juan@example.com",
            Role: "USER",
            CreatedAt: createdAt
        );

        dto.Id.Should().Be(id);
        dto.Username.Should().Be("juanperez");
        dto.Email.Should().Be("juan@example.com");
        dto.Role.Should().Be("USER");
        dto.CreatedAt.Should().Be(createdAt);
    }

    [Test]
    public void UserResponseDto_Constructor_SetsAllProperties()
    {
        var dto = new UserResponseDto(
            Username: "testuser",
            ImageUrl: "https://example.com/avatar.png",
            Name: "Test User",
            Bonos: 25.5
        );

        dto.Username.Should().Be("testuser");
        dto.ImageUrl.Should().Be("https://example.com/avatar.png");
        dto.Name.Should().Be("Test User");
        dto.Bonos.Should().Be(25.5);
    }

    [Test]
    public void UserResponseDto_DefaultBonos_ReturnsZero()
    {
        var dto = new UserResponseDto(
            Username: "testuser",
            ImageUrl: "https://example.com/avatar.png",
            Name: "Test User",
            Bonos: 0
        );

        dto.Bonos.Should().Be(0);
    }
}
