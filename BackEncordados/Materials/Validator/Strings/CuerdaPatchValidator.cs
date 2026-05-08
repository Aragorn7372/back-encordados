using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Model;
using FluentValidation;

namespace BackEncordados.Materials.Validator.Strings;

public class CuerdaPatchValidator : AbstractValidator<CuerdaPatchDto>
{
    public CuerdaPatchValidator()
    {
        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Stock >= 0)
            .WithMessage("El stock debe ser 0 o un número positivo.");

        RuleFor(x => x.Precio)
            .GreaterThan(0)
            .When(x => x.Precio >= 0)
            .WithMessage("El precio debe ser un número mayor que 0.");

        RuleFor(x => x.StringFormat)
            .Must(value => string.IsNullOrEmpty(value) || Enum.TryParse<FormatoCuerda>(value, true, out _))
            .WithMessage("StringFormat inválido. Valores permitidos: " + string.Join(", ", Enum.GetNames(typeof(FormatoCuerda))));

        RuleFor(x => x.StringsType)
            .Must(value => string.IsNullOrEmpty(value) || Enum.TryParse<StringsType>(value, true, out _))
            .WithMessage("StringsType inválido. Valores permitidos: " + string.Join(", ", Enum.GetNames(typeof(StringsType))));
    }
}


