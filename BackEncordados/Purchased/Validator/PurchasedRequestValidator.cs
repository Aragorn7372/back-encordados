using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.keys;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Repository;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using FluentValidation;

namespace BackEncordados.Purchased.Validator;

public class PurchasedRequestValidator : AbstractValidator<PurchasedRequestDto>
{
    public PurchasedRequestValidator(ITournamentRepository tournamentRepository, IUserRepository userRepository, ICacheService cache)
    {
        RuleFor(r => r.Nudos)
            .NotNull().WithMessage("Nudos es requerido.")
            .Must(n => n == 2 || n == 4 ).WithMessage("Nudos debe ser 2 o 4.");
        RuleFor(x => x.TournamentId)
            .Cascade(CascadeMode.Stop) // Si el ID es nulo, no sigue con la validación de DB
            .NotNull().WithMessage("El ID del torneo es obligatorio")
            .MustAsync(async (id, cancellation) => 
            {
                // Comprobamos que exista y que NO esté eliminado
                var tournament = await tournamentRepository.FindByIdAsync(id);
                return tournament != null && !tournament.IsDeleted;
            })
            .WithMessage("El torneo no existe o ha sido cancelado.");
       // Validación para el Jugador
RuleFor(x => x.PlayerName)
    .Cascade(CascadeMode.Stop)
    .NotEmpty().WithMessage("El nombre del jugador es obligatorio")
    .CustomAsync(async (username, context, cancellation) => 
    {
        string key = CacheKeys.UserKey + username;
        
        // Intentar obtener desde el caché híbrido
        var player = await cache.GetAsync<User?>(key);
        User? user = null;

        if (player == null)
        {
            //  Si no está en caché, ir a DB
            user = await userRepository.FindByUsernameAsync(username!);
            if (user == null || user.IsDeleted || user.Role != User.UserRoles.USER)
            {
                context.AddFailure("El jugador no existe, no tiene permisos o fue eliminado.");
                return;
            }
            
            player = user;
            await cache.SetAsync(key, player);
        }

        // Guardar el GUID para que el servicio lo use sin re-consultar
        context.RootContextData["PlayerGuid"] = player;
    });

// Validación para el Encordador asignado
RuleFor(x => x.AssignedToName)
    .Cascade(CascadeMode.Stop)
    .NotEmpty().WithMessage("El nombre del encordador es obligatorio")
    .CustomAsync(async (username, context, cancellation) => 
    {
        string key = CacheKeys.UserKey + username;
        
        var assigned = await cache.GetAsync<User?>(key);

        if (assigned == null)
        {
            var user = await userRepository.FindByUsernameAsync(username!);
            
            // Validación de roles específicos para el encordador/owner
            if (user == null || user.IsDeleted || 
               (user.Role != User.UserRoles.ENCORDER && user.Role != User.UserRoles.OWNER))
            {
                context.AddFailure("El encordador asignado no es válido o fue eliminado.");
                return;
            }

            assigned = user;
            await cache.SetAsync(key, assigned);
        }

        // Guardar en el contexto con una clave distinta
        context.RootContextData["AssignedToGuid"] = assigned;
    });
        RuleFor(x => x.TypeWork)
            .NotEmpty().WithMessage("El tipo de trabajo no puede estar vacío.")
            .Must(type => Enum.GetNames<TypePuchase>()
                .Any(name => name.Equals(type, StringComparison.OrdinalIgnoreCase)))
            .WithMessage(x => $"El valor '{x.TypeWork}' no es válido. Opciones: {string.Join(", ", Enum.GetNames<TypePuchase>())}");
        RuleFor(x => x.PayStatus)
            .NotEmpty().WithMessage("El estado de pago no puede estar vacío.")
            .Must(status => Enum.GetNames<PaymentStatus>()
                .Any(name => name.Equals(status, StringComparison.OrdinalIgnoreCase)))
            .WithMessage(x => $"El valor '{x.PayStatus}' no es válido. Opciones: {string.Join(", ", Enum.GetNames<PaymentStatus>())}");
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("El estado del pedido no puede estar vacío.")
            .Must(status => Enum.GetNames<Status>()
                .Any(name => name.Equals(status, StringComparison.OrdinalIgnoreCase)))
            .WithMessage(x => $"El valor '{x.Status}' no es válido. Opciones: {string.Join(", ", Enum.GetNames<Status>())}");
    }
}