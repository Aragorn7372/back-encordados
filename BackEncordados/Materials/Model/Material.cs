using BackEncordados.Common.Database.Helpers;

namespace BackEncordados.Materials.Model;

public class Material : ITimestamped
{
    public long Id { get; set; }
    public Ulid TournamentId { get; set; }
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Stock { get; set; }
    public double Precio { get; set; }
    public MaterialType Type { get; set; } = MaterialType.Grip;

    /// <summary>Fecha de creación en UTC.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Fecha de última modificación en UTC.</summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    
    public bool IsDeleted { get; set; }=false;
}