using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Model;
using BackEncordados.Talleres.Repository;
using FluentValidation;

namespace BackEncordados.Materials.Validator.Materials;

/// <summary>
/// Validador FluentValidation para <see cref="MaterialRequestDto"/>.
/// </summary>
/// <remarks>
/// <para>Reglas de validación:</para>
/// <list type="bullet">
///   <item><description><c>TournamentId</c> — obligatorio, debe existir en BD y no estar eliminado (validación asíncrona).</description></item>
///   <item><description><c>Type</c> — obligatorio, debe ser un valor válido de <see cref="MaterialType"/> (case-insensitive).</description></item>
/// </list>
/// <para>Depende de <see cref="ITournamentRepository"/> para la validación de existencia del torneo.</para>
/// </remarks>
public class MaterialRequestValidator : AbstractValidator<MaterialRequestDto>
{
    /// <summary>
    /// Inicializa el validador con las reglas de negocio para la creación de materiales.
    /// </summary>
    /// <param name="tournamentRepository">Repositorio de torneos para validar existencia.</param>
    public MaterialRequestValidator(ITournamentRepository tournamentRepository)
    {
        RuleFor(x => x.TournamentId)
            .Cascade(CascadeMode.Stop) 
            .NotNull().WithMessage("El ID del torneo es obligatorio")
            .MustAsync(async (id, cancellation) => 
            {
                // Comprobamos que exista y que NO esté eliminado
                var tournament = await tournamentRepository.FindByIdAsync(id);
                return tournament != null && !tournament.IsDeleted;
            })
            .WithMessage("El torneo no existe o ha sido cancelado.");
        
        RuleFor(x => x.Type)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Type es obligatorio.")
            .Must(value => Enum.TryParse<MaterialType>(value, true, out _))
            .WithMessage("Type inválido. Valores permitidos: " + string.Join(", ", Enum.GetNames(typeof(MaterialType))));
    }
}

