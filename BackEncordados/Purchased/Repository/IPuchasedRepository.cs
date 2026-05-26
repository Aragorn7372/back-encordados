using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;

namespace BackEncordados.Purchased.Repository;

/// <summary>
/// Defines data-access contracts for the Purchased (pedidos) aggregate,
/// covering both top-level order operations and line-level (PedidoLinea) operations.
/// </summary>
/// <remarks>
/// <para>
/// <c>IPuchasedRepository</c> is the sole repository for the Pedidos aggregate.
/// It handles:
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
///   <item>
///     <term>Flush</term>
///     <description><see cref="SaveChangesAsync"/></description>
///   </item>
/// </list>
/// <para>
/// All query methods use <c>AsNoTracking()</c> for read performance.
/// Write methods that accept <see cref="FilterPurchasedDto"/> support pagination, sorting, and composite filtering
/// (by encorder, player, tournament, and free-text search on comments/machine/raquet model).
/// </para>
/// <para>
/// <b>Note:</b> The interface name preserves the original typo (<c>IPuchasedRepository</c> instead of <c>IPurchasedRepository</c>)
/// for backward compatibility with DI registrations.
/// </para>
/// </remarks>
public interface IPuchasedRepository
{
    /// <summary>
    /// Returns a paginated, filtered, and sorted list of <see cref="Pedidos"/> together with the total count of matching records.
    /// </summary>
    /// <remarks>
    /// <para>Filtering logic:</para>
    /// <list type="bullet">
    ///   <item><description><b>Encorder filter</b> — if <c>IsEncorder == true</c>, filters by <c>AssignedTo == UserId</c>.</description></item>
    ///   <item><description><b>Player filter</b> — if <c>IsUser == true</c>, filters by <c>PlayerId == UserId</c>.</description></item>
    ///   <item><description><b>Tournament filter</b> — optional exact match on <c>TournamentId</c>.</description></item>
    ///   <item><description><b>Search</b> — free-text match against <c>Comments</c>, <c>Machine</c>, and line-level <c>RaquetModel</c> (via LIKE).</description></item>
    ///   <item><description><b>Sorting</b> — by <c>CreatedAt</c>, <c>Machine</c>, <c>PlayerId</c>, <c>AssignedTo</c> (as "encorder"), or default by <c>Id</c>.</description></item>
    /// </list>
    /// <para>
    /// After pagination, associated <see cref="PedidoLinea"/> items are loaded in a second query
    /// using a batch <c>WHERE PedidoId IN (...)</c> to avoid N+1.
    /// </para>
    /// </remarks>
    /// <param name="filter">Pagination (Page, Size), sort (SortBy, Direction), and filter (UserId, IsEncorder, IsUser, TournamentId, Search) parameters.</param>
    /// <returns>A tuple containing the page items (with line items populated) and total element count across all pages.</returns>
    Task<(IEnumerable<Pedidos> Items, int TotalCount)> FindAllAsync(FilterPurchasedDto filter);

    /// <summary>
    /// Retrieves a single <see cref="Pedidos"/> by its ID, including all associated <see cref="PedidoLinea"/> items.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <c>AsNoTracking()</c>. The line items are loaded in a separate query after the header is found.
    /// Returns <c>null</c> when no order matches the given <paramref name="id"/>.
    /// </para>
    /// </remarks>
    /// <param name="id">The order <see cref="Ulid"/>.</param>
    /// <returns>The matching <see cref="Pedidos"/> with <c>Lineas</c> populated, or <c>null</c> if not found.</returns>
    Task<Pedidos?> FindByIdAsync(Ulid id);

    /// <summary>
    /// Persists a new <see cref="Pedidos"/> aggregate (order header + all line items) and returns the saved entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The caller is responsible for generating new <see cref="Ulid"/> values for the order and each line.
    /// This method calls <c>SaveChangesAsync</c> internally.
    /// </para>
    /// </remarks>
    /// <param name="pedidos">The aggregate to create, with all child <see cref="PedidoLinea"/> items populated.</param>
    /// <returns>The saved <see cref="Pedidos"/> with database-generated values (timestamps) set.</returns>
    Task<Pedidos> CreatePurchasedAsync(Pedidos pedidos);

    /// <summary>
    /// Partially updates an existing order's mutable fields (Machine, Comments, PayStatus) by ID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the three fields listed above are updated; line items are not affected by this method.
    /// Fields that are null or empty on the input <paramref name="pedidos"/> retain their previous values.
    /// <c>UpdatedAt</c> is always set to the current UTC time.
    /// </para>
    /// </remarks>
    /// <param name="pedidos">An entity carrying the new values for fields to change (non-empty strings and non-default enum values are applied).</param>
    /// <param name="id">The target order <see cref="Ulid"/>.</param>
    /// <returns>The updated <see cref="Pedidos"/> or <c>null</c> if the order was not found.</returns>
    Task<Pedidos?> UpdatePurchasedAsync(Pedidos pedidos, Ulid id);

    /// <summary>
    /// Marks an existing order as <see cref="PaymentStatus.CANCELED"/> and sets every associated
    /// <see cref="PedidoLinea"/> to <see cref="Status.CANCELED"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The query specifically excludes orders already in <c>CANCELED</c> state
    /// (<c>PayStatus != PaymentStatus.CANCELED</c>) to make the operation idempotent.
    /// All line items for the order are loaded and their status and timestamps are updated.
    /// </para>
    /// </remarks>
    /// <param name="id">The order <see cref="Ulid"/> to cancel.</param>
    /// <returns>The updated <see cref="Pedidos"/> (with line items updated in the same transaction) or <c>null</c> if not found or already canceled.</returns>
    Task<Pedidos?> CancelPurchasedAsync(Ulid id);

    /// <summary>
    /// Changes only the payment status of an existing order to the provided string value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <paramref name="payStatus"/> string is parsed case-insensitively via <c>Enum.TryParse</c>.
    /// If parsing fails, the existing status is preserved and <c>UpdatedAt</c> is still bumped.
    /// </para>
    /// </remarks>
    /// <param name="id">The order <see cref="Ulid"/>.</param>
    /// <param name="payStatus">The new payment status as a string (e.g. "PAID", "PENDING", "CANCELED"). Case-insensitive.</param>
    /// <returns>The updated <see cref="Pedidos"/> or <c>null</c> if not found.</returns>
    Task<Pedidos?> ChangeStatusPurchasedAsync(Ulid id, string payStatus);

    /// <summary>
    /// Retrieves a single <see cref="PedidoLinea"/> by its ID and eagerly loads its parent <see cref="Pedidos"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The parent <c>Pedido</c> is loaded in a second query using <c>FirstOrDefaultAsync</c> on the <c>Pedidos</c> table.
    /// This is required by the service layer for tournament ID resolution and SignalR notifications.
    /// </para>
    /// </remarks>
    /// <param name="lineaId">The line <see cref="Ulid"/>.</param>
    /// <returns>The matching <see cref="PedidoLinea"/> (with <c>Pedido</c> navigation property populated) or <c>null</c>.</returns>
    Task<PedidoLinea?> FindLineaByIdAsync(Ulid lineaId);

    /// <summary>
    /// Persists a new <see cref="PedidoLinea"/> under an existing parent order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A new <see cref="Ulid"/> is generated server-side before saving.
    /// The method calls <c>SaveChangesAsync</c> internally.
    /// </para>
    /// </remarks>
    /// <param name="linea">The line entity to persist. <c>Id</c> will be overwritten with a new Ulid.</param>
    /// <returns>The saved <see cref="PedidoLinea"/> with generated identity values.</returns>
    Task<PedidoLinea> CreateLineaAsync(PedidoLinea linea);

    /// <summary>
    /// Replaces all mutable fields on an existing <see cref="PedidoLinea"/> and saves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="UpdatePurchasedAsync"/>, this method performs a full replacement of
    /// RaquetModel, Nudos, DateString, Logotype, Color, Status, and StringSetup from the input entity.
    /// <c>UpdatedAt</c> is always bumped. The parent <c>Pedido</c> navigation property is reloaded.
    /// </para>
    /// </remarks>
    /// <param name="linea">An entity carrying the full set of new values for the line.</param>
    /// <param name="lineaId">The target line <see cref="Ulid"/>.</param>
    /// <returns>The updated <see cref="PedidoLinea"/> or <c>null</c> if not found.</returns>
    Task<PedidoLinea?> UpdateLineaAsync(PedidoLinea linea, Ulid lineaId);

    /// <summary>
    /// Sets the <see cref="Status"/> of an existing <see cref="PedidoLinea"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Also reloads the parent <see cref="Pedidos"/> navigation property on the returned entity
    /// for use in subsequent notification logic.
    /// </para>
    /// </remarks>
    /// <param name="lineaId">The line <see cref="Ulid"/>.</param>
    /// <param name="status">The new status value (e.g. <see cref="Status.PENDING"/>, <see cref="Status.COMPLETED"/>, <see cref="Status.CANCELED"/>).</param>
    /// <returns>The updated <see cref="PedidoLinea"/> (with <c>Pedido</c> populated) or <c>null</c> if not found.</returns>
    Task<PedidoLinea?> ChangeLineaStatusAsync(Ulid lineaId, Status status);

    /// <summary>
    /// Flushes any pending changes to the underlying data store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used after bulk operations such as <see cref="Service.PurchasedService.ChangeAllLineasStatusAsync"/>
    /// where multiple line entities are mutated in memory before a single flush.
    /// </para>
    /// </remarks>
    Task SaveChangesAsync();
}
