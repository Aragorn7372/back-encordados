using BackEncordados.Usuarios.Dto;
using FluentValidation;

namespace BackEncordados.Usuarios.Validator;

public class UserRequestDtoValidator : AbstractValidator<UserRequestDto>
{
    public UserRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(1).WithMessage("El nombre debe de tener más de un caracter");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El correo electrónico no es válido");

        RuleFor(x => x.Telefono)
            .Must(telefono => string.IsNullOrEmpty(telefono) || System.Text.RegularExpressions.Regex.IsMatch(telefono, @"^[1-9]\d{6,14}$"))
            .WithMessage("El teléfono debe ser un número válido con prefijo internacional (ej: 34612345678, 15551234567)");

        RuleFor(x => x.Username)
            .MinimumLength(1).WithMessage("El nombre de usuario debe tener mas de 1 letra");
    }
}