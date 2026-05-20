using BackEncordados.Purchased.Model;

namespace TestEncordados.Unit.Fixtures;

public static class PedidoLineaBuilder
{
    public static PedidoLinea Create(
        Ulid? id = null,
        Ulid? pedidoId = null,
        string raquetModel = "Wilson Pro Staff",
        byte nudos = 4,
        DateTime? dateString = null,
        bool logotype = true,
        string color = "Negro",
        Status status = Status.PENDING,
        StringSetup? stringSetup = null)
    {
        return new PedidoLinea
        {
            Id = id ?? Ulid.NewUlid(),
            PedidoId = pedidoId ?? Ulid.NewUlid(),
            RaquetModel = raquetModel,
            Nudos = nudos,
            DateString = dateString ?? DateTime.UtcNow.AddDays(7),
            Logotype = logotype,
            Color = color,
            Status = status,
            StringSetup = stringSetup ?? StringSetupBuilder.Create()
        };
    }

    public static PedidoLinea WithNudos(byte nudos, Ulid? pedidoId = null) =>
        Create(pedidoId: pedidoId, nudos: nudos);

    public static PedidoLinea Completed(Ulid? id = null, Ulid? pedidoId = null) =>
        Create(id: id, pedidoId: pedidoId, status: Status.COMPLETED);

    public static PedidoLinea Pending(Ulid? id = null, Ulid? pedidoId = null) =>
        Create(id: id, pedidoId: pedidoId, status: Status.PENDING);
}