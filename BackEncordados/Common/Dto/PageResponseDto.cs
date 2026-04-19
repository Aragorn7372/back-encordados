namespace BackEncordados.Common.Dto;

/// <summary>
/// Record genérico para respuestas paginadas.
/// </summary>
/// <remarks>
/// Se utiliza como tipo de retorno en endpoints que devuelven listas paginadas.
/// Proporciona metadatos de paginación además del contenido.
///
/// <para><b>Propiedades calculadas:</b></para>
/// <list type="bullet">
///     <item>Empty: true si no hay contenido</item>
///     <item>First: true si es la primera página</item>
///     <item>Last: true si es la última página</item>
/// </list>
/// </remarks>
/// <typeparam name="T">Tipo de los elementos en el contenido.</typeparam>
public record PageResponseDto<T>(
    /// <summary>
    /// Lista de elementos de la página actual.
    /// </summary>
    List<T> Content,
    
    /// <summary>
    /// Total de páginas disponibles.
    /// </summary>
    int TotalPages,
    
    /// <summary>
    /// Total de elementos sin paginar.
    /// </summary>
    long TotalElements,
    
    /// <summary>
    /// Tamaño de página solicitado.
    /// </summary>
    int PageSize,
    
    /// <summary>
    /// Número de página actual (0-indexed).
    /// </summary>
    int PageNumber,
    
    /// <summary>
    /// Cantidad de elementos en esta página.
    /// </summary>
    int TotalPageElements,
    
    /// <summary>
    /// Campo utilizado para ordenar.
    /// </summary>
    string SortBy,
    
    /// <summary>
    /// Dirección de ordenación.
    /// </summary>
    string Direction
)
{
    /// <summary>
    /// Indica si la respuesta está vacía.
    /// </summary>
    public bool Empty => Content.Count == 0;
    
    /// <summary>
    /// Indica si es la primera página.
    /// </summary>
    public bool First => PageNumber == 0;
    
    /// <summary>
    /// Indica si es la última página.
    /// </summary>
    public bool Last => PageNumber >= TotalPages - 1;
}