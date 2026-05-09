using BackEncordados.Common.Database.Helpers;

namespace BackEncordados.Talleres.Model;

public record Tournaments: ITimestamped
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Fecha de creación en UTC.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Fecha de última modificación en UTC.</summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public DateTime StartTournament { get; set; }=DateTime.UtcNow;
    public DateTime EndTournament { get; set; }=DateTime.UtcNow;

    public string Logotype { get; set; } = string.Empty;
    public List<Ulid> WorkersList {get; set;} = new ();
    public List<WorkerMachineAssignment> WorkerMachineAssignments { get; set; } = new ();
    public bool IsDeleted { get; set; }=false;
};