using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Talleres.Dto;

/// <summary>
/// DTO de solicitud para asignar un trabajador a una máquina en un torneo.
/// </summary>
/// <remarks>
/// <para>La asignación vincula un usuario (worker) con una máquina específica para un torneo.
/// El <see cref="UserId"/> se proporciona como string (ULID) y el <see cref="MachineName"/>
/// debe tener entre 1 y 50 caracteres.</para>
/// </remarks>
public class WorkerMachineAssignmentRequestDto
{
    /// <summary>Identificador del usuario/trabajador (ULID en formato string).</summary>
    [Required]
    public string UserId { get; set; }=string.Empty;
    /// <summary>Nombre de la máquina asignada. Entre 1 y 50 caracteres.</summary>
    [Required]
    [MaxLength(50)]
    [MinLength(1)]
    public string MachineName { get; set; }=string.Empty;
}