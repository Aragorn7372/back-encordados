namespace BackEncordados.Excel.Dto;

/// <summary>
/// DTO de solicitud para la exportación avanzada de datos de un torneo a Excel.
/// </summary>
/// <remarks>
/// <para>Permite al cliente especificar qué módulos exportar y qué campos incluir
/// en cada hoja del archivo Excel.</para>
/// <para><b>Valores válidos para <see cref="Types"/>:</b></para>
/// <list type="bullet">
///   <item><description><c>"users"</c> — Usuarios del torneo.</description></item>
///   <item><description><c>"materials"</c> — Materiales de encordado.</description></item>
///   <item><description><c>"cuerdas"</c> — Cuerdas.</description></item>
///   <item><description><c>"tournament"</c> — Datos generales del torneo.</description></item>
///   <item><description><c>"pedidos"</c> — Pedidos y líneas de pedido.</description></item>
/// </list>
/// <para>Si <see cref="Types"/> está vacío, se exportan todos los módulos por defecto.</para>
/// </remarks>
public class ExcelAdvancedRequestDto
{
    /// <summary>ID del torneo a exportar.</summary>
    public Ulid TournamentId { get; set; }

    /// <summary>Lista de tipos de datos a incluir en la exportación.
    /// Valores posibles: "users", "materials", "cuerdas", "tournament", "pedidos".</summary>
    public List<string> Types { get; set; } = new() { "users", "materials", "cuerdas", "tournament", "pedidos" };

    /// <summary>Diccionario opcional para filtrar campos específicos por tipo de dato.
    /// La clave es el nombre del tipo, el valor es la lista de campos a incluir.</summary>
    public Dictionary<string, List<string>>? Fields { get; set; }
}