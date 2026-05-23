using System.ComponentModel.DataAnnotations;
using BackEncordados.Excel.Dto;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Dto;
using BackEncordados.Usuarios.Dto;
using FluentAssertions;

namespace TestEncordados.Unit.Common.Dto;

public class ExcelAdvancedRequestDtoTests
{
    [Test]
    public void DefaultConstructor_SetsDefaultTypes()
    {
        var dto = new ExcelAdvancedRequestDto();

        dto.Types.Should().BeEquivalentTo(new List<string> { "users", "materials", "cuerdas", "tournament", "pedidos" });
        dto.Fields.Should().BeNull();
    }

    [Test]
    public void TournamentId_CanBeSet()
    {
        var id = Ulid.NewUlid();
        var dto = new ExcelAdvancedRequestDto { TournamentId = id };

        dto.TournamentId.Should().Be(id);
    }

    [Test]
    public void Types_CanBeReplaced()
    {
        var dto = new ExcelAdvancedRequestDto
        {
            Types = new List<string> { "custom" }
        };

        dto.Types.Should().BeEquivalentTo(new List<string> { "custom" });
    }

    [Test]
    public void Fields_CanBeSet()
    {
        var dto = new ExcelAdvancedRequestDto
        {
            Fields = new Dictionary<string, List<string>>
            {
                ["users"] = new() { "name", "email" }
            }
        };

        dto.Fields.Should().ContainKey("users");
        dto.Fields["users"].Should().BeEquivalentTo(new List<string> { "name", "email" });
    }
}

public class CuerdaResponseDtoTests
{
    [Test]
    public void Constructor_SetsProperties()
    {
        var id = 42L;
        var tournamentId = Ulid.NewUlid();

        var dto = new CuerdaResponseDto(id, tournamentId, "Babolat", "Pure Drive", 10, 199.99, "16x19", "Polyester", "test.jpg");

        dto.Id.Should().Be(id);
        dto.TournamentId.Should().Be(tournamentId);
        dto.Marca.Should().Be("Babolat");
        dto.Modelo.Should().Be("Pure Drive");
        dto.Stock.Should().Be(10);
        dto.Precio.Should().Be(199.99);
        dto.StringFormat.Should().Be("16x19");
        dto.StringsType.Should().Be("Polyester");
        dto.ImageUrl.Should().Be("test.jpg");
    }

    [Test]
    public void Equality_SameValues_AreEqual()
    {
        var tournamentId = Ulid.NewUlid();
        var dto1 = new CuerdaResponseDto(1, tournamentId, "Marca", "Modelo", 5, 99.99, "16x19", "Nylon", "test.jpg");
        var dto2 = new CuerdaResponseDto(1, tournamentId, "Marca", "Modelo", 5, 99.99, "16x19", "Nylon", "test.jpg");

        dto1.Should().Be(dto2);
        (dto1 == dto2).Should().BeTrue();
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var tournamentId = Ulid.NewUlid();
        var dto1 = new CuerdaResponseDto(1, tournamentId, "Marca", "Modelo", 5, 99.99, "16x19", "Nylon", "test.jpg");
        var dto2 = new CuerdaResponseDto(2, tournamentId, "Marca", "Modelo", 5, 99.99, "16x19", "Nylon", "test.jpg");

        dto1.Should().NotBe(dto2);
        (dto1 == dto2).Should().BeFalse();
    }

    [Test]
    public void Deconstruct_ReturnsValuesInOrder()
    {
        var tournamentId = Ulid.NewUlid();
        var dto = new CuerdaResponseDto(42, tournamentId, "Wilson", "Blade", 3, 159.50, "18x20", "Multi", "test.jpg");

        var (id, tid, marca, modelo, stock, precio, format, type, imageUrl) = dto;

        id.Should().Be(42);
        tid.Should().Be(tournamentId);
        marca.Should().Be("Wilson");
        modelo.Should().Be("Blade");
        stock.Should().Be(3);
        precio.Should().Be(159.50);
        format.Should().Be("18x20");
        type.Should().Be("Multi");
        imageUrl.Should().Be("test.jpg");
    }
}

public class PedidoLineaResponseDtoTests
{
    [Test]
    public void Constructor_SetsProperties()
    {
        var id = Ulid.NewUlid();
        var date = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        var dto = new PedidoLineaResponseDto(id, "AeroPro", 2, date, true, "Black", Status.PENDING, new StringSetup());

        dto.Id.Should().Be(id);
        dto.RaquetModel.Should().Be("AeroPro");
        dto.Nudos.Should().Be(2);
        dto.DateString.Should().Be(date);
        dto.Logotype.Should().BeTrue();
        dto.Color.Should().Be("Black");
        dto.Status.Should().Be(Status.PENDING);
        dto.StringSetup.Should().NotBeNull();
    }

    [Test]
    public void Equality_SameValues_AreEqual()
    {
        var id = Ulid.NewUlid();
        var date = DateTime.UtcNow;
        var setup = new StringSetup { StringV = "VS Gut", TensionV = 25 };
        var dto1 = new PedidoLineaResponseDto(id, "Blade", 2, date, false, "White", Status.IN_PROGRESS, setup);
        var dto2 = new PedidoLineaResponseDto(id, "Blade", 2, date, false, "White", Status.IN_PROGRESS, setup);

        dto1.Should().Be(dto2);
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var id = Ulid.NewUlid();
        var date = DateTime.UtcNow;
        var dto1 = new PedidoLineaResponseDto(id, "Blade", 2, date, false, "White", Status.IN_PROGRESS, new StringSetup());
        var dto2 = new PedidoLineaResponseDto(id, "Speed", 2, date, false, "White", Status.IN_PROGRESS, new StringSetup());

        dto1.Should().NotBe(dto2);
    }
}

public class PurchasedResponseDtoTests
{
    [Test]
    public void Constructor_SetsProperties()
    {
        var id = Ulid.NewUlid();
        var tournamentId = Ulid.NewUlid();
        var player = new UserResponseDto("player1", "", "Player", 0);
        var encorder = new UserResponseDto("encorder1", "", "Encorder", 0);
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var lineas = new List<PedidoLineaResponseDto>
        {
            new(Ulid.NewUlid(), "Pro Staff", 2, DateTime.UtcNow, true, "Black", Status.COMPLETED, new StringSetup())
        };

        var dto = new PurchasedResponseDto(id, tournamentId, player, encorder, "Machine1", "Comments",
            "paid", createdAt, updatedAt, 199.99, lineas);

        dto.Id.Should().Be(id);
        dto.TournamentId.Should().Be(tournamentId);
        dto.Player.Should().Be(player);
        dto.Encorder.Should().Be(encorder);
        dto.Machine.Should().Be("Machine1");
        dto.Comments.Should().Be("Comments");
        dto.PayStatus.Should().Be("paid");
        dto.CreatedAt.Should().Be(createdAt);
        dto.UpdatedAt.Should().Be(updatedAt);
        dto.Price.Should().Be(199.99);
        dto.Lineas.Should().BeSameAs(lineas);
    }

    [Test]
    public void Equality_SameValues_AreEqual()
    {
        var id = Ulid.NewUlid();
        var tid = Ulid.NewUlid();
        var player = new UserResponseDto("p", "", "", 0);
        var encorder = new UserResponseDto("e", "", "", 0);
        var now = DateTime.UtcNow;
        var lineas = new List<PedidoLineaResponseDto>();
        var dto1 = new PurchasedResponseDto(id, tid, player, encorder, "M", "", "", now, now, 0, lineas);
        var dto2 = new PurchasedResponseDto(id, tid, player, encorder, "M", "", "", now, now, 0, lineas);

        dto1.Should().Be(dto2);
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var id = Ulid.NewUlid();
        var tid = Ulid.NewUlid();
        var player = new UserResponseDto("p", "", "", 0);
        var encorder = new UserResponseDto("e", "", "", 0);
        var now = DateTime.UtcNow;
        var lineas = new List<PedidoLineaResponseDto>();
        var dto1 = new PurchasedResponseDto(id, tid, player, encorder, "M", "", "", now, now, 0, lineas);
        var dto2 = new PurchasedResponseDto(id, tid, player, encorder, "M2", "", "", now, now, 0, lineas);

        dto1.Should().NotBe(dto2);
    }
}

public class TournamentResponseDetailsDtoTests
{
    [Test]
    public void Constructor_SetsProperties()
    {
        var id = Ulid.NewUlid();
        var owner = new UserResponseDto("owner", "", "Owner", 0);
        var users = new List<UserResponseDto>
        {
            new("user1", "", "User 1", 0),
            new("user2", "", "User 2", 0)
        };
        var supervisors = new List<UserResponseDto>
        {
            new("sup1", "", "Supervisor 1", 0)
        };

        var dto = new TournamentResponseDetailsDto(id, "Torneo Test",
            new DateTime(2025, 5, 1), new DateTime(2025, 5, 5),
            "logo.png", users, owner, supervisors);

        dto.Id.Should().Be(id);
        dto.Name.Should().Be("Torneo Test");
        dto.StartDate.Should().Be(new DateTime(2025, 5, 1));
        dto.EndDate.Should().Be(new DateTime(2025, 5, 5));
        dto.Logotype.Should().Be("logo.png");
        dto.User.Should().BeSameAs(users);
        dto.Owner.Should().Be(owner);
        dto.Supevisors.Should().BeSameAs(supervisors);
    }

    [Test]
    public void Equality_SameValues_AreEqual()
    {
        var id = Ulid.NewUlid();
        var owner = new UserResponseDto("o", "", "", 0);
        var users = new List<UserResponseDto>();
        var supervisors = new List<UserResponseDto>();
        var start = new DateTime(2025, 6, 1);
        var end = new DateTime(2025, 6, 5);
        var dto1 = new TournamentResponseDetailsDto(id, "Test", start, end, "logo.png", users, owner, supervisors);
        var dto2 = new TournamentResponseDetailsDto(id, "Test", start, end, "logo.png", users, owner, supervisors);

        dto1.Should().Be(dto2);
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var id = Ulid.NewUlid();
        var owner = new UserResponseDto("o", "", "", 0);
        var users = new List<UserResponseDto>();
        var supervisors = new List<UserResponseDto>();
        var start = new DateTime(2025, 6, 1);
        var end = new DateTime(2025, 6, 5);
        var dto1 = new TournamentResponseDetailsDto(id, "Test", start, end, "logo.png", users, owner, supervisors);
        var dto2 = new TournamentResponseDetailsDto(id, "Other", start, end, "logo.png", users, owner, supervisors);

        dto1.Should().NotBe(dto2);
    }
}

public class CreateEncoderRequestDtoTests
{
    [Test]
    public void UserId_CanBeSet()
    {
        var id = Ulid.NewUlid();
        var dto = new CreateEncoderRequestDto { UserId = id };

        dto.UserId.Should().Be(id);
    }

    [Test]
    public void DefaultUserId_IsEmpty()
    {
        var dto = new CreateEncoderRequestDto();

        dto.UserId.Should().Be(Ulid.Empty);
    }

    [Test]
    public void RequiredAttribute_WithUserId_IsValid()
    {
        var dto = new CreateEncoderRequestDto { UserId = Ulid.NewUlid() };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }
}
