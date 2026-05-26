namespace BackEncordados.Usuarios.Dto;

/// <summary>
/// DTO de filtro y paginación para la consulta de usuarios.
/// </summary>
/// <remarks>
/// <para>Permite filtrar por tipo de usuario (<c>FindUsers</c>, <c>FindEncorders</c>, <c>FindSupervisors</c>),
/// torneo específico y término de búsqueda textual.</para>
/// <para>La paginación se controla mediante <see cref="Page"/> y <see cref="Size"/>,
/// con ordenación configurable por <see cref="SortBy"/> y <see cref="Direction"/>.</para>
/// <para>Los campos de tipo <c>bool?</c> actúan como filtros opcionales: si son <c>null</c> no filtran;
/// si son <c>true</c> incluyen ese tipo. Múltiples filtros verdaderos se combinan con OR lógico.</para>
/// </remarks>
/// <param name="FindUsers">Si es <c>true</c>, incluye usuarios con rol USER en los resultados.</param>
/// <param name="FindEncorders">Si es <c>true</c>, incluye usuarios con rol ENCORDER en los resultados.</param>
/// <param name="FindSupervisors">Si es <c>true</c>, incluye usuarios con rol SUPERVISOR en los resultados.</param>
/// <param name="TournamentId">Si se proporciona, filtra usuarios asociados al torneo con este ULID.</param>
/// <param name="Search">Término de búsqueda textual aplicado sobre nombre, email o username.</param>
/// <param name="Page">Número de página (empezando desde 0). Valor por defecto: 0.</param>
/// <param name="Size">Tamaño de página. Valor por defecto: 10.</param>
/// <param name="SortBy">Campo por el que ordenar. Valores típicos: <c>name</c>, <c>email</c>, <c>username</c>. Por defecto: <c>name</c>.</param>
/// <param name="Direction">Dirección de ordenación: <c>asc</c> o <c>desc</c>. Por defecto: <c>asc</c>.</param>
public record FilterUserDto(
    bool? FindUsers,
    bool? FindEncorders,
    bool? FindSupervisors,
    Ulid? TournamentId,
    string Search,
    int Page = 0,
    int Size = 10,
    string SortBy = "name",
    string Direction = "asc");
