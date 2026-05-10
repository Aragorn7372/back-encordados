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
        RuleFor(x => x.TournamentId)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("El ID del torneo es obligatorio")
            .MustAsync(async (id, cancellation) =>
            {
                var tournament = await tournamentRepository.FindByIdAsync(id);
                return tournament != null && !tournament.IsDeleted;
            })
            .WithMessage("El torneo no existe o ha sido cancelado.");

        RuleFor(x => x.PlayerName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El nombre del jugador es obligatorio")
            .CustomAsync(async (username, context, cancellation) =>
            {
                string key = CacheKeys.UserKey + username;

                var player = await cache.GetAsync<User?>(key);
                User? user = null;

                if (player == null)
                {
                    user = await userRepository.FindByUsernameAsync(username!);
                    if (user == null || user.IsDeleted || user.Role != User.UserRoles.USER)
                    {
                        context.AddFailure("El jugador no existe, no tiene permisos o fue eliminado.");
                        return;
                    }

                    player = user;
                    await cache.SetAsync(key, player);
                }

                context.RootContextData["PlayerUlid"] = player;
            });

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

                    if (user == null || user.IsDeleted ||
                       (user.Role != User.UserRoles.ENCORDER && user.Role != User.UserRoles.OWNER))
                    {
                        context.AddFailure("El encordador asignado no es válido o fue eliminado.");
                        return;
                    }

                    assigned = user;
                    await cache.SetAsync(key, assigned);
                }

                context.RootContextData["AssignedToGuid"] = assigned;
            });

        RuleFor(x => x.PayStatus)
            .NotEmpty().WithMessage("El estado de pago no puede estar vacío.")
            .Must(status => Enum.GetNames<PaymentStatus>()
                .Any(name => name.Equals(status, StringComparison.OrdinalIgnoreCase)))
            .WithMessage(x => $"El valor '{x.PayStatus}' no es válido. Opciones: {string.Join(", ", Enum.GetNames<PaymentStatus>())}");

        RuleFor(x => x.Lineas)
            .NotEmpty().WithMessage("Debe haber al menos una línea de pedido")
            .ForEach(linea => linea.SetValidator(new PedidoLineaRequestValidator()));
    }
}