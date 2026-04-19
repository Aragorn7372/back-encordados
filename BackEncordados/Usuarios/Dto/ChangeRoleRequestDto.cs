using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Usuarios.Dto;

public record ChangeRoleRequestDto
{
    public Guid UserId { get; set; }
    [Required]
    [RegularExpression("ADMIN|USER|OWNER|ENCORDER")]
    public String RoleName { get; init; }
}