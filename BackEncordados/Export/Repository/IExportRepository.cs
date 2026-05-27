using BackEncordados.Export.Dto;

namespace BackEncordados.Export.Repository;

/// <summary>
/// Contrato del repositorio para la exportación e importación global
/// de todas las entidades de la base de datos.
/// </summary>
/// <remarks>
/// <para>Opera sobre los cuatro DbContexts de la aplicación para obtener,
/// limpiar y reimportar todos los datos del sistema. Utilizado por
/// <see cref="IExportService"/> para las operaciones de respaldo y restauración.</para>
/// <para><b>Métodos del contrato:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Retorno</description>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>GetAllDataAsync</c></term>
///     <description><c>Task&lt;ExportDataDto&gt;</c></description>
///     <description>Obtiene todas las entidades de los cuatro DbContexts con <c>IgnoreQueryFilters()</c>
///     para incluir registros soft-deleted, y carga <c>PedidoLineas</c> por separado para
///     asignarlas manualmente a cada pedido.</description>
///   </item>
///   <item>
///     <term><c>ClearAllDataAsync</c></term>
///     <description><c>Task</c></description>
///     <description>Elimina todos los datos en orden inverso de dependencias.
///     Usa estrategias diferentes según el proveedor de base de datos
///     (InMemory → RemoveRange, producción → ExecuteDeleteAsync).</description>
///   </item>
///   <item>
///     <term><c>ImportDataAsync</c></term>
///     <description><c>Task</c></description>
///     <description>Importa datos en orden correcto respetando FK: torneos primero
///     (con manejo especial de WorkerMachineAssignments), luego users, materials,
///     cuerdas y finalmente pedidos.</description>
///   </item>
/// </list>
/// <para><b>Orden de operaciones:</b></para>
/// <list type="bullet">
///   <item><description>Exportación: no requiere orden específico (solo lectura).</description></item>
///   <item><description>Limpieza: orden inverso (Pedidos → Materials/Cuerdas → Users → Tournaments).</description></item>
///   <item><description>Importación: orden directo (Tournaments → Users → Materials → Cuerdas → Pedidos).</description></item>
/// </list>
/// </remarks>
public interface IExportRepository
{
    /// <summary>
    /// Obtiene todas las entidades de la base de datos para exportación.
    /// </summary>
    /// <remarks>
    /// <para>Consulta los cuatro DbContexts usando <c>IgnoreQueryFilters()</c> para
    /// incluir registros marcados como eliminados (soft delete). Usa <c>AsNoTracking()</c>
    /// en todas las consultas por ser de solo lectura.</para>
    /// <para>Las líneas de pedido (<c>PedidoLineas</c>) se cargan por separado en un
    /// <c>Lookup</c> y se asignan manualmente a cada pedido, en lugar de usar
    /// <c>Include()</c>, para evitar arrastrar relaciones de navegación circulares
    /// que complican la serialización.</para>
    /// </remarks>
    /// <returns>DTO con todas las listas de entidades pobladas.</returns>
    Task<ExportDataDto> GetAllDataAsync();

    /// <summary>
    /// Elimina todos los datos de la base de datos en orden inverso de dependencias.
    /// </summary>
    /// <remarks>
    /// <para>Selecciona automáticamente la estrategia de borrado según el proveedor
    /// de base de datos:</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Proveedor</term>
    ///     <description>Estrategia</description>
    ///     <description>Método</description>
    ///   </listheader>
    ///   <item>
    ///     <term>InMemory</term>
    ///     <description><c>RemoveRange</c> + <c>SaveChangesAsync</c></description>
    ///     <description><c>ClearAllDataInMemoryAsync</c></description>
    ///   </item>
    ///   <item>
    ///     <term>Producción (PostgreSQL, SQL Server, etc.)</term>
    ///     <description><c>ExecuteDeleteAsync</c> para tablas sin restricciones FK batch,
    ///     <c>RemoveRange</c> + <c>SaveChangesAsync</c> para tablas con dependencias</description>
    ///     <description><c>ClearAllDataProductionAsync</c></description>
    ///   </item>
    /// </list>
    /// <para><b>Orden de eliminación:</b> Pedidos (y líneas) → Materials/Cuerdas → Users → Tournaments.
    /// Este orden respeta las restricciones de clave foránea.</para>
    /// </remarks>
    Task ClearAllDataAsync();

    /// <summary>
    /// Importa todas las entidades en la base de datos respetando el orden de dependencias.
    /// </summary>
    /// <remarks>
    /// <para><b>Orden de importación:</b></para>
    /// <list type="number">
    ///   <item><description><b>Torneos</b> — Primero porque otras entidades (Users) pueden referenciarlos.
    ///   Requiere manejo especial de <c>WorkerMachineAssignments</c> (OwnsMany): se extraen
    ///   antes de insertar el torneo y se reasignan post-insert con IDs numéricos secuenciales,
    ///   ya que el <c>ValueConverter</c> de OwnsMany requiere IDs únicos.</description></item>
    ///   <item><description><b>Usuarios</b> — Después de torneos. Pueden tener <c>TournamentId</c> opcional.</description></item>
    ///   <item><description><b>Materiales</b> — No tienen dependencias de otras entidades recién insertadas.</description></item>
    ///   <item><description><b>Cuerdas</b> — No tienen dependencias de otras entidades recién insertadas.</description></item>
    ///   <item><description><b>Pedidos</b> — Último porque dependen de usuarios (PlayerId, AssignedTo)
    ///   y torneos (TournamentId). Incluyen líneas de pedido anidadas.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="data">DTO con todas las listas de entidades a importar.</param>
    Task ImportDataAsync(ExportDataDto data);

    /// <summary>
    /// Limpia el ChangeTracker de los cuatro DbContexts para eliminar cualquier
    /// referencia residual a entidades previamente trackeadas.
    /// </summary>
    /// <remarks>
    /// <para>Debe llamarse entre <see cref="ClearAllDataAsync"/> e <see cref="ImportDataAsync"/>
    /// para evitar conflictos de keys duplicadas en el change tracker de EF Core
    /// al reinsertar entidades con los mismos IDs que las recién eliminadas.</para>
    /// </remarks>
    void ClearChangeTrackers();
}