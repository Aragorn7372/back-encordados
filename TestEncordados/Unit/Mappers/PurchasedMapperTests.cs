using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Mapper;
using BackEncordados.Purchased.Model;
using BackEncordados.Usuarios.Dto;
using FluentAssertions;
using TestEncordados.Unit.Fixtures;

namespace TestEncordados.Unit.Mappers;

public class PurchasedMapperTests
{
    private static UserResponseDto CreatePlayerDto() => new("player1", "http://img/player.jpg", "Player One", 0.0);
    private static UserResponseDto CreateEncorderDto() => new("encorder1", "http://img/encorder.jpg", "Encorder One", 0.0);

    [Test]
    public void ToDto_PedidoLinea_ReturnsCorrectDto()
    {
        var linea = PedidoLineaBuilder.Create(
            raquetModel: "Wilson Pro Staff",
            nudos: 4,
            logotype: true,
            color: "Negro",
            status: Status.PENDING);

        var result = linea.ToDto();

        result.Id.Should().Be(linea.Id);
        result.RaquetModel.Should().Be("Wilson Pro Staff");
        result.Nudos.Should().Be((byte)4);
        result.Logotype.Should().BeTrue();
        result.Color.Should().Be("Negro");
        result.Status.Should().Be(Status.PENDING);
        result.StringSetup.Should().BeSameAs(linea.StringSetup);
    }

    [Test]
    public void ToDto_PedidoWithLineas_ReturnsCorrectDto()
    {
        var pedido = PedidosBuilder.Create(
            machine: "Machine-1",
            comments: "Urgent",
            price: 75.0,
            payStatus: PaymentStatus.PAID);
        var linea = PedidoLineaBuilder.Create(pedidoId: pedido.Id);
        pedido.Lineas.Add(linea);

        var result = pedido.ToDto(CreatePlayerDto(), CreateEncorderDto());

        result.Id.Should().Be(pedido.Id);
        result.TournamentId.Should().Be(pedido.TournamentId);
        result.Machine.Should().Be("Machine-1");
        result.Comments.Should().Be("Urgent");
        result.PayStatus.Should().Be("PAID");
        result.Price.Should().Be(75.0);
        result.Lineas.Should().HaveCount(1);
    }

    [Test]
    public void ToEntity_ValidDto_ReturnsPedidosWithLineas()
    {
        var playerId = Ulid.NewUlid();
        var encorderId = Ulid.NewUlid();
        var tournamentId = Ulid.NewUlid();
        var dto = new PurchasedRequestDto
        {
            TournamentId = tournamentId,
            PlayerName = "player1",
            AssignedToName = "encorder1",
            Machine = "Machine-A",
            Comments = "Test comment",
            PayStatus = "PENDING_PAYMENT",
            Price = 50.0,
            Lineas = new List<PedidoLineaRequestDto>
            {
                new()
                {
                    RaquetModel = "Babolat Pure Aero",
                    Nudos = 4,
                    DateString = DateTime.UtcNow.AddDays(3),
                    Logotype = true,
                    Color = "Azul",
                    StringSetup = new StringSetupDto
                    {
                        StringV = "RPM Blast",
                        TensionV = 25.0,
                        PreStetchV = 10,
                        StringH = "VS Touch",
                        TensionH = 23.0,
                        PreStetchH = 5
                    }
                }
            }
        };

        var result = dto.ToEntity(playerId, encorderId);

        result.TournamentId.Should().Be(tournamentId);
        result.PlayerId.Should().Be(playerId);
        result.AssignedTo.Should().Be(encorderId);
        result.Machine.Should().Be("Machine-A");
        result.Comments.Should().Be("Test comment");
        result.PayStatus.Should().Be(PaymentStatus.PENDING_PAYMENT);
        result.Price.Should().Be(50.0);
        result.Lineas.Should().HaveCount(1);
        result.Lineas.First().PedidoId.Should().Be(result.Id);
    }

    [Test]
    public void ToEntity_MultipleLineas_CreatesAllLineas()
    {
        var playerId = Ulid.NewUlid();
        var encorderId = Ulid.NewUlid();
        var dto = new PurchasedRequestDto
        {
            TournamentId = Ulid.NewUlid(),
            PlayerName = "player",
            AssignedToName = "encorder",
            Machine = "M1",
            Comments = "",
            PayStatus = "PAID",
            Price = 100.0,
            Lineas = new List<PedidoLineaRequestDto>
            {
                CreateLineaDto("Racket1"),
                CreateLineaDto("Racket2"),
                CreateLineaDto("Racket3")
            }
        };

        var result = dto.ToEntity(playerId, encorderId);

        result.Lineas.Should().HaveCount(3);
        result.Lineas.Select(l => l.RaquetModel).Should().ContainInOrder("Racket1", "Racket2", "Racket3");
    }

    [Test]
    public void ToEntity_AllPayStatuses_ParsesCorrectly()
    {
        var playerId = Ulid.NewUlid();
        var encorderId = Ulid.NewUlid();
        var statuses = new[] { "PENDING_PAYMENT", "PAID", "CANCELED", "FINNISH_TOURNAMENT" };

        foreach (var status in statuses)
        {
            var dto = new PurchasedRequestDto
            {
                TournamentId = Ulid.NewUlid(),
                PlayerName = "p",
                AssignedToName = "e",
                Machine = "M",
                PayStatus = status,
                Price = 10.0,
                Lineas = new List<PedidoLineaRequestDto> { CreateLineaDto("R") }
            };

            var result = dto.ToEntity(playerId, encorderId);
            result.PayStatus.Should().Be(Enum.Parse<PaymentStatus>(status, true));
        }
    }

    [Test]
    public void ToEntity_PedidoLineaDto_CreatesCorrectLinea()
    {
        var pedidoId = Ulid.NewUlid();
        var dto = new PedidoLineaRequestDto
        {
            RaquetModel = "Head Graphene 360+",
            Nudos = 2,
            DateString = new DateTime(2025, 6, 15),
            Logotype = false,
            Color = "Rojo",
            StringSetup = new StringSetupDto
            {
                StringV = "Synthetic Gut",
                TensionV = 22.0,
                PreStetchV = 5,
                StringH = "",
                TensionH = 0,
                PreStetchH = 0
            }
        };

        var result = dto.ToEntity(pedidoId);

        result.PedidoId.Should().Be(pedidoId);
        result.RaquetModel.Should().Be("Head Graphene 360+");
        result.Nudos.Should().Be((byte)2);
        result.DateString.Should().Be(new DateTime(2025, 6, 15));
        result.Logotype.Should().BeFalse();
        result.Color.Should().Be("Rojo");
        result.Status.Should().Be(Status.PENDING);
        result.StringSetup.StringV.Should().Be("Synthetic Gut");
        result.StringSetup.TensionV.Should().Be(22.0);
    }

[Test]
    public void ToEntity_PedidoLineaPatchDto_UpdatesOnlyProvidedFields()
    {
        var existing = PedidoLineaBuilder.Create(
            raquetModel: "Old Model",
            nudos: 4,
            color: "Old Color",
            status: Status.PENDING);

        var patch = new PedidoLineaPatchDto
        {
            RaquetModel = "New Model",
            Nudos = 2,
            Color = "New Color"
        };

        var result = patch.ToEntity(existing);

        result.RaquetModel.Should().Be("New Model");
        result.Nudos.Should().Be((byte)2);
        result.Color.Should().Be("New Color");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void ToEntity_PedidoLineaPatchDto_PartialUpdate_PreservesOtherFields()
    {
        var originalDate = new DateTime(2025, 1, 1);
        var existing = PedidoLineaBuilder.Create(
            raquetModel: "Original",
            nudos: 4,
            dateString: originalDate,
            logotype: true,
            color: "Original",
            status: Status.COMPLETED);

        var patch = new PedidoLineaPatchDto { RaquetModel = "Updated" };

        var result = patch.ToEntity(existing);

        result.RaquetModel.Should().Be("Updated");
        result.Nudos.Should().Be((byte)4);
        result.DateString.Should().Be(originalDate);
        result.Logotype.Should().BeTrue();
        result.Color.Should().Be("Original");
        result.Status.Should().Be(Status.COMPLETED);
    }

    [Test]
    public void ToEntity_PedidoLineaPatchDto_StatusUpdate_ParsesCorrectly()
    {
        var existing = PedidoLineaBuilder.Create(status: Status.PENDING);
        var patch = new PedidoLineaPatchDto { Status = "DELIVERED_TOpLAYER" };

        var result = patch.ToEntity(existing);

        result.Status.Should().Be(Status.DELIVERED_TOpLAYER);
    }

    [Test]
    public void ToModel_StringSetupDto_ReturnsCorrectModel()
    {
        var dto = new StringSetupDto
        {
            StringV = "Luxilon ALU Power",
            TensionV = 27.5,
            PreStetchV = 15,
            StringH = "Luxilon Big Banger",
            TensionH = 25.0,
            PreStetchH = 10
        };

        var result = dto.ToModel();

        result.StringV.Should().Be("Luxilon ALU Power");
        result.TensionV.Should().Be(27.5);
        result.PreStetchV.Should().Be((short)15);
        result.StringH.Should().Be("Luxilon Big Banger");
        result.TensionH.Should().Be(25.0);
        result.PreStetchH.Should().Be((short)10);
    }

    private static PedidoLineaRequestDto CreateLineaDto(string model) => new()
    {
        RaquetModel = model,
        Nudos = 4,
        DateString = DateTime.UtcNow.AddDays(7),
        Logotype = false,
        Color = "Black",
        StringSetup = new StringSetupDto
        {
            StringV = "Synthetic",
            TensionV = 20.0,
            PreStetchV = 10
        }
    };
}