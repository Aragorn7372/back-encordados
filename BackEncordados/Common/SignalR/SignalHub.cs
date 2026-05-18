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
                    // Query 1: Traer Partidos donde el usuario es Owner 
                    var ownedIds = await context.Partidos
                        .Where(t => !t.IsDeleted && t.Owner == userUlid)
                        .Select(t => t.Id)
                        .ToListAsync();

                    // Query 2: Traer el resto de Partidos a memoria para filtrar por WorkersList y SupervisorList
                    var otherPartidos = await context.Partidos
                        .Where(t => !t.IsDeleted && t.Owner != userUlid)
                        .AsNoTracking()
                        .ToListAsync();

                    // Filtrar en memoria 
                    var otherIds = otherPartidos
                        .Where(t => t.WorkersList?.Contains(userUlid) == true || 
                                    t.SupervisorList?.Contains(userUlid) == true)
                        .Select(t => t.Id)
                        .ToList();

                    // Combinar los resultados
                    var torneosIds = ownedIds.Union(otherIds).ToList();

                    foreach (var torneoId in torneosIds)
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