namespace BackEncordados.Materials.Dto.Materials;

/// <summary>
/// DTO de filtro para la búsqueda paginada de materiales.
/// </summary>
/// <remarks>
/// <para>Se utiliza en el endpoint GET del <c>MaterialsController</c>
/// para aplicar filtros, paginación y ordenamiento a la consulta de materiales.</para>
/// <para><b>Campos:</b></para>
/// <list type="bullet">
///   <item><description><c>TournamentId</c> — filtra por torneo específico (opcional).</description></item>
///   <item><description><c>Search</c> — término de búsqueda general (opcional, vacío por defecto).</description></item>
///   <item><description><c>Page</c> — número de página (0-indexed, default 0).</description></item>
///   <item><description><c>Size</c> — tamaño de página (default 10).</description></item>
///   <item><description><c>SortBy</c> — campo de ordenamiento (default "name").</description></item>
///   <item><description><c>Direction</c> — dirección "asc" o "desc" (default "asc").</description></item>
/// </list>
/// </remarks>
public record MaterialFilterDto(
    /// <summary>Filtro opcional por ID de torneo.</summary>
    Ulid? TournamentId,

    /// <summary>Término de búsqueda general (opcional).</summary>
    string Search,

    /// <summary>Número de página (0-indexed, default 0).</summary>
    int Page = 0,

    /// <summary>Tamaño de página (default 10).</summary>
    int Size = 10,
    
    /// <summary>Campo para ordenar resultados (default "name").</summary>
    string SortBy = "name",
    
    /// <summary>Dirección de ordenamiento: "asc" o "desc" (default "asc").</summary>
    string Direction = "asc");