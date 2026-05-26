using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Model;
using FluentValidation;

namespace BackEncordados.Materials.Validator.Materials;

/// <summary>
/// Validador FluentValidation para <see cref="MaterialPatchDto"/>.
/// </summary>
/// <remarks>
/// <para>Reglas de validación (solo aplican cuando el campo es proporcionado):</para>
/// <list type="bullet">
///   <item><description><c>Stock</c> — si >= 0, debe ser un número positivo.</description></item>
///   <item><description><c>Precio</c> — si >= 0, debe ser mayor que 0.</description></item>
///   <item><description><c>Type</c> — si no está vacío, debe ser un valor válido de <see cref="MaterialType"/>.</description></item>
/// </list>
/// </remarks>
public class MaterialPatchValidator : AbstractValidator<MaterialPatchDto>
{
    /// <summary>
    /// Inicializa el validador con reglas condicionales para actualización de materiales.
    /// </summary>
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

