using BackEncordados.Purchased.Dto;
using FluentValidation;

namespace BackEncordados.Purchased.Validator;

/// <summary>
/// Validates a <see cref="PurchasedPatchDto"/> for partial order updates.
/// Only validates fields that are present (non-null), allowing granular patches.
/// </summary>
public class PurchasedPatchValidator : AbstractValidator<PurchasedPatchDto>
{
    /// <summary>
    /// Defines length constraints for Machine and Comments when they are provided.
    /// </summary>
    public PurchasedPatchValidator()
    {
        RuleFor(x => x.Machine)
            .Length(1, 100).WithMessage("La máquina debe tener entre 1 y 100 caracteres")
            .When(x => x.Machine != null);

        RuleFor(x => x.Comments)
            .MaximumLength(500).WithMessage("Los comentarios no pueden superar los 500 caracteres")
            .When(x => x.Comments != null);
    }
}
