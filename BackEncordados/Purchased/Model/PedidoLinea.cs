using BackEncordados.Common.Database.Helpers;

namespace BackEncordados.Purchased.Model;

public class PedidoLinea: ITimestamped
{
    public Ulid Id { get; set; }
    public Ulid PedidoId { get; set; }
    public string RaquetModel { get; set; } = string.Empty;
    public byte Nudos { get; set; }
    public DateTime DateString { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool Logotype { get; set; }
    public string Color { get; set; } = string.Empty;
    public StringSetup StringSetup { get; set; } = null!;
    public Status Status { get; set; } = Status.PENDING;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Pedidos Pedido { get; set; } = null!;
}