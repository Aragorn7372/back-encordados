using BackEncordados.Common.Database.Config;
using BackEncordados.Usuarios.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.SignalR;

[Authorize(policy: "RequireSupervisorRole")]
public class SignalHub(TalleresDbContext context) : Hub {
    
    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole(User.UserRoles.ADMIN) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Tournament_All_Admin");
        }
        else
        {
            var userUlidRaw = Context.UserIdentifier;

            if (Ulid.TryParse(userUlidRaw, out Ulid userUlid))
            {
                var torneosIds = await context.Partidos
                    .Where(t => !t.IsDeleted && (
                        t.Owner == userUlid || 
                        t.WorkersList.Contains(userUlid) || 
                        t.SupervisorList.Contains(userUlid)
                    ))
                    .Select(t => t.Id)
                    .ToListAsync();

                foreach (var torneoId in torneosIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Tournament_{torneoId}");
                }
            }
        }

        await base.OnConnectedAsync();
    }
}