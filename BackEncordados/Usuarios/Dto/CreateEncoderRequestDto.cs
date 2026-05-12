using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Usuarios.Dto;

public class CreateEncoderRequestDto
{
    [Required]
    public Ulid UserId { get; set; }
}