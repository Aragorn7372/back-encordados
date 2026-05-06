using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;

namespace BackEncordados.Common.Utils;

/// <summary>
/// Atributo para manejar transacciones coordinadas en múltiples DbContexts.
/// Soporta cualquier número de bases de datos simultáneamente.
/// Uso: [Transactional(typeof(PedidosDbContext), typeof(UserDbContext), typeof(TalleresDbContext))]
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
    public TransactionalAttribute(params Type[] contextTypes){
        if (contextTypes == null || contextTypes.Length == 0)
            throw new ArgumentException("Debe proporcionar al menos un tipo de DbContext",
                nameof(contextTypes));

        // Validar que todos los tipos sean DbContext
        var invalidTypes = contextTypes.Where(t => !typeof(DbContext).IsAssignableFrom(t)).ToList();
        if (invalidTypes.Any())
            throw new ArgumentException(
                $"Los siguientes tipos no heredan de DbContext: {string.Join(", ", invalidTypes.Select(t => t.Name))}",
                nameof(contextTypes));

        _contextTypes = contextTypes;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        var transactions = new List<IDbContextTransaction>();

        try {
            // Obtener todas las instancias de DbContext y iniciar transacciones
            foreach (var contextType in _contextTypes) {
                var dbContext = context.HttpContext.RequestServices.GetService(contextType) as DbContext;
                if (dbContext == null)
                    throw new InvalidOperationException($"No se pudo obtener la instancia de {contextType.Name}");

                var transaction = await dbContext.Database.BeginTransactionAsync();
                transactions.Add(transaction);

                Log.Information("Transacción iniciada en {DbContextName}", contextType.Name);
            }

            // Ejecutar la acción
            var resultContext = await next();

            // Si no hay excepción, hacer commit en todas las transacciones
            if (resultContext.Exception == null) {
                foreach (var transaction in transactions) {
                    await transaction.CommitAsync();
                }

                Log.Information("Todas las transacciones fueron confirmadas exitosamente");
            }
            else {
                // Si hay excepción, hacer rollback en todas (en orden inverso para evitar deadlocks)
                var reversedTransactions = transactions.Reverse<IDbContextTransaction>();
                foreach (var transaction in reversedTransactions) {
                    try {
                        await transaction.RollbackAsync();
                    }
                    catch (Exception ex) {
                        Log.Error(ex, "Error durante el rollback de transacción");
                    }
                }

                Log.Warning("Todas las transacciones fueron revertidas por error: {Exception}",
                    resultContext.Exception?.Message);
            }
        }
        finally
        {
            // Garantizar la liberación de todas las transacciones
            foreach (var transaction in transactions) {
                try {
                    await transaction.DisposeAsync();
                }
                catch (Exception ex) {
                    Log.Error(ex, "Error liberando transacción");
                }
            }
        }
    }
}