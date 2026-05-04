using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Talleres.Dto;

public class WorkerMachineAssignmentRequestDto
{
    [Required]
    public string UserId { get; set; }=string.Empty;
    [Required]
    [MaxLength(50)]
    [MinLength(1)]
    public string MachineName { get; set; }=string.Empty;
}