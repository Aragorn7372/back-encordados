using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Model;
using FluentValidation;

namespace BackEncordados.Materials.Validator.Strings;

/// <summary>
/// Validador FluentValidation para <see cref="CuerdaPatchDto"/>.
/// </summary>
/// <remarks>
/// <para>Reglas de validación (solo aplican cuando el campo es proporcionado):</para>
/// <list type="bullet">
///   <item><description><c>Stock</c> — si >= 0, debe ser un número positivo.</description></item>
///   <item><description><c>Precio</c> — si >= 0, debe ser mayor que 0.</description></item>
///   <item><description><c>StringFormat</c> — si no está vacío, debe ser un valor válido de <see cref="FormatoCuerda"/>.</description></item>
///   <item><description><c>StringsType</c> — si no está vacío, debe ser un valor válido de <see cref="StringsType"/>.</description></item>
/// </list>
/// </remarks>
public class CuerdaPatchValidator : AbstractValidator<CuerdaPatchDto>
{
    /// <summary>
    /// Inicializa el validador con reglas condicionales para actualización de cuerdas.
    /// </summary>
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


