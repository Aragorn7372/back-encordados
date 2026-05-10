using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;

namespace BackEncordados.Common.Utils;

/// <summary>
/// Atributo para manejar transacciones coordinadas en múltiples DbContexts (HÍBRIDO).
/// Detecta EXCEPCIONES y RESULT.FAILURE para rollback automático.
/// Soporta cualquier número de bases de datos simultáneamente.
/// Uso: [Transactional(typeof(PedidosDbContext), typeof(UserDbContext))]
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TransactionalAttribute : ActionFilterAttribute
{
    private readonly Type[] _contextTypes;

    /// <summary>
    /// Inicializa el atributo con los tipos de DbContext a transaccionalizar.
    /// </summary>
    /// <param name="contextTypes">Tipos de DbContext que deben participar en la transacción.</param>
    /// <exception cref="ArgumentException">Si no se proporcionan contextos o alguno no es DbContext.</exception>
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
    /// Detecta si hay error basado en status code HTTP o excepción.
    /// Status 2xx = éxito (commit)
    /// Status 4xx/5xx = error (rollback)
    /// Excepciones = error (rollback)
    /// </summary>
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