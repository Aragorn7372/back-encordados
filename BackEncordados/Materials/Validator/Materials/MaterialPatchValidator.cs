using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Model;
using FluentValidation;

namespace BackEncordados.Materials.Validator.Materials;

public class MaterialPatchValidator : AbstractValidator<MaterialPatchDto>
{
    public MaterialPatchValidator()
    {
        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Stock >= 0)
            .WithMessage("El stock debe ser 0 o un número positivo.");

        RuleFor(x => x.Precio)
            .GreaterThan(0)
            .When(x => x.Precio >= 0)
            .WithMessage("El precio debe ser un número mayor que 0.");

        RuleFor(x => x.Type)
            .Must(value => string.IsNullOrEmpty(value) || Enum.TryParse<MaterialType>(value, true, out _))
            .WithMessage("Type inválido. Valores permitidos: " + string.Join(", ", Enum.GetNames(typeof(MaterialType))));
    }
}

