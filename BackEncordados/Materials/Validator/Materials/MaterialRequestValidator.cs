using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Model;
using BackEncordados.Talleres.Repository;
using FluentValidation;

namespace BackEncordados.Materials.Validator.Materials;

public class MaterialRequestValidator : AbstractValidator<MaterialRequestDto>
{
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

