using BackEncordados.Common.Database.Config;
using BackEncordados.Usuarios.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Common.SignalR;

/// <summary>
/// Hub de SignalR que gestiona la conexión y pertenencia a grupos de torneos
/// para notificaciones en tiempo real.
/// </summary>
/// <remarks>
/// <para>Requiere autenticación con policy <c>"RequireSupervisorRole"</c> (solo usuarios
/// con rol Supervisor o Admin pueden conectar).</para>
///
/// <para><b>Asignación de grupos:</b></para>
/// <list type="bullet">
///   <item><description><b>Usuarios ADMIN:</b> Se unen al grupo global <c>"Tournament_All_Admin"</c>
///   y reciben notificaciones de <b>todos</b> los torneos del sistema.</description></item>
///   <item><description><b>Usuarios no-ADMIN (Owner, Worker, Supervisor):</b> Se unen únicamente
///   a los grupos <c>"Tournament_{id}"</c> de los torneos donde tienen algún rol asignado.</description></item>
/// </list>
///
/// <para>Esto permite enviar notificaciones selectivas: un cambio en un torneo específico
/// se notifica solo a los miembros de <c>"Tournament_{id}"</c>, mientras que eventos
/// globales se envían a <c>"Tournament_All_Admin"</c>.</para>
/// </remarks>
/// <param name="context">DbContext de torneos para consultar la pertenencia del usuario a cada torneo.</param>
[Authorize(policy: "RequireSupervisorRole")]
public class SignalHub(TalleresDbContext context) : Hub {
    
    /// <summary>
    /// Se ejecuta cuando un usuario se conecta al hub. Asigna el usuario a los grupos
    /// de torneos correspondientes según su rol y permisos.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Verifica si el usuario autenticado tiene rol <c>ADMIN</c>.</description></item>
    ///   <item><description>Si es ADMIN → lo agrega al grupo global <c>"Tournament_All_Admin"</c>
    ///   (recibe notificaciones de todos los torneos).</description></item>
    ///   <item><description>Si no es ADMIN → parsea su <c>UserIdentifier</c> como <see cref="Ulid"/>,
    ///   consulta los torneos activos donde sea <c>Owner</c>, esté en <c>WorkersList</c> o
    ///   en <c>SupervisorList</c>, y lo agrega al grupo <c>"Tournament_{id}"</c> de cada uno.</description></item>
    ///   <item><description>Si ocurre una excepción durante el proceso, se registra en consola
    ///   pero se garantiza la llamada a <c>base.OnConnectedAsync()</c> para no bloquear la conexión.</description></item>
    /// </list>
    /// </remarks>
    /// <returns>Tarea asincrónica que completa cuando el usuario fue asignado a todos los grupos correspondientes.</returns>
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