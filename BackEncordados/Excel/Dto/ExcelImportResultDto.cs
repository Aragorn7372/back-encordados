namespace BackEncordados.Excel.Dto;

/// <summary>
/// DTO que encapsula el resultado de una operación de importación desde Excel.
/// </summary>
/// <remarks>
/// <para>Proporciona contadores detallados de registros creados y actualizados
/// por cada módulo del sistema, más una lista de errores ocurridos durante
/// el proceso.</para>
/// <para><b>Estructura de contadores:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Módulo</term>
///     <description>Creados</description>
///     <description>Actualizados</description>
///   </listheader>
///   <item>
///     <term>Usuarios</term>
///     <description><see cref="UsersCreated"/></description>
///     <description><see cref="UsersUpdated"/></description>
///   </item>
///   <item>
///     <term>Materiales</term>
///     <description><see cref="MaterialsCreated"/></description>
///     <description><see cref="MaterialsUpdated"/></description>
///   </item>
///   <item>
///     <term>Cuerdas</term>
///     <description><see cref="CuerdasCreated"/></description>
///     <description><see cref="CuerdasUpdated"/></description>
///   </item>
///   <item>
///     <term>Torneos</term>
///     <description><see cref="TournamentsCreated"/></description>
///     <description><see cref="TournamentsUpdated"/></description>
///   </item>
///   <item>
///     <term>Pedidos</term>
///     <description><see cref="PedidosCreated"/></description>
///     <description><see cref="PedidosUpdated"/></description>
///   </item>
///   <item>
///     <term>Líneas Pedido</term>
///     <description>—</description>
///     <description><see cref="PedidosLineasUpdated"/></description>
///   </item>
/// </list>
/// <para>Las líneas de pedido solo se actualizan (nunca se crean desde importación),
/// por lo que no tienen contador <c>Created</c>.</para>
/// </remarks>
public class ExcelImportResultDto
{
    /// <summary>Número de usuarios nuevos creados durante la importación.</summary>
    public int UsersCreated { get; set; }

    /// <summary>Número de usuarios existentes actualizados durante la importación.</summary>
    public int UsersUpdated { get; set; }

    /// <summary>Número de materiales nuevos creados durante la importación.</summary>
    public int MaterialsCreated { get; set; }

    /// <summary>Número de materiales existentes actualizados durante la importación.</summary>
    public int MaterialsUpdated { get; set; }

    /// <summary>Número de cuerdas nuevas creadas durante la importación.</summary>
    public int CuerdasCreated { get; set; }

    /// <summary>Número de cuerdas existentes actualizadas durante la importación.</summary>
    public int CuerdasUpdated { get; set; }

    /// <summary>Número de torneos nuevos creados durante la importación.</summary>
    public int TournamentsCreated { get; set; }

    /// <summary>Número de torneos existentes actualizados durante la importación.</summary>
    public int TournamentsUpdated { get; set; }

    /// <summary>Número de pedidos nuevos creados durante la importación.</summary>
    public int PedidosCreated { get; set; }

    /// <summary>Número de pedidos existentes actualizados durante la importación.</summary>
    public int PedidosUpdated { get; set; }

    /// <summary>Número de líneas de pedido actualizadas (las líneas no se crean en importación).</summary>
    public int PedidosLineasUpdated { get; set; }

    /// <summary>Lista de mensajes de error ocurridos durante el proceso de importación.</summary>
    public List<string> Errors { get; set; } = new();
}