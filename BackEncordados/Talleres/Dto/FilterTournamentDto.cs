namespace BackEncordados.Talleres.Dto;

/// <summary>
/// DTO de filtro y paginación para la consulta de torneos.
/// </summary>
/// <remarks>
/// <para>Permite filtrar por término de búsqueda textual y por usuario (supervisor/owner) asignado.</para>
/// <para>La paginación se controla mediante <see cref="Page"/> y <see cref="Size"/>,
/// con ordenación configurable por <see cref="SortBy"/> y <see cref="Direction"/>.</para>
/// <para>La propiedad <see cref="UserId"/> es inicializable desde el constructor
/// y también tiene setter público para permitir asignación posterior.</para>
/// </remarks>
/// <param name="Search">Término de búsqueda textual aplicado sobre nombre del torneo.</param>
/// <param name="UserId">Filtro opcional por usuario (ULID) asignado al torneo (supervisor/owner).</param>
/// <param name="Page">Número de página (empezando desde 0). Por defecto: 0.</param>
/// <param name="Size">Tamaño de página. Por defecto: 10.</param>
/// <param name="SortBy">Campo por el que ordenar. Valores típicos: <c>name</c>, <c>startDate</c>, <c>endDate</c>. Por defecto: <c>name</c>.</param>
/// <param name="Direction">Dirección de ordenación: <c>asc</c> o <c>desc</c>. Por defecto: <c>asc</c>.</param>
public record FilterTournamentDto(
    string Search,
    Ulid? UserId,
    int Page = 0,
    int Size = 10,
    string SortBy = "name",
    string Direction = "asc") {
    public Ulid? UserId { get; set; } = UserId;
};