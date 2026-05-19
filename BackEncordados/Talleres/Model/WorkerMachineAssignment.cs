namespace BackEncordados.Talleres.Model;

public record WorkerMachineAssignment
{
    public long Id { get; set; }

    public Ulid WorkerId { get; set; }

    public string MachineName { get; set; } = string.Empty;
}