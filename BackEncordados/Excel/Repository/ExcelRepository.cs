using BackEncordados.Common.Database.Config;
using BackEncordados.Excel.Dto;
using BackEncordados.Materials.Model;
using BackEncordados.Purchased.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Excel.Repository;

/// <summary>
/// Implementación de <see cref="IExcelRepository"/> que consolida consultas
/// a través de los cuatro DbContexts del sistema.
/// </summary>
/// <remarks>
/// <para>Inyecta los cuatro DbContexts de la aplicación para centralizar
/// el acceso a datos de exportación Excel en un solo repositorio:</para>
/// <list type="table">
///   <listheader>
///     <term>DbContext</term>
///     <description>Tablas consultadas</description>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>PedidosDbContext</c></term>
///     <description>Pedidos, PedidoLineas</description>
///     <description>Órdenes de encordado y sus líneas de detalle.</description>
///   </item>
///   <item>
///     <term><c>UserDbContext</c></term>
///     <description>Users</description>
///     <description>Usuarios (jugadores, workers, supervisores).</description>
///   </item>
///   <item>
///     <term><c>TalleresDbContext</c></term>
///     <description>Partidos (Tournaments)</description>
///     <description>Torneos con sus listas de asignación.</description>
///   </item>
///   <item>
///     <term><c>MaterialsDbContext</c></term>
///     <description>Materiales, Cuerdas</description>
///     <description>Inventario de materiales y cuerdas.</description>
///   </item>
/// </list>
/// <para>Todos los métodos de consulta usan <c>AsNoTracking()</c> donde es posible,
/// ya que el repositorio solo lee datos sin necesidad de seguimiento de cambios.</para>
/// </remarks>
/// <param name="pedidosDbContext">DbContext de pedidos y líneas de pedido.</param>
/// <param name="userDbContext">DbContext de usuarios.</param>
/// <param name="talleresDbContext">DbContext de torneos (partidos).</param>
/// <param name="materialsDbContext">DbContext de materiales y cuerdas.</param>
public class ExcelRepository(
    PedidosDbContext pedidosDbContext,
    UserDbContext userDbContext,
    TalleresDbContext talleresDbContext,
    MaterialsDbContext materialsDbContext
) : IExcelRepository
{
    /// <summary>
    /// Obtiene el resumen simple de un torneo agrupando pedidos por jugador.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Consulta todos los pedidos del torneo desde <c>PedidosDbContext.Pedidos</c>.</description></item>
    ///   <item><description>Extrae los IDs de jugadores distintos (<c>PlayerId</c>).</description></item>
    ///   <item><description>Consulta los usuarios correspondientes desde <c>UserDbContext.Users</c>
    ///   y los carga en un <c>Dictionary&lt;Ulid, (Username, Name)&gt;</c> para acceso O(1).</description></item>
    ///   <item><description>Agrupa los pedidos por <c>PlayerId</c>, proyecta cada grupo a
    ///   <see cref="TournamentExcelRowDto"/> con conteo de raquetas y suma de precios,
    ///   y ordena por nombre de usuario.</description></item>
    /// </list>
    /// <para><b>Casos borde:</b></para>
    /// <list type="bullet">
    ///   <item><description>Si no hay pedidos → retorna lista vacía (no lanza error).</description></item>
    ///   <item><description>Si un <c>PlayerId</c> no existe en la tabla de usuarios → se muestra como "Unknown".</description></item>
    /// </list>
    /// </remarks>
    /// <param name="tournamentId">ID del torneo a consultar (ULID).</param>
    /// <returns>Lista de filas con resumen por jugador, ordenadas por <c>Username</c> ascendente.</returns>
    public async Task<IEnumerable<TournamentExcelRowDto>> GetTournamentDataAsync(Ulid tournamentId)
    {
        var pedidos = await pedidosDbContext.Pedidos
            .Where(p => p.TournamentId == tournamentId)
            .ToListAsync();

        var playerIds = pedidos.Select(p => p.PlayerId).Distinct().ToList();
        
        var users = await userDbContext.Users
            .Where(u => playerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new { u.Username, u.Name });

        var result = pedidos
            .GroupBy(p => p.PlayerId)
            .Select(g =>
            {
                var user = users.GetValueOrDefault(g.Key);
                return new TournamentExcelRowDto
                {
                    Username = user?.Username ?? "Unknown",
                    Name = user?.Name ?? "Unknown",
                    RacketCount = g.Count(),
                    TotalPrice = (decimal)g.Sum(p => p.Price)
                };
            })
            .OrderBy(r => r.Username)
            .ToList();

        return result;
    }

    /// <summary>
    /// Verifica si un usuario está en la lista de supervisores de un torneo.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el torneo por <paramref name="tournamentId"/> en <c>TalleresDbContext.Partidos</c>.</description></item>
    ///   <item><description>Si no existe → retorna <c>false</c> (torneo no encontrado).</description></item>
    ///   <item><description>Verifica si <paramref name="userId"/> está en <c>SupervisorList</c>
    ///   (búsqueda lineal O(n) sobre <c>List&lt;Ulid&gt;</c>).</description></item>
    /// </list>
    /// <para><b>Nota:</b> No lanza excepción si el torneo no existe; retorna <c>false</c>
    /// como comportamiento fail-safe.</para>
    /// </remarks>
    /// <param name="userId">ID del usuario a verificar (ULID).</param>
    /// <param name="tournamentId">ID del torneo (ULID).</param>
    /// <returns><c>true</c> si el usuario está en <c>SupervisorList</c> del torneo; <c>false</c> en caso contrario.</returns>
    public async Task<bool> IsUserSupervisorOfTournamentAsync(Ulid userId, Ulid tournamentId)
    {
        var tournament = await talleresDbContext.Partidos
            .Where(t => t.Id == tournamentId)
            .FirstOrDefaultAsync();

        if (tournament == null)
            return false;

        return tournament.SupervisorList.Contains(userId);
    }

    /// <summary>
    /// Verifica si un usuario es el propietario de un torneo.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el torneo por <paramref name="tournamentId"/> en <c>TalleresDbContext.Partidos</c>.</description></item>
    ///   <item><description>Si no existe → retorna <c>false</c>.</description></item>
    ///   <item><description>Compara <paramref name="userId"/> con la propiedad <c>Owner</c> del torneo.</description></item>
    /// </list>
    /// <para><b>Nota:</b> No lanza excepción si el torneo no existe; retorna <c>false</c>.</para>
    /// </remarks>
    /// <param name="userId">ID del usuario a verificar (ULID).</param>
    /// <param name="tournamentId">ID del torneo (ULID).</param>
    /// <returns><c>true</c> si <c>tournament.Owner == userId</c>; <c>false</c> en caso contrario.</returns>
    public async Task<bool> IsUserOwnerOfTournamentAsync(Ulid userId, Ulid tournamentId)
    {
        var tournament = await talleresDbContext.Partidos
            .Where(t => t.Id == tournamentId)
            .FirstOrDefaultAsync();

        if (tournament == null)
            return false;

        return tournament.Owner == userId;
    }

    /// <summary>
    /// Obtiene datos multi-hoja de un torneo para exportación avanzada a Excel.
    /// </summary>
    /// <remarks>
    /// <para>Método principal del repositorio que orquesta consultas a los cuatro DbContexts
    /// según los tipos de datos solicitados en <paramref name="types"/>.</para>
    /// <para><b>Flujo general:</b></para>
    /// <list type="number">
    ///   <item><description>Crea una instancia vacía de <see cref="ExcelAdvancedDataDto"/>.</description></item>
    ///   <item><description>Si <c>types</c> contiene <c>"users"</c> → consulta usuarios del torneo y mapea a <see cref="ExcelUsersDto"/>.</description></item>
    ///   <item><description>Si <c>types</c> contiene <c>"materials"</c> → consulta materiales y mapea a <see cref="ExcelMaterialsDto"/>.</description></item>
    ///   <item><description>Si <c>types</c> contiene <c>"cuerdas"</c> → consulta cuerdas y mapea a <see cref="ExcelCuerdasDto"/>.</description></item>
    ///   <item><description>Si <c>types</c> contiene <c>"tournament"</c> → consulta datos del torneo y mapea a <see cref="ExcelTournamentDto"/>.</description></item>
    ///   <item><description>Si <c>types</c> contiene <c>"pedidos"</c> → consulta pedidos y sus líneas, y mapea a <see cref="ExcelPedidosDto"/> y <see cref="ExcelPedidoLineasDto"/>.</description></item>
    ///   <item><description>Retorna el DTO con las listas pobladas según los tipos solicitados.</description></item>
    /// </list>
    /// <para><b>Mapeo de propiedades — módulo "users":</b></para>
    /// <list type="table">
    ///   <listheader><term>DTO (ExcelUsersDto)</term><description>Entidad (User)</description></listheader>
    ///   <item><term>Id</term><description>u.Id.ToString()</description></item>
    ///   <item><term>Username</term><description>u.Username</description></item>
    ///   <item><term>Name</term><description>u.Name</description></item>
    ///   <item><term>Email</term><description>u.Email</description></item>
    ///   <item><term>Phone</term><description>u.Phone</description></item>
    ///   <item><term>TournamentId</term><description>u.TournamentId?.ToString()</description></item>
    /// </list>
    /// <para><b>Mapeo — módulo "materials":</b></para>
    /// <list type="table">
    ///   <listheader><term>DTO (ExcelMaterialsDto)</term><description>Entidad (Material)</description></listheader>
    ///   <item><term>Id</term><description>m.Id</description></item>
    ///   <item><term>TournamentId</term><description>m.TournamentId.ToString()</description></item>
    ///   <item><term>Marca</term><description>m.Marca</description></item>
    ///   <item><term>Modelo</term><description>m.Modelo</description></item>
    ///   <item><term>Stock</term><description>m.Stock</description></item>
    ///   <item><term>Precio</term><description>m.Precio</description></item>
    ///   <item><term>Type</term><description>m.Type.ToString()</description></item>
    /// </list>
    /// <para><b>Mapeo — módulo "cuerdas":</b></para>
    /// <list type="table">
    ///   <listheader><term>DTO (ExcelCuerdasDto)</term><description>Entidad (Cuerda)</description></listheader>
    ///   <item><term>Id</term><description>c.Id</description></item>
    ///   <item><term>TournamentId</term><description>c.TournamentId.ToString()</description></item>
    ///   <item><term>Marca</term><description>c.Marca</description></item>
    ///   <item><term>Modelo</term><description>c.Modelo</description></item>
    ///   <item><term>Stock</term><description>c.Stock</description></item>
    ///   <item><term>Precio</term><description>c.Precio</description></item>
    ///   <item><term>Calibre</term><description>c.Calibre</description></item>
    ///   <item><term>StringFormat</term><description>c.StringFormat.ToString()</description></item>
    ///   <item><term>StringsType</term><description>c.StringsType.ToString()</description></item>
    /// </list>
    /// <para><b>Mapeo — módulo "tournament":</b></para>
    /// <list type="table">
    ///   <listheader><term>DTO (ExcelTournamentDto)</term><description>Entidad (Tournament)</description></listheader>
    ///   <item><term>Id</term><description>t.Id.ToString()</description></item>
    ///   <item><term>Owner</term><description>t.Owner.ToString()</description></item>
    ///   <item><term>Title</term><description>t.Title</description></item>
    ///   <item><term>StartTournament</term><description>t.StartTournament</description></item>
    ///   <item><term>EndTournament</term><description>t.EndTournament</description></item>
    ///   <item><term>Logotype</term><description>t.Logotype</description></item>
    ///   <item><term>WorkersList</term><description>string.Join(";", t.WorkersList)</description></item>
    ///   <item><term>SupervisorList</term><description>string.Join(";", t.SupervisorList)</description></item>
    /// </list>
    /// <para><b>Mapeo — módulo "pedidos" (Pedidos):</b></para>
    /// <list type="table">
    ///   <listheader><term>DTO (ExcelPedidosDto)</term><description>Entidad (Pedido)</description></listheader>
    ///   <item><term>Id</term><description>p.Id.ToString()</description></item>
    ///   <item><term>TournamentId</term><description>p.TournamentId.ToString()</description></item>
    ///   <item><term>PlayerId</term><description>p.PlayerId.ToString()</description></item>
    ///   <item><term>AssignedTo</term><description>p.AssignedTo.ToString()</description></item>
    ///   <item><term>Machine</term><description>p.Machine</description></item>
    ///   <item><term>Comments</term><description>p.Comments</description></item>
    ///   <item><term>Price</term><description>p.Price</description></item>
    ///   <item><term>PayStatus</term><description>p.PayStatus.ToString()</description></item>
    /// </list>
    /// <para><b>Mapeo — módulo "pedidos" (PedidoLineas):</b></para>
    /// <list type="table">
    ///   <listheader><term>DTO (ExcelPedidoLineasDto)</term><description>Entidad (PedidoLinea)</description></listheader>
    ///   <item><term>Id</term><description>l.Id.ToString()</description></item>
    ///   <item><term>PedidoId</term><description>l.PedidoId.ToString()</description></item>
    ///   <item><term>RaquetModel</term><description>l.RaquetModel</description></item>
    ///   <item><term>Nudos</term><description>l.Nudos</description></item>
    ///   <item><term>DateString</term><description>l.DateString</description></item>
    ///   <item><term>Logotype</term><description>l.Logotype</description></item>
    ///   <item><term>Color</term><description>l.Color</description></item>
    ///   <item><term>StringV</term><description>l.StringSetup?.StringV ?? ""</description></item>
    ///   <item><term>TensionV</term><description>l.StringSetup?.TensionV ?? 0</description></item>
    ///   <item><term>PreStetchV</term><description>l.StringSetup?.PreStetchV ?? 0</description></item>
    ///   <item><term>StringH</term><description>l.StringSetup?.StringH ?? ""</description></item>
    ///   <item><term>TensionH</term><description>l.StringSetup?.TensionH ?? 0</description></item>
    ///   <item><term>PreStetchH</term><description>l.StringSetup?.PreStetchH ?? 0</description></item>
    ///   <item><term>Status</term><description>l.Status.ToString()</description></item>
    /// </list>
    /// <para><b>Notas técnicas:</b></para>
    /// <list type="bullet">
    ///   <item><description>Todas las consultas usan <c>AsNoTracking()</c> (solo lectura).</description></item>
    ///   <item><description>Las líneas de pedido se consultan en batch mediante <c>Contains</c>
    ///   para evitar el problema N+1.</description></item>
    ///   <item><description>El módulo "tournament" puede retornar una lista vacía si el torneo no existe.</description></item>
    ///   <item><description><c>StringSetup</c> es un Owned Entity (value object) anidado en PedidoLinea;
    ///   se accede con null-conditional operator por seguridad.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="tournamentId">ID del torneo a consultar (ULID).</param>
    /// <param name="types">Lista de tipos de datos a incluir.
    /// Valores válidos: "users", "materials", "cuerdas", "tournament", "pedidos".</param>
    /// <returns><see cref="ExcelAdvancedDataDto"/> con las listas correspondientes pobladas.</returns>
    public async Task<ExcelAdvancedDataDto> GetAdvancedDataAsync(Ulid tournamentId, List<string> types)
    {
        var data = new ExcelAdvancedDataDto();

        if (types.Contains("users"))
        {
            var users = await userDbContext.Users
                .AsNoTracking()
                .Where(u => u.TournamentId == tournamentId)
                .ToListAsync();
            
            data.Users = users.Select(u => new ExcelUsersDto
            {
                Id = u.Id.ToString(),
                Username = u.Username,
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                TournamentId = u.TournamentId?.ToString()
            }).ToList();
        }

        if (types.Contains("materials"))
        {
            var materials = await materialsDbContext.Materiales
                .AsNoTracking()
                .Where(m => m.TournamentId == tournamentId)
                .ToListAsync();

            data.Materials = materials.Select(m => new ExcelMaterialsDto
            {
                Id = m.Id,
                TournamentId = m.TournamentId.ToString(),
                Marca = m.Marca,
                Modelo = m.Modelo,
                Stock = m.Stock,
                Precio = m.Precio,
                Type = m.Type.ToString()
            }).ToList();
        }

        if (types.Contains("cuerdas"))
        {
            var cuerdas = await materialsDbContext.Cuerdas
                .AsNoTracking()
                .Where(c => c.TournamentId == tournamentId)
                .ToListAsync();

            data.Cuerdas = cuerdas.Select(c => new ExcelCuerdasDto
            {
                Id = c.Id,
                TournamentId = c.TournamentId.ToString(),
                Marca = c.Marca,
                Modelo = c.Modelo,
                Stock = c.Stock,
                Precio = c.Precio,
                Calibre = c.Calibre,
                StringFormat = c.StringFormat.ToString(),
                StringsType = c.StringsType.ToString()
            }).ToList();
        }

        if (types.Contains("tournament"))
        {
            var tournament = await talleresDbContext.Partidos
                .Where(t => t.Id == tournamentId)
                .FirstOrDefaultAsync();

            if (tournament != null)
            {
                data.Tournament.Add(new ExcelTournamentDto
                {
                    Id = tournament.Id.ToString(),
                    Owner = tournament.Owner.ToString(),
                    Title = tournament.Title,
                    StartTournament = tournament.StartTournament,
                    EndTournament = tournament.EndTournament,
                    Logotype = tournament.Logotype,
                    WorkersList = string.Join(";", tournament.WorkersList),
                    SupervisorList = string.Join(";", tournament.SupervisorList)
                });
            }
        }

        if (types.Contains("pedidos"))
        {
            var pedidos = await pedidosDbContext.Pedidos
                .Where(p => p.TournamentId == tournamentId)
                .ToListAsync();

            data.Pedidos = pedidos.Select(p => new ExcelPedidosDto
            {
                Id = p.Id.ToString(),
                TournamentId = p.TournamentId.ToString(),
                PlayerId = p.PlayerId.ToString(),
                AssignedTo = p.AssignedTo.ToString(),
                Machine = p.Machine,
                Comments = p.Comments,
                Price = p.Price,
                PayStatus = p.PayStatus.ToString()
            }).ToList();

            var pedidoIds = pedidos.Select(p => p.Id).ToList();
            var lineas = await pedidosDbContext.PedidoLineas
                .Where(l => pedidoIds.Contains(l.PedidoId))
                .ToListAsync();

            data.PedidoLineas = lineas.Select(l => new ExcelPedidoLineasDto
            {
                Id = l.Id.ToString(),
                PedidoId = l.PedidoId.ToString(),
                RaquetModel = l.RaquetModel,
                Nudos = l.Nudos,
                DateString = l.DateString,
                Logotype = l.Logotype,
                Color = l.Color,
                StringV = l.StringSetup?.StringV ?? "",
                TensionV = l.StringSetup?.TensionV ?? 0,
                PreStetchV = l.StringSetup?.PreStetchV ?? 0,
                StringH = l.StringSetup?.StringH ?? "",
                TensionH = l.StringSetup?.TensionH ?? 0,
                PreStetchH = l.StringSetup?.PreStetchH ?? 0,
                Status = l.Status.ToString()
            }).ToList();
        }

        return data;
    }
}