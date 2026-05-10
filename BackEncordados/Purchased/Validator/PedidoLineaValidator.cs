using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using FluentValidation;

namespace BackEncordados.Purchased.Validator;

public class PedidoLineaRequestValidator : AbstractValidator<PedidoLineaRequestDto>
{
    public PedidoLineaRequestValidator()
    {
        RuleFor(x => x.RaquetModel)
            .NotEmpty().WithMessage("El modelo de raqueta es obligatorio")
            .Length(1, 200).WithMessage("El modelo de raqueta debe tener entre 1 y 200 caracteres");

        RuleFor(x => x.Nudos)
            .Must(n => n == 2 || n == 4)
            .WithMessage("El número de nudos debe ser 2 o 4");

        RuleFor(x => x.DateString)
            .NotEmpty().WithMessage("La fecha de encordado es obligatoria");

        RuleFor(x => x.Color)
            .MaximumLength(100).WithMessage("El color no puede superar los 100 caracteres");

        RuleFor(x => x.StringSetup)
            .NotNull().WithMessage("La configuración de cuerdas es obligatoria")
            .SetValidator(new StringSetupRequestValidator());
    }
}

public class StringSetupRequestValidator : AbstractValidator<StringSetupDto>
{
    public StringSetupRequestValidator()
    {
        RuleFor(x => x.StringV)
            .NotEmpty().WithMessage("El nombre del cordaje vertical es obligatorio")
            .Length(1, 100).WithMessage("El nombre del cordaje vertical debe tener entre 1 y 100 caracteres");

        RuleFor(x => x.TensionV)
            .InclusiveBetween(5.0, 40.0).WithMessage("La tensión vertical debe estar entre 5 y 40kg");

        RuleFor(x => x.PreStetchV)
            .InclusiveBetween((short)0, (short)20).WithMessage("El pre-estirado vertical debe estar entre 0 y 20%");

        RuleFor(x => x.StringH)
            .MaximumLength(100).WithMessage("El nombre horizontal no puede superar los 100 caracteres");

        RuleFor(x => x.TensionH)
            .InclusiveBetween(5.0, 40.0)
            .When(x => x.TensionH > 0)
            .WithMessage("La tensión horizontal debe estar entre 5 y 40kg");

        RuleFor(x => x.PreStetchH)
            .InclusiveBetween((short)0, (short)20)
            .When(x => x.PreStetchH > 0)
            .WithMessage("El pre-estirado horizontal debe estar entre 0 y 20%");
    }
}