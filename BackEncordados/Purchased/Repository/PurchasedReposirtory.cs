using BackEncordados.Common.Database.Config;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Purchased.Repository;

/// <summary>
/// Implements <see cref="IPuchasedRepository"/> using Entity Framework Core over <see cref="PedidosDbContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// <c>PurchasedReposirtory</c> is the data-access layer for the Pedidos aggregate, covering:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Category</term>
///     <description>Methods</description>
///   </listheader>
///   <item>
///     <term>Query (order)</term>
///     <description><see cref="FindAllAsync"/>, <see cref="FindByIdAsync"/></description>
///   </item>
///   <item>
///     <term>Command (order)</term>
///     <description><see cref="CreatePurchasedAsync"/>, <see cref="UpdatePurchasedAsync"/>, <see cref="CancelPurchasedAsync"/>, <see cref="ChangeStatusPurchasedAsync"/></description>
///   </item>
///   <item>
///     <term>Query (line)</term>
///     <description><see cref="FindLineaByIdAsync"/></description>
///   </item>
///   <item>
///     <term>Command (line)</term>
///     <description><see cref="CreateLineaAsync"/>, <see cref="UpdateLineaAsync"/>, <see cref="ChangeLineaStatusAsync"/></description>
///   </item>
/// </list>
/// <para>
/// Design decisions:
/// </para>
/// <list type="bullet">
///   <item><description><b>No-tracking queries</b> — All read methods use <c>AsNoTracking()</c> for performance. Only write methods track entities.</description></item>
///   <item><description><b>Two-phase line loading</b> — <see cref="FindAllAsync"/> loads line items in a batch second query (WHERE PedidoId IN ...) to avoid N+1.</description></item>
///   <item><description><b>Idempotent cancel</b> — <see cref="CancelPurchasedAsync"/> pre-filters to orders not already CANCELED.</description></item>
///   <item><description><b>Navigation population</b> — Line-level methods eagerly load the parent <c>Pedido</c> so the service layer can access TournamentId for notifications.</description></item>
/// </list>
/// </remarks>
public class PurchasedReposirtory(PedidosDbContext context, ILogger<PurchasedReposirtory> logger) : IPuchasedRepository
{
    /// <summary>
    /// Returns a paginated, filtered, and sorted list of <see cref="Pedidos"/> together with the total count of matching records.
    /// </summary>
    /// <remarks>
    /// <para>Filtering pipeline:</para>
    /// <list type="number">
    ///   <item><description>Start with <c>context.Pedidos.AsNoTracking().AsQueryable()</c>.</description></item>
    ///   <item><description>If <c>filter.IsEncorder == true</c> and <c>UserId</c> parses to a valid <see cref="Ulid"/>: filter by <c>p.AssignedTo == encorderId</c>.</description></item>
    ///   <item><description>If <c>filter.IsUser == true</c> and <c>UserId</c> parses to a valid <see cref="Ulid"/>: filter by <c>p.PlayerId == userId</c>.</description></item>
    ///   <item><description>If <c>filter.TournamentId</c> has a value: filter by exact match on <c>TournamentId</c>.</description></item>
    ///   <item><description>If <c>filter.Search</c> is not empty: match against <c>Comments</c>, <c>Machine</c> (via LIKE), and any line item whose <c>RaquetModel</c> matches (sub-query).</description></item>
    ///   <item><description>Apply sorting by <c>CreatedAt</c>, <c>Machine</c>, <c>PlayerId</c>, <c>AssignedTo</c> (as "encorder"), or default <c>Id</c>, respecting <c>Direction</c>.</description></item>
    ///   <item><description>Apply pagination: skip <c>filter.Page * filter.Size</c>, take <c>filter.Size</c>.</description></item>
    ///   <item><description>After the main query, load all line items for the returned order IDs in one batch query.</description></item>
    /// </list>
    /// <para>Performance note: the free-text search against <c>RaquetModel</c> requires a separate sub-query because it crosses an aggregate boundary.</para>
    /// </remarks>
    /// <param name="filter">Pagination, sort, and filter parameters.</param>
    /// <returns>A tuple containing the page of orders (with line items populated) and total matching count.</returns>
    public async Task<(IEnumerable<Pedidos> Items, int TotalCount)> FindAllAsync(FilterPurchasedDto filter)
    {
        var query = context.Pedidos.AsNoTracking().AsQueryable();

        if (filter.IsEncorder == true && filter.UserId != null && Ulid.TryParse(filter.UserId, out var encorderId))
            query = query.Where(p => p.AssignedTo == encorderId);

        if (filter.IsUser == true && filter.UserId != null && Ulid.TryParse(filter.UserId, out var userId))
            query = query.Where(p => p.PlayerId == userId);

        if (filter.TournamentId.HasValue)
            query = query.Where(p => p.TournamentId == filter.TournamentId.Value);

        if (!string.IsNullOrEmpty(filter.Search))
        {
            var pedidoIdsWithMatchingLineas = await context.PedidoLineas
                .Where(l => EF.Functions.Like(l.RaquetModel, $"%{filter.Search}%"))
                .Select(l => l.PedidoId)
                .Distinct()
                .ToListAsync();

            query = query.Where(p => EF.Functions.Like(p.Comments, $"%{filter.Search}%")
                          || EF.Functions.Like(p.Machine, $"%{filter.Search}%")
                          || pedidoIdsWithMatchingLineas.Contains(p.Id));
        }

        var totalCount = await query.CountAsync();

        bool isDesc = filter.Direction.ToLower().Equals("desc");
        query = filter.SortBy.ToLower() switch
        {
            "createdAt" => isDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "machine" => isDesc ? query.OrderByDescending(p => p.Machine) : query.OrderBy(p => p.Machine),
            "playerId" => isDesc ? query.OrderByDescending(p => p.PlayerId) : query.OrderBy(p => p.PlayerId),
            "encorder" => isDesc ? query.OrderByDescending(p => p.AssignedTo) : query.OrderBy(p => p.AssignedTo),
            _ => isDesc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id)
        };

        var items = await query.Skip(filter.Page * filter.Size).Take(filter.Size).ToListAsync();

        if (items.Any())
        {
            var pedidoIds = items.Select(p => p.Id).ToList();
            var lineas = await context.PedidoLineas.Where(l => pedidoIds.Contains(l.PedidoId)).ToListAsync();
            foreach (var item in items)
            {
                item.Lineas = lineas.Where(l => l.PedidoId == item.Id).ToList();
            }
        }

        return (items, totalCount);
    }

    /// <summary>
    /// Retrieves a single <see cref="Pedidos"/> by its ID, including all associated <see cref="PedidoLinea"/> items.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <c>AsNoTracking()</c>. If the order header is found, its line items are loaded
    /// in a second query. Returns <c>null</c> when no order matches the given Id.
    /// </para>
    /// </remarks>
    /// <param name="id">The order <see cref="Ulid"/>.</param>
    /// <returns>The matching order with line items, or <c>null</c>.</returns>
    public async Task<Pedidos?> FindByIdAsync(Ulid id)
    {
        logger.LogInformation("Buscando pedido con ID {Id}", id);
        var pedido = await context.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (pedido != null)
        {
            pedido.Lineas = await context.PedidoLineas.Where(l => l.PedidoId == id).ToListAsync();
        }
        return pedido;
    }

    /// <summary>
    /// Persists a new <see cref="Pedidos"/> aggregate (order + line items) and returns the saved entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The caller is expected to have generated <see cref="Ulid"/> values for both the order and all line
    /// items (<see cref="Ulid.NewUlid()"/>). This method calls <c>SaveChangesAsync</c> internally.
    /// </para>
    /// </remarks>
    /// <param name="pedidos">The aggregate to persist (header + child line items).</param>
    /// <returns>The saved aggregate with database-generated values (timestamps) set.</returns>
    public async Task<Pedidos> CreatePurchasedAsync(Pedidos pedidos)
    {
        logger.LogInformation("Creando pedido");
        var saved = await context.Pedidos.AddAsync(pedidos);
        await context.SaveChangesAsync();
        return saved.Entity;
    }

    /// <summary>
    /// Partially updates an existing order's Machine, Comments, and PayStatus fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method loads the tracked entity, applies non-null/non-default changes from the input,
    /// and calls <c>SaveChangesAsync</c>. Only three fields are mutable through this path:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>Machine</c> — replaced if the input string is not null or empty.</description></item>
    ///   <item><description><c>Comments</c> — replaced if the input string is not null or empty.</description></item>
    ///   <item><description><c>PayStatus</c> — replaced if the input value is not the default (default is PENDING).</description></item>
    /// </list>
    /// <para><c>UpdatedAt</c> is always bumped to the current UTC time.</para>
    /// </remarks>
    /// <param name="pedidos">An entity carrying the new values for fields that should be updated.</param>
    /// <param name="id">The target order <see cref="Ulid"/>.</param>
    /// <returns>The updated tracked entity, or <c>null</c> if no matching order was found.</returns>
    public async Task<Pedidos?> UpdatePurchasedAsync(Pedidos pedidos, Ulid id)
    {
        logger.LogInformation("Actualizando pedido con ID {Id}", id);

        var existingPurchased = await context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);

        if (existingPurchased == null)
        {
            logger.LogWarning("Pedido no encontrado. ID {Id}", id);
            return null;
        }

        if (!string.IsNullOrEmpty(pedidos.Machine))
            existingPurchased.Machine = pedidos.Machine;

        if (!string.IsNullOrEmpty(pedidos.Comments))
            existingPurchased.Comments = pedidos.Comments;

        if (pedidos.PayStatus != default)
            existingPurchased.PayStatus = pedidos.PayStatus;

        existingPurchased.UpdatedAt = DateTime.UtcNow;

        var saved = context.Pedidos.Update(existingPurchased);
        await context.SaveChangesAsync();

        logger.LogInformation("Pedido actualizado exitosamente. ID {Id}", id);
        return saved.Entity;
    }

    /// <summary>
    /// Marks an order as <see cref="PaymentStatus.CANCELED"/> and cancels all its line items.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The query includes a guard (<c>PayStatus != PaymentStatus.CANCELED</c>) to make the operation
    /// idempotent. All associated <see cref="PedidoLinea"/> items are loaded and set to <see cref="Status.CANCELED"/>
    /// with their <c>UpdatedAt</c> timestamps bumped within the same EF Core transaction.
    /// </para>
    /// <para>Returns <c>null</c> if the order was not found or was already canceled.</para>
    /// </remarks>
    /// <param name="id">The order <see cref="Ulid"/>.</param>
    /// <returns>The updated order entity, or <c>null</c>.</returns>
    public async Task<Pedidos?> CancelPurchasedAsync(Ulid id)
    {
        logger.LogInformation("Cancelando pedido con ID {Id}", id);
        var existingPurchased = await context.Pedidos.FirstOrDefaultAsync(p => p.Id == id && p.PayStatus != PaymentStatus.CANCELED);

        if (existingPurchased == null)
            return null;

        var lineas = await context.PedidoLineas.Where(l => l.PedidoId == id).ToListAsync();

        existingPurchased.PayStatus = PaymentStatus.CANCELED;
        foreach (var linea in lineas)
        {
            linea.Status = Status.CANCELED;
            linea.UpdatedAt = DateTime.UtcNow;
        }

        var saved = context.Pedidos.Update(existingPurchased);
        await context.SaveChangesAsync();
        logger.LogInformation("Pedido cancelado exitosamente. ID {Id}", id);
        return saved.Entity;
    }

    /// <summary>
    /// Changes only the payment status of an existing order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <paramref name="payStatus"/> string is parsed case-insensitively via <c>Enum.TryParse</c>.
    /// If parsing fails, the existing status is silently preserved (no error thrown) and <c>UpdatedAt</c>
    /// is still bumped to prevent tight-loop retries.
    /// </para>
    /// </remarks>
    /// <param name="id">The order <see cref="Ulid"/>.</param>
    /// <param name="payStatus">The new payment status as a string (e.g. "PAID", "PENDING", "CANCELED").</param>
    /// <returns>The updated order entity, or <c>null</c>.</returns>
    public async Task<Pedidos?> ChangeStatusPurchasedAsync(Ulid id, string payStatus)
    {
        logger.LogInformation("Cambiando estado de pago del pedido con ID {Id}", id);
        var existingPurchased = await context.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
        if (existingPurchased == null)
            return null;

        existingPurchased.PayStatus = Enum.TryParse<PaymentStatus>(payStatus, true, out var newPayStatus)
            ? newPayStatus
            : existingPurchased.PayStatus;

        existingPurchased.UpdatedAt = DateTime.UtcNow;

        var saved = context.Pedidos.Update(existingPurchased);
        await context.SaveChangesAsync();
        logger.LogInformation("Estado de pago del pedido actualizado exitosamente. ID {Id}", id);
        return saved.Entity;
    }

    /// <summary>
    /// Retrieves a single <see cref="PedidoLinea"/> by its ID and eagerly loads its parent <see cref="Pedidos"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The parent order is loaded separately (<c>FirstOrDefaultAsync</c>) and assigned to the navigation
    /// property. This is required by the service layer to access <c>Pedido.TournamentId</c> for
    /// SignalR group targeting and WhatsApp notifications.
    /// </para>
    /// </remarks>
    /// <param name="lineaId">The line <see cref="Ulid"/>.</param>
    /// <returns>The matching line (with <c>Pedido</c> populated), or <c>null</c>.</returns>
    public async Task<PedidoLinea?> FindLineaByIdAsync(Ulid lineaId)
    {
        logger.LogInformation("Buscando línea con ID {Id}", lineaId);
        var linea = await context.PedidoLineas.FirstOrDefaultAsync(l => l.Id == lineaId);
        if (linea != null)
        {
            linea.Pedido = await context.Pedidos.FirstOrDefaultAsync(p => p.Id == linea.PedidoId) ?? null!;
        }
        return linea;
    }

    /// <summary>
    /// Persists a new <see cref="PedidoLinea"/> under an existing parent order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generates a new <see cref="Ulid"/> for the line ID server-side.
    /// The parent order must already exist (foreign key constraint via <c>PedidoId</c>).
    /// Calls <c>SaveChangesAsync</c> internally.
    /// </para>
    /// </remarks>
    /// <param name="linea">The line entity to persist. <c>Id</c> is overwritten with <see cref="Ulid.NewUlid()"/>.</param>
    /// <returns>The saved line entity.</returns>
    public async Task<PedidoLinea> CreateLineaAsync(PedidoLinea linea)
    {
        logger.LogInformation("Creando línea para pedido {PedidoId}", linea.PedidoId);
        linea.Id = Ulid.NewUlid();
        var saved = await context.PedidoLineas.AddAsync(linea);
        await context.SaveChangesAsync();
        return saved.Entity;
    }

    /// <summary>
    /// Replaces all mutable fields on an existing <see cref="PedidoLinea"/> and saves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="UpdatePurchasedAsync"/> (which does partial update), this method performs
    /// a full field-by-field overwrite of the tracked entity:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>RaquetModel, Nudos, DateString, Logotype, Color — all replaced unconditionally.</description></item>
    ///   <item><description>Status — replaced unconditionally.</description></item>
    ///   <item><description>StringSetup — replaced unconditionally (value object, full swap).</description></item>
    ///   <item><description>UpdatedAt — bumped to current UTC time.</description></item>
    /// </list>
    /// <para>The parent <c>Pedido</c> navigation property is also reloaded.</para>
    /// </remarks>
    /// <param name="linea">An entity carrying the full set of new values.</param>
    /// <param name="lineaId">The target line <see cref="Ulid"/>.</param>
    /// <returns>The updated line (with <c>Pedido</c> populated), or <c>null</c>.</returns>
    public async Task<PedidoLinea?> UpdateLineaAsync(PedidoLinea linea, Ulid lineaId)
    {
        logger.LogInformation("Actualizando línea con ID {Id}", lineaId);

        var existingLinea = await context.PedidoLineas.FirstOrDefaultAsync(l => l.Id == lineaId);
        if (existingLinea == null)
        {
            logger.LogWarning("Línea no encontrada. ID {Id}", lineaId);
            return null;
        }

        existingLinea.Pedido = await context.Pedidos.FirstOrDefaultAsync(p => p.Id == existingLinea.PedidoId) ?? null!;

        existingLinea.RaquetModel = linea.RaquetModel;
        existingLinea.Nudos = linea.Nudos;
        existingLinea.DateString = linea.DateString;
        existingLinea.Logotype = linea.Logotype;
        existingLinea.Color = linea.Color;
        existingLinea.Status = linea.Status;
        existingLinea.StringSetup = linea.StringSetup;
        existingLinea.UpdatedAt = DateTime.UtcNow;

        var saved = context.PedidoLineas.Update(existingLinea);
        await context.SaveChangesAsync();

        logger.LogInformation("Línea actualizada exitosamente. ID {Id}", lineaId);
        return saved.Entity;
    }

    /// <summary>
    /// Sets the <see cref="Status"/> of an existing line item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Also reloads the parent <c>Pedido</c> navigation property so the service layer
    /// can access <c>Pedido.TournamentId</c> and player/encorder information for notifications.
    /// </para>
    /// </remarks>
    /// <param name="lineaId">The line <see cref="Ulid"/>.</param>
    /// <param name="status">The new status value.</param>
    /// <returns>The updated line (with <c>Pedido</c> populated), or <c>null</c>.</returns>
    public async Task<PedidoLinea?> ChangeLineaStatusAsync(Ulid lineaId, Status status)
    {
        logger.LogInformation("Cambiando estado de línea con ID {Id} a {Status}", lineaId, status);
        var linea = await context.PedidoLineas.FirstOrDefaultAsync(l => l.Id == lineaId);
        if (linea == null)
            return null;

        linea.Pedido = await context.Pedidos.FirstOrDefaultAsync(p => p.Id == linea.PedidoId) ?? null!;

        linea.Status = status;
        linea.UpdatedAt = DateTime.UtcNow;

        var saved = context.PedidoLineas.Update(linea);
        await context.SaveChangesAsync();

        logger.LogInformation("Estado de línea cambiado exitosamente. ID {Id}, Status {Status}", lineaId, status);
        return saved.Entity;
    }

    /// <summary>
    /// Flushes any pending changes to the underlying data store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used by <see cref="Service.PurchasedService.ChangeAllLineasStatusAsync"/> where multiple
    /// line entities are mutated in a loop before a single call to persist.
    /// </para>
    /// </remarks>
    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}
