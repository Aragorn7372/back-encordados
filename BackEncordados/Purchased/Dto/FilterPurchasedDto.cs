namespace BackEncordados.Purchased.Dto;

/// <summary>
/// DTO de filtro y paginación para la consulta de pedidos.
/// </summary>
/// <remarks>
/// <para>Permite filtrar por tipo de usuario (encordador/jugador), usuario específico, torneo y búsqueda textual.</para>
/// <para>Las propiedades con setter público (<see cref="UserId"/>, <see cref="IsEncorder"/>, <see cref="IsUser"/>, <see cref="TournamentId"/>)
/// son reasignables después de la construcción para permitir al controlador inyectar el UserId del JWT automáticamente.</para>
/// <para>La paginación se controla mediante <see cref="Page"/> y <see cref="Size"/>,
/// con ordenación descendente por defecto (<c>createdAt desc</c>).</para>
/// </remarks>
/// <param name="IsEncorder">Si es <c>true</c>, filtra pedidos donde el usuario autenticado es el encordador asignado.</param>
/// <param name="IsUser">Si es <c>true</c>, filtra pedidos donde el usuario autenticado es el jugador.</param>
/// <param name="UserId">Filtra pedidos por un ULID de usuario específico (en formato string).</param>
/// <param name="TournamentId">Filtra pedidos por torneo.</param>
/// <param name="Search">Término de búsqueda textual.</param>
/// <param name="Page">Número de página (desde 0). Por defecto: 0.</param>
/// <param name="Size">Tamaño de página. Por defecto: 10.</param>
/// <param name="SortBy">Campo de ordenación. Por defecto: <c>createdAt</c>.</param>
/// <param name="Direction">Dirección: <c>asc</c> o <c>desc</c>. Por defecto: <c>desc</c>.</param>
public record FilterPurchasedDto(
    bool? IsEncorder,
    bool? IsUser,
    string? UserId,
    Ulid? TournamentId,
    string Search,
    int Page = 0,
    int Size = 10,
    string SortBy = "createdAt",
    string Direction = "desc") {
    public string? UserId { get; set; } = UserId;
    public bool? IsEncorder { get; set; } = IsEncorder;
    public bool? IsUser { get; set; } = IsUser;
    public Ulid? TournamentId { get; set; } = TournamentId;
};