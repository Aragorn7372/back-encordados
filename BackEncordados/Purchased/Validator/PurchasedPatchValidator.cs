using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using FluentValidation;

namespace BackEncordados.Purchased.Validator;

public class PurchasedPatchValidator : AbstractValidator<PurchasedPatchDto>
{
    public PurchasedPatchValidator()
    {
        RuleFor(x => x.TypeString)
            .Length(1, 100).WithMessage("El tipo de trabajo debe tener entre 1 y 100 caracteres")
            .When(x => x.TypeString != null);

        RuleFor(x => x.TypeWork)
            .IsEnumName(typeof(TypePuchase), caseSensitive: false)
            .WithMessage($"Tipo de trabajo inválido. Opciones: {string.Join(", ", Enum.GetNames<TypePuchase>())}")
            .When(x => x.TypeWork != null);

        RuleFor(x => x.DateString)
            .GreaterThan(DateTime.UtcNow).WithMessage("La fecha de finalización debe ser en el futuro")
            .When(x => x.DateString.HasValue);

        RuleFor(x => x.RaquetModel)
            .Length(1, 200).WithMessage("El modelo de raqueta debe tener entre 1 y 200 caracteres")
            .When(x => x.RaquetModel != null);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("El precio debe ser un número positivo")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.Nudos)
            .Must(n => n == 2 || n == 4)
            .WithMessage("El número de nudos debe ser 2 o 4")
            .When(x => x.Nudos.HasValue);

        RuleFor(x => x.Machine)
            .Length(1, 100).WithMessage("La máquina debe tener entre 1 y 100 caracteres")
            .When(x => x.Machine != null);

        RuleFor(x => x.Comments)
            .MaximumLength(500).WithMessage("Los comentarios no pueden superar los 500 caracteres")
            .When(x => x.Comments != null);
        RuleFor(x => x.StringSetup)
            .SetValidator(new StringSetupPatchValidator()!)
            .When(x => x.StringSetup != null);
    }
}
