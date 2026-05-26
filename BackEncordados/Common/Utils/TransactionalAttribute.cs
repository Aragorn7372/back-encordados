using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;

namespace BackEncordados.Common.Utils;

/// <summary>
/// Atributo de filtro de acción que coordina transacciones en múltiples DbContexts
/// con detección híbrida de errores (excepciones HTTP y Result.Failure vía status codes).
/// </summary>
/// <remarks>
/// <para>Implementa un patrón de transacción distribuida manual (coordinada) sobre DbContexts
/// independientes que comparten la misma base de datos SQL Server. No utiliza
/// DistributedTransactionCoordinator (MSDTC), sino que inicia transacciones separadas
/// y las confirma/revierte al unísono.</para>
///
/// <para><b>Flujo de ejecución:</b></para>
/// <list type="number">
///   <item><description>Inicializa una lista vacía de transacciones.</description></item>
///   <item><description>Para cada DbContext configurado, obtiene la instancia del DI, verifica
///   si es InMemory (salta), e inicia <c>BeginTransactionAsync</c>.</description></item>
///   <item><description>Ejecuta la acción del endpoint mediante <c>next()</c>.</description></item>
///   <item><description>Detecta error: excepción no manejada O status code HTTP ≥400 en la respuesta.</description></item>
///   <item><description>Sin error → <b>Commit</b> de todas las transacciones en orden.</description></item>
///   <item><description>Con error → <b>Rollback</b> en orden inverso (para evitar deadlocks),
///   con try/catch individual por si falla un rollback.</description></item>
///   <item><description>En <c>finally</c> → <c>DisposeAsync</c> de todas las transacciones (liberación garantizada).</description></item>
/// </list>
///
/// <para><b>Casos especiales:</b></para>
/// <list type="bullet">
///   <item><description><b>InMemory Database:</b> Durante desarrollo/tests, las bases de datos
///   en memoria no soportan transacciones. Se saltan con log Debug.</description></item>
///   <item><description><b>Rollback parcial:</b> Si un rollback falla (ej: conexión perdida),
///   se loggea el error y se continúa con las demás transacciones.</description></item>
/// </list>
///
/// <para><b>Detección híbrida de errores (<see cref="HasResultFailure"/>):</b></para>
/// <list type="bullet">
///   <item><description>Excepción no manejada en <c>context.Exception</c> → rollback.</description></item>
///   <item><description><c>ObjectResult</c> con <c>StatusCode ≥ 400</c> → rollback.</description></item>
///   <item><description><c>BadRequestResult</c> (400), <c>NotFoundResult</c> (404) → rollback.</description></item>
///   <item><description><c>StatusCodeResult</c> con código ≥ 400 → rollback.</description></item>
///   <item><description>Cualquier otro resultado (2xx, 3xx) → commit.</description></item>
/// </list>
///
/// <para>Uso: <c>[Transactional(typeof(PedidosDbContext), typeof(UserDbContext))]</c></para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class TransactionalAttribute : ActionFilterAttribute
{
    private readonly Type[] _contextTypes;

    /// <summary>
    /// Inicializa el atributo validando los tipos de DbContext proporcionados.
    /// </summary>
    /// <param name="contextTypes">Tipos de DbContext que deben participar en la transacción coordinada.</param>
    /// <exception cref="ArgumentException">
    /// Si no se proporcionan contextos o alguno de los tipos no hereda de <see cref="DbContext"/>.
    /// </exception>
    public TransactionalAttribute(params Type[] contextTypes)
    {
        if (contextTypes == null || contextTypes.Length == 0)
            throw new ArgumentException("Debe proporcionar al menos un tipo de DbContext",
                nameof(contextTypes));

        var invalidTypes = contextTypes.Where(t => !typeof(DbContext).IsAssignableFrom(t)).ToList();
        if (invalidTypes.Any())
            throw new ArgumentException(
                $"Los siguientes tipos no heredan de DbContext: {string.Join(", ", invalidTypes.Select(t => t.Name))}",
                nameof(contextTypes));

        _contextTypes = contextTypes;
    }

    /// <summary>
    /// Hook principal del filtro que gestiona el ciclo de vida completo de las transacciones multi-DbContext.
    /// </summary>
    /// <remarks>
    /// <para>Ver flujo numerado en la documentación de la clase.</para>
    /// <para>Libera todos los recursos transaccionales en el bloque <c>finally</c>,
    /// asegurando que no queden transacciones abiertas incluso si ocurre una excepción
    /// durante el commit o rollback.</para>
    /// </remarks>
    /// <param name="context">Contexto de ejecución de la acción (contiene HttpContext, ModelState, etc.).</param>
    /// <param name="next">Delegado para ejecutar la acción y obtener el resultado.</param>
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var transactions = new List<(IDbContextTransaction Transaction, Type ContextType)>();

        try
        {
            // Obtener todas las instancias de DbContext e iniciar transacciones
            foreach (var contextType in _contextTypes)
            {
                var dbContext = context.HttpContext.RequestServices.GetService(contextType) as DbContext;
                if (dbContext == null)
                    throw new InvalidOperationException($"No se pudo obtener la instancia de {contextType.Name}");

                // Skip transactions for in-memory database (dev mode)
                if (dbContext.Database.IsInMemory())
                {
                    Log.Debug("In-memory database detectada, saltando transacción para {DbContextName}", contextType.Name);
                    continue;
                }

                var transaction = await dbContext.Database.BeginTransactionAsync();
                transactions.Add((transaction, contextType));

                Log.Information("Transacción iniciada en {DbContextName}", contextType.Name);
            }

            // Ejecutar la acción
            var resultContext = await next();

            // Detectar si hay excepción o Result.Failure (HÍBRIDO)
            bool hasError = resultContext.Exception != null || HasResultFailure(resultContext);

            if (!hasError)
            {
                // COMMIT: Si todo está bien
                foreach (var (transaction, contextType) in transactions)
                {
                    await transaction.CommitAsync();
                    Log.Information("Transacción confirmada para {DbContextName}", contextType.Name);
                }

                Log.Information("Todas las transacciones fueron confirmadas exitosamente");
            }
            else
            {
                // ROLLBACK: Si hay excepción o Result.Failure
                var reversedTransactions = transactions.AsEnumerable().Reverse();
                foreach (var (transaction, contextType) in reversedTransactions)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                        Log.Warning("Transacción revertida para {DbContextName}", contextType.Name);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error durante el rollback de transacción en {DbContextName}", contextType.Name);
                    }
                }

                if (resultContext.Exception != null)
                    Log.Warning("Transacciones revertidas por excepción: {Exception}",
                        resultContext.Exception?.Message);
                else
                    Log.Warning("Transacciones revertidas por Result.Failure");
            }
        }
        finally
        {
            // Garantizar la liberación de todas las transacciones
            foreach (var (transaction, contextType) in transactions)
            {
                try
                {
                    await transaction.DisposeAsync();
                    Log.Debug("Transacción liberada para {DbContextName}", contextType.Name);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error liberando transacción para {DbContextName}", contextType.Name);
                }
            }
        }
    }

    /// <summary>
    /// Detecta si la respuesta de la acción indica un error que debe provocar rollback.
    /// </summary>
    /// <remarks>
    /// <para>Evalúa tres condiciones en orden:</para>
    /// <list type="number">
    ///   <item><description><b>Excepción:</b> <c>context.Exception != null</c> → error.</description></item>
    ///   <item><description><b>ObjectResult con status HTTP:</b> si <c>StatusCode ≥ 400</c> → error.</description></item>
    ///   <item><description><b>Resultados de error específicos:</b> <c>BadRequestResult</c> (400),
    ///   <c>NotFoundResult</c> (404), <c>StatusCodeResult ≥ 400</c> → error.</description></item>
    /// </list>
    /// <para>Si ocurre una excepción durante la detección (catch general), se asume éxito
    /// y se permite el commit para no bloquear la operación.</para>
    /// </remarks>
    /// <param name="context">Contexto de resultado de la acción ejecutada.</param>
    /// <returns><c>true</c> si hay error y debe hacerse rollback; <c>false</c> para commit.</returns>
    private bool HasResultFailure(ActionExecutedContext context)
    {
        try
        {
            if (context.Exception != null)
                return true;

            if (context.Result is ObjectResult objResult)
            {
                if (objResult.StatusCode.HasValue && objResult.StatusCode >= 400)
                {
                    Log.Debug("Detectado error HTTP {StatusCode} en respuesta", objResult.StatusCode);
                    return true;
                }
            }

            if (context.Result is BadRequestResult or NotFoundResult)
            {
                Log.Debug("Detectado resultado de error: {ResultType}", context.Result.GetType().Name);
                return true;
            }

            if (context.Result is StatusCodeResult statusCodeResult && statusCodeResult.StatusCode >= 400)
            {
                Log.Debug("Detectado StatusCodeResult {StatusCode}", statusCodeResult.StatusCode);
                return true;
            }

            Log.Debug("Detectado resultado exitoso: {ResultType}", context.Result?.GetType().Name ?? "null");
            return false;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error detectando fallo; continuando con commit");
            return false;
        }
    }
}