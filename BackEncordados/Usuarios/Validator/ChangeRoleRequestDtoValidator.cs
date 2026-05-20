using BackEncordados.Usuarios.Dto;
using FluentValidation;

namespace BackEncordados.Usuarios.Validator;

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

    private static bool BeValidRole(string roleName)
    {
        return ValidRoles.Contains(roleName?.ToUpperInvariant());
    }
}