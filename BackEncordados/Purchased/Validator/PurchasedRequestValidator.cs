using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.keys;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Repository;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using FluentValidation;

namespace BackEncordados.Purchased.Validator;

/// <summary>
/// Validates a <see cref="PurchasedRequestDto"/> before a purchase order is created.
/// </summary>
/// <remarks>
/// <para>
/// This validator performs the following cross-cutting validation steps:
/// </para>
/// <list type="number">
///   <item><description><b>Tournament validation</b> — verifies the tournament exists in the database and has not been soft-deleted (<c>IsDeleted == false</c>).</description></item>
///   <item><description><b>Player validation</b> — looks up the <c>PlayerName</c> by username (cache-first, then DB). Rejects if the user does not exist, is deleted, or does not have the <c>USER</c> role. On success, stores the resolved <see cref="User"/> in <c>RootContextData["PlayerUlid"]</c>.</description></item>
///   <item><description><b>Encorder validation</b> — looks up the <c>AssignedToName</c> by username. Rejects if the user does not exist, is deleted, or does not have the <c>ENCORDER</c> or <c>OWNER</c> role. Stores the resolved <see cref="User"/> in <c>RootContextData["AssignedToGuid"]</c>.</description></item>
///   <item><description><b>Payment status validation</b> — ensures the <c>PayStatus</c> string matches one of the <see cref="PaymentStatus"/> enum values (case-insensitive).</description></item>
///   <item><description><b>Line items validation</b> — ensures at least one line item exists and delegates each item to <see cref="PedidoLineaRequestValidator"/>.</description></item>
/// </list>
/// <para>
/// The resolved <see cref="User"/> objects stored in <c>RootContextData</c> are consumed by the controller
/// or service layer to avoid redundant database lookups after validation passes.
/// </para>
/// </remarks>
public class PurchasedRequestValidator : AbstractValidator<PurchasedRequestDto>
{
    /// <summary>
    /// Initializes a new <see cref="PurchasedRequestValidator"/> with all rule definitions.
    /// </summary>
    /// <param name="tournamentRepository">Repository used to verify the tournament exists and is active.</param>
    /// <param name="userRepository">Repository used to look up player and encorder users by username.</param>
    /// <param name="cache">Cache service (L1 + L2 hybrid) to avoid repeated DB lookups for recently validated usernames.</param>
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
