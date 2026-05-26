using BackEncordados.Purchased.Model;

namespace TestEncordados.Unit.Fixtures;

public static class PedidosBuilder
{
    public static Pedidos Create(
        Ulid? id = null,
        Ulid? tournamentId = null,
        Ulid? playerId = null,
        Ulid? assignedTo = null,
        string machine = "Machine-1",
        string comments = "",
        double price = 50.0,
        PaymentStatus payStatus = PaymentStatus.PENDING_PAYMENT,
        List<PedidoLinea>? lineas = null)
    {
        return new Pedidos
        {
            Id = id ?? Ulid.NewUlid(),
            TournamentId = tournamentId ?? Ulid.NewUlid(),
            PlayerId = playerId ?? Ulid.NewUlid(),
            AssignedTo = assignedTo ?? Ulid.NewUlid(),
            Machine = machine,
            Comments = comments,
            Price = price,
            PayStatus = payStatus,
            Lineas = lineas ?? new List<PedidoLinea>()
        };
    }

    public static Pedidos WithLineas(Ulid? id = null, List<PedidoLinea>? lineas = null) =>
        Create(id: id, lineas: lineas ?? new List<PedidoLinea>());

    public static Pedidos PendingPayment(Ulid? id = null) =>
        Create(id: id, payStatus: PaymentStatus.PENDING_PAYMENT);

    public static Pedidos Paid(Ulid? id = null) =>
        Create(id: id, payStatus: PaymentStatus.PAID);
}