using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Model;
using BackEncordados.Talleres.Repository;
using FluentValidation;

namespace BackEncordados.Materials.Validator.Strings;

public class CuerdaRequestValidator : AbstractValidator<CuerdaRequestDto>
{
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

