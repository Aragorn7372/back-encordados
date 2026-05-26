using BackEncordados.Usuarios.Dto;
using FluentValidation;

namespace BackEncordados.Usuarios.Validator;

/// <summary>
/// Validador FluentValidation para <see cref="ChangeRoleRequestDto"/>.
/// </summary>
/// <remarks>
/// <para>Valida que el <see cref="ChangeRoleRequestDto.UserId"/> no esté vacío y que el
/// <see cref="ChangeRoleRequestDto.RoleName"/> sea uno de los roles permitidos.</para>
/// <para>Roles válidos: <c>ADMIN</c>, <c>USER</c>, <c>OWNER</c>, <c>ENCORDER</c>.</para>
/// <para>La validación del role name es case-insensitive gracias a <c>ToUpperInvariant()</c>.</para>
/// </remarks>
public class ChangeRoleRequestDtoValidator : AbstractValidator<ChangeRoleRequestDto>
{
    private static readonly string[] ValidRoles = { "ADMIN", "USER", "OWNER", "ENCORDER" };

    public ChangeRoleRequestDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El ID del usuario es obligatorio");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("El nombre del rol es obligatorio")
            .Must(BeValidRole).WithMessage($"El rol debe ser uno de los siguientes: {string.Join(", ", ValidRoles)}");
    }

    /// <summary>Verifica que el nombre del rol esté en la lista de roles válidos (comparación case-insensitive).</summary>
    /// <param name="roleName">Nombre del rol a validar.</param>
    /// <returns><c>true</c> si el rol está en la lista blanca.</returns>
    private static bool BeValidRole(string roleName)
    {
        return ValidRoles.Contains(roleName?.ToUpperInvariant());
    }
}