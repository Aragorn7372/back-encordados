using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Usuarios.Dto;

/// <summary>
/// DTO de solicitud para cambiar el rol de un usuario.
/// </summary>
/// <remarks>
/// <para>El rol debe ser uno de los valores permitidos: <c>ADMIN</c>, <c>USER</c>, <c>OWNER</c> o <c>ENCORDER</c>.</para>
/// <para>La validación se realiza mediante <see cref="RegularExpressionAttribute"/> en el servidor.</para>
/// </remarks>
public record ChangeRoleRequestDto
{
    /// <summary>Identificador ULID del usuario cuyo rol se desea modificar.</summary>
    public Ulid UserId { get; set; }

    /// <summary>Nuevo rol asignado. Debe coincidir con la expresión regular <c>ADMIN|USER|OWNER|ENCORDER</c>.</summary>
    [Required]
    [RegularExpression("ADMIN|USER|OWNER|ENCORDER")]
    public string RoleName { get; init; } = null!;
}