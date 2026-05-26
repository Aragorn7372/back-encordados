using BackEncordados.Usuarios.Dto;
using FluentValidation;

namespace BackEncordados.Usuarios.Validator;

/// <summary>
/// Validador FluentValidation para <see cref="UserRequestDto"/>.
/// </summary>
/// <remarks>
/// <para>Define reglas de validación para los cuatro campos editables del perfil de usuario:</para>
/// <list type="table">
///   <listheader>
///     <term>Campo</term>
///     <description>Regla</description>
///     <description>Mensaje de error</description>
///   </listheader>
///   <item>
///     <term><c>Name</c></term>
///     <description>Longitud mínima 1 carácter</description>
///     <description>"El nombre debe de tener más de un caracter"</description>
///   </item>
///   <item>
///     <term><c>Email</c></term>
///     <description>Formato de email válido</description>
///     <description>"El correo electrónico no es válido"</description>
///   </item>
///   <item>
///     <term><c>Telefono</c></term>
///     <description>Regex de prefijo internacional (opcional)</description>
///     <description>"El teléfono debe ser un número válido con prefijo internacional"</description>
///   </item>
///   <item>
///     <term><c>Username</c></term>
///     <description>Longitud mínima 1 carácter</description>
///     <description>"El nombre de usuario debe tener mas de 1 letra"</description>
///   </item>
/// </list>
/// <para>Todos los campos son opcionales en el DTO, pero si se proporcionan deben cumplir las reglas.</para>
/// </remarks>
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