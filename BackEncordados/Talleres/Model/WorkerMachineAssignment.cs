namespace BackEncordados.Talleres.Model;

/// <summary>
/// Registro que representa la asignación de un trabajador a una máquina específica dentro de un torneo.
/// </summary>
/// <remarks>
/// <para>Esta entidad se almacena como colección OwnsMany dentro de <see cref="Tournaments.WorkerMachineAssignments"/>.</para>
/// <para><see cref="Id"/> es un identificador secuencial (auto-incrementado por lógica de aplicación) dentro del torneo.</para>
/// <para><see cref="WorkerId"/> referencia al usuario (rol WORKER) asignado a la máquina.</para>
/// </remarks>
public record WorkerMachineAssignment
{
    /// <summary>Identificador secuencial único dentro del torneo (asignado por lógica de aplicación: maxId + 1).</summary>
    public long Id { get; set; }

    /// <summary>ULID del usuario trabajador asignado a esta máquina.</summary>
    public Ulid WorkerId { get; set; }

    /// <summary>Nombre o identificador de la máquina asignada.</summary>
    public string MachineName { get; set; } = string.Empty;
}