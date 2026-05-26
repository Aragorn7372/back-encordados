using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Usuarios.Dto;

/// <summary>
/// DTO de solicitud para cambiar la contraseña de un usuario.
/// </summary>
/// <remarks>
/// <para>Ambos campos son obligatorios y deben coincidir para que el cambio sea válido.</para>
/// <para>El servicio valida que la nueva contraseña cumpla con los requisitos de seguridad
/// antes de aplicar el cambio en la base de datos.</para>
/// </remarks>
public class ChangePasswordRequestDto {
    /// <summary>Nueva contraseña deseada. No debe estar vacía.</summary>
    [Required]
    public string NewPassword { get; init; }=string.Empty;
    /// <summary>Confirmación de la nueva contraseña. Debe coincidir con <see cref="NewPassword"/>.</summary>
    [Required]
    public string ConfirmPassword { get; init; }=string.Empty;
}