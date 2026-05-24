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
        try
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
                        .Where(t => !t.IsDeleted)
                        .Select(t => new { t.Id, t.Owner, t.WorkersList, t.SupervisorList })
                        .AsNoTracking()
                        .ToListAsync();

                    var hubIds = torneosIds
                        .Where(t => t.Owner == userUlid ||
                                    t.WorkersList.Contains(userUlid) ||
                                    t.SupervisorList.Contains(userUlid))
                        .Select(t => t.Id)
                        .ToList();

                    foreach (var torneoId in hubIds)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"Tournament_{torneoId}");
                    }
                }
            }

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en SignalHub.OnConnectedAsync: {ex.Message}");
            await base.OnConnectedAsync();
        }
    }
}