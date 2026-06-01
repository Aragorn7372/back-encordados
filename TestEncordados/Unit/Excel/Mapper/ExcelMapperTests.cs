using BackEncordados.Excel.Dto;
using BackEncordados.Excel.Mapper;
using BackEncordados.Materials.Model;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Model;
using FluentAssertions;

namespace TestEncordados.Unit.Excel.Mapper;

public class ExcelMapperTests
{
    [Test]
    public void ToExcelUsersDto_MapsAllProperties()
    {
        var user = new User
        {
            Id = Ulid.NewUlid(),
            Username = "testuser",
            Name = "Test User",
            Email = "test@test.com",
            Phone = "123456789",
            TournamentId = Ulid.NewUlid()
        };

        var dto = user.ToExcelUsersDto();

        dto.Id.Should().Be(user.Id.ToString());
        dto.Username.Should().Be("testuser");
        dto.Name.Should().Be("Test User");
        dto.Email.Should().Be("test@test.com");
        dto.Phone.Should().Be("123456789");
        dto.TournamentId.Should().Be(user.TournamentId.ToString());
    }

    [Test]
    public void ToExcelUsersDto_WhenTournamentIdNull_SetsNull()
    {
        var user = new User
        {
            Id = Ulid.NewUlid(),
            Username = "test",
            Name = "Test",
            Email = "t@t.com",
            TournamentId = null
        };

        var dto = user.ToExcelUsersDto();

        dto.TournamentId.Should().BeNull();
    }

    [Test]
    public void ToExcelUsersDto_WhenPhoneNull_SetsNull()
    {
        var user = new User
        {
            Id = Ulid.NewUlid(),
            Username = "test",
            Name = "Test",
            Email = "t@t.com",
            Phone = null
        };

        var dto = user.ToExcelUsersDto();

        dto.Phone.Should().BeNull();
    }

    [Test]
    public void ToExcelMaterialsDto_MapsAllProperties()
    {
        var material = new Material
        {
            Id = 42,
            TournamentId = Ulid.NewUlid(),
            Marca = "Wilson",
            Modelo = "Pro Grip",
            Stock = 10,
            Precio = 5.99,
            Type = MaterialType.Grip
        };

        var dto = material.ToExcelMaterialsDto();

        dto.Id.Should().Be(42);
        dto.TournamentId.Should().Be(material.TournamentId.ToString());
        dto.Marca.Should().Be("Wilson");
        dto.Modelo.Should().Be("Pro Grip");
        dto.Stock.Should().Be(10);
        dto.Precio.Should().Be(5.99);
        dto.Type.Should().Be("Grip");
    }

    [Test]
    public void ToExcelMaterialsDto_MapsDifferentMaterialType()
    {
        var material = new Material
        {
            Id = 1,
            TournamentId = Ulid.NewUlid(),
            Marca = "Babolat",
            Modelo = "Overgrip",
            Stock = 20,
            Precio = 3.50,
            Type = MaterialType.Overgrip
        };

        var dto = material.ToExcelMaterialsDto();

        dto.Type.Should().Be("Overgrip");
    }

    [Test]
    public void ToExcelCuerdasDto_MapsAllProperties()
    {
        var cuerda = new Cuerdas
        {
            Id = 7,
            TournamentId = Ulid.NewUlid(),
            Marca = "Luxilon",
            Modelo = "ALU Power",
            Stock = 5,
            Precio = 15.99,
            Calibre = 1.25,
            StringFormat = FormatoCuerda.Reel,
            StringsType = StringsType.Polyester
        };

        var dto = cuerda.ToExcelCuerdasDto();

        dto.Id.Should().Be(7);
        dto.TournamentId.Should().Be(cuerda.TournamentId.ToString());
        dto.Marca.Should().Be("Luxilon");
        dto.Modelo.Should().Be("ALU Power");
        dto.Stock.Should().Be(5);
        dto.Precio.Should().Be(15.99);
        dto.Calibre.Should().Be(1.25);
        dto.StringFormat.Should().Be("Reel");
        dto.StringsType.Should().Be("Polyester");
    }

    [Test]
    public void ToExcelCuerdasDto_MapsDifferentStringTypes()
    {
        var cuerda = new Cuerdas
        {
            Id = 1,
            TournamentId = Ulid.NewUlid(),
            Marca = "Babolat",
            Modelo = "VS Touch",
            Stock = 3,
            Precio = 29.99,
            Calibre = 1.30,
            StringFormat = FormatoCuerda.Set,
            StringsType = StringsType.NaturalGut
        };

        var dto = cuerda.ToExcelCuerdasDto();

        dto.StringFormat.Should().Be("Set");
        dto.StringsType.Should().Be("NaturalGut");
    }

    [Test]
    public void ToExcelTournamentDto_MapsAllProperties()
    {
        var tournament = new Tournaments
        {
            Id = Ulid.NewUlid(),
            Owner = Ulid.NewUlid(),
            Title = "Test Tournament",
            StartTournament = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            EndTournament = new DateTime(2024, 1, 7, 18, 0, 0, DateTimeKind.Utc),
            Logotype = "https://example.com/logo.png",
            WorkersList = new List<Ulid> { Ulid.NewUlid(), Ulid.NewUlid() },
            SupervisorList = new List<Ulid> { Ulid.NewUlid(), Ulid.NewUlid() }
        };

        var dto = tournament.ToExcelTournamentDto();

        dto.Id.Should().Be(tournament.Id.ToString());
        dto.Owner.Should().Be(tournament.Owner.ToString());
        dto.Title.Should().Be("Test Tournament");
        dto.StartTournament.Should().Be(new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        dto.EndTournament.Should().Be(new DateTime(2024, 1, 7, 18, 0, 0, DateTimeKind.Utc));
        dto.Logotype.Should().Be("https://example.com/logo.png");
        dto.WorkersList.Should().Contain(";");
        dto.SupervisorList.Should().Contain(";");
    }

    [Test]
    public void ToExcelTournamentDto_WhenListsEmpty_ReturnsEmptyStrings()
    {
        var tournament = new Tournaments
        {
            Id = Ulid.NewUlid(),
            Owner = Ulid.NewUlid(),
            Title = "Empty Lists",
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow,
            Logotype = "",
            WorkersList = new List<Ulid>(),
            SupervisorList = new List<Ulid>()
        };

        var dto = tournament.ToExcelTournamentDto();

        dto.WorkersList.Should().BeEmpty();
        dto.SupervisorList.Should().BeEmpty();
    }

    [Test]
    public void ToExcelPedidosDto_MapsAllProperties()
    {
        var pedido = new Pedidos
        {
            Id = Ulid.NewUlid(),
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            Machine = "Machine-1",
            Comments = "Urgent",
            Price = 25.50,
            PayStatus = PaymentStatus.PAID
        };

        var dto = pedido.ToExcelPedidosDto();

        dto.Id.Should().Be(pedido.Id.ToString());
        dto.TournamentId.Should().Be(pedido.TournamentId.ToString());
        dto.PlayerId.Should().Be(pedido.PlayerId.ToString());
        dto.AssignedTo.Should().Be(pedido.AssignedTo.ToString());
        dto.Machine.Should().Be("Machine-1");
        dto.Comments.Should().Be("Urgent");
        dto.Price.Should().Be(25.50);
        dto.PayStatus.Should().Be("PAID");
    }

    [Test]
    public void ToExcelPedidosDto_MapsDifferentPaymentStatus()
    {
        var pedido = new Pedidos
        {
            Id = Ulid.NewUlid(),
            TournamentId = Ulid.NewUlid(),
            PlayerId = Ulid.NewUlid(),
            AssignedTo = Ulid.NewUlid(),
            PayStatus = PaymentStatus.PENDING_PAYMENT
        };

        var dto = pedido.ToExcelPedidosDto();

        dto.PayStatus.Should().Be("PENDING_PAYMENT");
    }

    [Test]
    public void ToExcelPedidoLineasDto_MapsAllProperties()
    {
        var linea = new PedidoLinea
        {
            Id = Ulid.NewUlid(),
            PedidoId = Ulid.NewUlid(),
            RaquetModel = "Pure Drive",
            Nudos = 2,
            DateString = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            Logotype = true,
            Color = "Red",
            StringSetup = new StringSetup
            {
                StringV = "ALU Power",
                TensionV = 23.5,
                PreStetchV = 10,
                StringH = "VS Touch",
                TensionH = 22.0,
                PreStetchH = 5
            },
            Status = Status.IN_PROGRESS
        };

        var dto = linea.ToExcelPedidoLineasDto();

        dto.Id.Should().Be(linea.Id.ToString());
        dto.PedidoId.Should().Be(linea.PedidoId.ToString());
        dto.RaquetModel.Should().Be("Pure Drive");
        dto.Nudos.Should().Be(2);
        dto.Logotype.Should().BeTrue();
        dto.Color.Should().Be("Red");
        dto.StringV.Should().Be("ALU Power");
        dto.TensionV.Should().Be(23.5);
        dto.PreStetchV.Should().Be(10);
        dto.StringH.Should().Be("VS Touch");
        dto.TensionH.Should().Be(22.0);
        dto.PreStetchH.Should().Be(5);
        dto.Status.Should().Be("IN_PROGRESS");
    }

    [Test]
    public void ToExcelPedidoLineasDto_WhenStringSetupNull_UsesDefaults()
    {
        var linea = new PedidoLinea
        {
            Id = Ulid.NewUlid(),
            PedidoId = Ulid.NewUlid(),
            RaquetModel = "Aero",
            Nudos = 2,
            DateString = DateTime.UtcNow,
            Color = "Black",
            StringSetup = null!,
            Status = Status.PENDING
        };

        var dto = linea.ToExcelPedidoLineasDto();

        dto.StringV.Should().BeEmpty();
        dto.TensionV.Should().Be(0);
        dto.PreStetchV.Should().Be(0);
        dto.StringH.Should().BeEmpty();
        dto.TensionH.Should().Be(0);
        dto.PreStetchH.Should().Be(0);
    }

    [Test]
    public void ToExcelPedidoLineasDto_MapsStatusForAllStatuses()
    {
        foreach (Status status in Enum.GetValues<Status>())
        {
            var linea = new PedidoLinea
            {
                Id = Ulid.NewUlid(),
                PedidoId = Ulid.NewUlid(),
                DateString = DateTime.UtcNow,
                Status = status
            };

            var dto = linea.ToExcelPedidoLineasDto();

            dto.Status.Should().Be(status.ToString());
        }
    }

    [Test]
    public void ToTournamentExcelRowDto_MapsGroupAndUser()
    {
        var playerId = Ulid.NewUlid();
        var pedidos = new List<Pedidos>
        {
            new() { PlayerId = playerId, Price = 10.0, TournamentId = Ulid.NewUlid() },
            new() { PlayerId = playerId, Price = 20.0, TournamentId = Ulid.NewUlid() },
            new() { PlayerId = Ulid.NewUlid(), Price = 30.0, TournamentId = Ulid.NewUlid() }
        };

        var group = pedidos.GroupBy(p => p.PlayerId).First();
        var users = new Dictionary<Ulid, (string Username, string Name)>
        {
            [playerId] = ("johndoe", "John Doe")
        };

        var dto = group.ToTournamentExcelRowDto(users);

        dto.Username.Should().Be("johndoe");
        dto.Name.Should().Be("John Doe");
        dto.RacketCount.Should().Be(2);
        dto.TotalPrice.Should().Be(30.0m);
    }

    [Test]
    public void ToTournamentExcelRowDto_WhenUserNotFound_UsesUnknown()
    {
        var playerId = Ulid.NewUlid();
        var pedidos = new List<Pedidos>
        {
            new() { PlayerId = playerId, Price = 15.0, TournamentId = Ulid.NewUlid() }
        };

        var group = pedidos.GroupBy(p => p.PlayerId).First();
        var users = new Dictionary<Ulid, (string Username, string Name)>();

        var dto = group.ToTournamentExcelRowDto(users);

        dto.Username.Should().Be("Unknown");
        dto.Name.Should().Be("Unknown");
        dto.RacketCount.Should().Be(1);
        dto.TotalPrice.Should().Be(15.0m);
    }
}
