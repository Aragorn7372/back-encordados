using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Usuarios.Dto;

public class ChangePasswordRequestDto {
    [Required]
    public string NewPassword { get; init; }=string.Empty;
    [Required]
    public string ConfirmPassword { get; init; }=string.Empty;
}