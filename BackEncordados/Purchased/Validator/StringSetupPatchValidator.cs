using BackEncordados.Purchased.Dto;
using FluentValidation;

namespace BackEncordados.Purchased.Validator;

public class StringSetupPatchValidator : AbstractValidator<StringSetupDto>
{
    public StringSetupPatchValidator()
    {
        
        RuleFor(x => x.StringV)
            .NotEmpty().WithMessage("Si actualizas el cordaje, el nombre vertical es obligatorio.")
            .Length(1, 100).WithMessage("El nombre vertical debe tener entre 1 y 100 caracteres.");

        RuleFor(x => x.TensionV)
            .NotEmpty().WithMessage("La tensión vertical es obligatoria.")
            .InclusiveBetween(5.0, 40.0).WithMessage("La tensión vertical debe estar entre 5 y 40kg.");

        RuleFor(x => x.PreStetchV)
            .NotNull().WithMessage("El pre-stretch vertical es obligatorio.")
            .InclusiveBetween((short)0, (short)20).WithMessage("El pre-estirado vertical debe estar entre 0 y 20%.");



        RuleFor(x => x.StringH)
            .MaximumLength(100).WithMessage("El nombre horizontal no puede superar los 100 caracteres.");

        RuleFor(x => x.TensionH)
            .InclusiveBetween(5.0, 40.0)
            .When(x => x.TensionH > 0)
            .WithMessage("La tensión horizontal debe estar entre 5 y 40kg.");

        RuleFor(x => x.PreStetchH)
            .InclusiveBetween((short)0, (short)20)
            .When(x => x.PreStetchH > 0)
            .WithMessage("El pre-estirado horizontal debe estar entre 0 y 20%.");
    }
}