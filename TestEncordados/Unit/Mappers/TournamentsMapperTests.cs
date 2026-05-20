using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Mapper;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Dto;
using FluentAssertions;
using Moq;
using TestEncordados.Unit.Fixtures;

namespace TestEncordados.Unit.Mappers;

public class TournamentsMapperTests
{
    private static readonly Mock<ICloudinaryService> MockCloudinary = new();

    static TournamentsMapperTests()
    {
        MockCloudinary.Setup(c => c.ResolveImageUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string url, string folder) => $"resolved://{url}");
    }

    [Test]
    public void ToTournamentResponseDto_ValidTournament_ReturnsCorrectDto()
    {
        var tournament = TournamentBuilder.Create(title: "Roland Garros 2025");
        tournament.Logotype = "rg_logo.png";

        var result = tournament.ToTournamentResponseDto(MockCloudinary.Object);

        result.Id.Should().Be(tournament.Id);
        result.Name.Should().Be("Roland Garros 2025");
        result.StartTournament.Should().Be(tournament.StartTournament);
        result.EndTournament.Should().Be(tournament.EndTournament);
    }

    [Test]
    public void ToTournamentResponseDetailsDto_ValidTournament_ReturnsCorrectDto()
    {
        var tournament = TournamentBuilder.Create(title: "Wimbledon 2025");
        var users = new List<UserResponseDto>
        {
            new("user1", "url1", "Player 1", 0),
            new("user2", "url2", "Player 2", 0)
        };
        var owner = new UserResponseDto("owner1", "url3", "Owner One", 0);
        var supervisors = new List<UserResponseDto> { new("super1", "url4", "Supervisor 1", 0) };

        var result = tournament.ToTournamentResponseDetailsDto(users, owner, supervisors, MockCloudinary.Object);

        result.Id.Should().Be(tournament.Id);
        result.Name.Should().Be("Wimbledon 2025");
        result.User.Should().HaveCount(2);
        result.Supevisors.Should().HaveCount(1);
    }

    [Test]
    public void ToTournaments_TournamentAdminRequestDto_ReturnsCorrectEntity()
    {
        var ownerId = Ulid.NewUlid();
        var dto = new TournamentAdminRequestDto
        {
            Name = "US Open",
            OwnerId = ownerId,
            StartTournament = new DateTime(2025, 8, 1),
            EndTournament = new DateTime(2025, 9, 1)
        };

        var result = dto.ToTournaments("uploaded_file.png", "public_id_123");

        result.Owner.Should().Be(ownerId);
        result.Title.Should().Be("US Open");
        result.StartTournament.Should().Be(new DateTime(2025, 8, 1));
        result.EndTournament.Should().Be(new DateTime(2025, 9, 1));
        result.Logotype.Should().Be("uploaded_file.png");
        result.LogotypePublicId.Should().Be("public_id_123");
    }

    [Test]
    public void ToTournaments_TournamentRequestDto_ReturnsCorrectEntity()
    {
        var ownerId = Ulid.NewUlid();
        var dto = new TournamentRequestDto
        {
            Name = "Australian Open",
            StartTournament = new DateTime(2025, 1, 10),
            EndTournament = new DateTime(2025, 2, 1)
        };

        var result = dto.ToTournaments(ownerId, "ao_logo.jpg", "ao_public_id");

        result.Owner.Should().Be(ownerId);
        result.Title.Should().Be("Australian Open");
        result.StartTournament.Should().Be(new DateTime(2025, 1, 10));
        result.EndTournament.Should().Be(new DateTime(2025, 2, 1));
        result.Logotype.Should().Be("ao_logo.jpg");
        result.LogotypePublicId.Should().Be("ao_public_id");
    }

    [Test]
    public void ToWorkerMachineAssignmentResponseDto_ValidAssignment_ReturnsCorrectDto()
    {
        var userDto = new UserResponseDto("worker1", "url", "Worker One", 0);
        var assignment = new WorkerMachineAssignment { MachineName = "Stringing Machine A" };

        var result = assignment.ToWorkerMachineAssignmentResponseDto(userDto);

        result.MachineName.Should().Be("Stringing Machine A");
        result.User.Should().BeEquivalentTo(userDto);
    }
}