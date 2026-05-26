using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Model;
using BackEncordados.Talleres.Repository;
using FluentValidation;

namespace BackEncordados.Materials.Validator.Strings;

/// <summary>
/// Validador FluentValidation para <see cref="CuerdaRequestDto"/>.
/// </summary>
/// <remarks>
/// <para>Reglas de validación:</para>
/// <list type="bullet">
///   <item><description><c>TournamentId</c> — obligatorio, debe existir en BD y no estar eliminado (validación asíncrona).</description></item>
///   <item><description><c>StringFormat</c> — obligatorio, debe ser un valor válido de <see cref="FormatoCuerda"/>.</description></item>
///   <item><description><c>StringsType</c> — obligatorio, debe ser un valor válido de <see cref="StringsType"/>.</description></item>
/// </list>
/// <para>Depende de <see cref="ITournamentRepository"/> para la validación de existencia del torneo.</para>
/// </remarks>
public class CuerdaRequestValidator : AbstractValidator<CuerdaRequestDto>
{
    /// <summary>
    /// Inicializa el validador con las reglas de negocio para la creación de cuerdas.
    /// </summary>
    /// <param name="tournamentRepository">Repositorio de torneos para validar existencia.</param>
    public CuerdaRequestValidator(ITournamentRepository tournamentRepository)
    {
        RuleFor(x => x.TournamentId)
            .Cascade(CascadeMode.Stop) 
            .NotNull().WithMessage("El ID del torneo es obligatorio")
            .MustAsync(async (id, cancellation) => 
            {
                var tournament = await tournamentRepository.FindByIdAsync(id);
                return tournament != null && !tournament.IsDeleted;
            })
            .WithMessage("El torneo no existe o ha sido cancelado.");
        
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

