using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using FluentValidation;

namespace BackEncordados.Purchased.Validator;

/// <summary>
/// Validates a <see cref="PedidoLineaRequestDto"/> for a single line item within a purchase order.
/// </summary>
/// <remarks>
/// <para>
/// Enforces the following business rules:
/// </para>
/// <list type="bullet">
///   <item><description><c>RaquetModel</c> — required, 1–200 characters.</description></item>
///   <item><description><c>Nudos</c> (knots) — must be exactly 2 or 4. No other knot count is allowed.</description></item>
///   <item><description><c>DateString</c> — required (typically a JSON date string or scheduled date).</description></item>
///   <item><description><c>Color</c> — optional, max 100 characters.</description></item>
///   <item><description><c>StringSetup</c> — required, delegates to <see cref="StringSetupRequestValidator"/>.</description></item>
/// </list>
/// <para>
/// This validator is nested inside <see cref="PurchasedRequestValidator"/> for each element
/// of the <c>Lineas</c> collection via <c>ForEach(…).SetValidator(…)</c>.
/// </para>
/// </remarks>
public class PedidoLineaRequestValidator : AbstractValidator<PedidoLineaRequestDto>
{
    /// <summary>
    /// Defines all validation rules for a single purchase order line item.
    /// </summary>
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

/// <summary>
/// Validates a <see cref="StringSetupDto"/> for the creation (not patch) scenario.
/// </summary>
/// <remarks>
/// <para>
/// In creation mode, vertical string name (<c>StringV</c>) is required, tension
/// must be within the realistic 5–40 kg range, and pre-stretch must be 0–20 %.
/// Horizontal fields are optional because some racquets use a one-piece stringing method.
/// </para>
/// <para>When horizontal values ARE provided:</para>
/// <list type="bullet">
///   <item><description><c>TensionH</c> must be between 5.0 and 40.0 kg if greater than 0.</description></item>
///   <item><description><c>PreStetchH</c> must be between 0 and 20 % if greater than 0.</description></item>
/// </list>
/// <para>
/// This validator is used by <see cref="PedidoLineaRequestValidator"/> for the <c>StringSetup</c> property.
/// For patch scenarios (where all core fields are required), see <see cref="StringSetupPatchValidator"/>.
/// </para>
/// </remarks>
public class StringSetupRequestValidator : AbstractValidator<StringSetupDto>
{
    /// <summary>
    /// Defines validation rules for a string setup when creating a line item.
    /// </summary>
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
