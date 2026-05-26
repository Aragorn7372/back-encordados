using BackEncordados.Purchased.Dto;
using FluentValidation;

namespace BackEncordados.Purchased.Validator;

/// <summary>
/// Validates a <see cref="StringSetupDto"/> when used in a patch (partial update) scenario
/// for a <see cref="PedidoLinea"/> line item.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="StringSetupRequestValidator"/> (used in creation), this validator is stricter
/// because in a patch context the caller is explicitly choosing to update the string setup,
/// so all core fields (vertical string name, tension, pre-stretch) are required.
/// </para>
/// <para>Validation rules:</para>
/// <list type="table">
///   <listheader>
///     <term>Field</term>
///     <term>Rule</term>
///   </listheader>
///   <item>
///     <term><c>StringV</c></term>
///     <term>Required, 1–100 characters.</term>
///   </item>
///   <item>
///     <term><c>TensionV</c></term>
///     <term>Required, 5.0–40.0 kg.</term>
///   </item>
///   <item>
///     <term><c>PreStetchV</c></term>
///     <term>Required, 0–20 %.</term>
///   </item>
///   <item>
///     <term><c>StringH</c></term>
///     <term>Optional, max 100 characters.</term>
///   </item>
///   <item>
///     <term><c>TensionH</c></term>
///     <term>Optional, 5.0–40.0 kg when provided and &gt; 0.</term>
///   </item>
///   <item>
///     <term><c>PreStetchH</c></term>
///     <term>Optional, 0–20 % when provided and &gt; 0.</term>
///   </item>
/// </list>
/// </remarks>
public class StringSetupPatchValidator : AbstractValidator<StringSetupDto>
{
    /// <summary>
    /// Defines validation rules for string setup in a patch operation.
    /// </summary>
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
