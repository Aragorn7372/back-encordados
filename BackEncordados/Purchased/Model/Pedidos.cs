using BackEncordados.Common.Database.Helpers;

namespace BackEncordados.Purchased.Model;

public class Pedidos: ITimestamped
{
    public Ulid Id { get; set; }
    public long TournamentId { get; set; }
    public string TypeString { get; set; } = string.Empty;
    public TypePuchase TypeWork { get; set; }= TypePuchase.ENCORDADO;
    /// <summary>Fecha de creación en UTC.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>Fecha de última modificación en UTC.</summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public DateTime DateString { get; set; }= DateTime.UtcNow.AddDays(7);
    public bool Logotype { get; set; }
    public string RaquetModel { get; set; } = string.Empty;
    public double Price { get; set; }
    public byte Nudos { get; set; }
    public Ulid PlayerId { get; set; }
    public Ulid AssignedTo { get; set; }
    public string Machine { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public PaymentStatus PayStatus { get; set; } = PaymentStatus.PENDING_PAYMENT;
    public Status  Status { get; set; }= Status.PENDING;
    public StringSetup StringSetup { get; set; } = null!;
}