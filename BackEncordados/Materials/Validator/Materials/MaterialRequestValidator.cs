using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Model;
using FluentValidation;

namespace BackEncordados.Materials.Validator.Materials;

public class MaterialRequestValidator : AbstractValidator<MaterialRequestDto>
{
    public MaterialRequestValidator()
    {
        // The DTO already has DataAnnotations for required/lengths/ranges.
        // Here we only validate that 'Type' maps to the MaterialType enum.
        RuleFor(x => x.Type)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Type es obligatorio.")
            .Must(value => Enum.TryParse<MaterialType>(value, true, out _))
            .WithMessage("Type inválido. Valores permitidos: " + string.Join(", ", Enum.GetNames(typeof(MaterialType))));
    }
}

