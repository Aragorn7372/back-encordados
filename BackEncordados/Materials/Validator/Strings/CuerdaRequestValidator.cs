using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Model;
using FluentValidation;

namespace BackEncordados.Materials.Validator.Strings;

public class CuerdaRequestValidator : AbstractValidator<CuerdaRequestDto>
{
    public CuerdaRequestValidator()
    {
        RuleFor(x => x.StringFormat)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("StringFormat es obligatorio.")
            .Must(value => Enum.TryParse<FormatoCuerda>(value, true, out _))
            .WithMessage("StringFormat inválido. Valores permitidos: " + string.Join(", ", Enum.GetNames(typeof(FormatoCuerda))));

        RuleFor(x => x.StringsType)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("StringsType es obligatorio.")
            .Must(value => Enum.TryParse<StringsType>(value, true, out _))
            .WithMessage("StringsType inválido. Valores permitidos: " + string.Join(", ", Enum.GetNames(typeof(StringsType))));
    }
}

