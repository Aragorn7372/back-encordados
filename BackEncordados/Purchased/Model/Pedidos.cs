using BackEncordados.Common.Database.Helpers;

namespace BackEncordados.Purchased.Model;

public class Pedidos: ITimestamped
{
    public Ulid Id { get; set; }
    public long TournamentId { get; set; }
    public Ulid PlayerId { get; set; }
    public Ulid AssignedTo { get; set; }
    public string Machine { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public PaymentStatus PayStatus { get; set; } = PaymentStatus.PENDING_PAYMENT;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<PedidoLinea> Lineas { get; set; } = new List<PedidoLinea>();
}