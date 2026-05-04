namespace BackEncordados.Talleres.Model;

public record WorkerMachineAssignment()
{
    public Guid WorkerId { get; set; }

    public string MachineName { get; set; } = string.Empty;
}