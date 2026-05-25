using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackEncordados.Common.Database.Helpers;


/// <summary>
/// Interceptor de EF Core que actualiza automáticamente los timestamps <c>CreatedAt</c> y <c>UpdatedAt</c>
/// de todas las entidades que implementan <see cref="ITimestamped"/>.
/// </summary>
/// <remarks>
/// <para><b>Comportamiento por estado:</b></para>
/// <list type="bullet">
///   <item><description><c>Added</c> — establece <c>CreatedAt</c> y <c>UpdatedAt</c> a <c>DateTime.UtcNow</c>.</description></item>
///   <item><description><c>Modified</c> — actualiza solo <c>UpdatedAt</c> a <c>DateTime.UtcNow</c>.</description></item>
/// </list>
/// <para>Si el <c>DbContext</c> es <c>null</c>, la operación se omite silenciosamente.</para>
/// </remarks>
public class TimestampInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Se ejecuta antes de guardar los cambios (operación síncrona).
    /// Actualiza los timestamps de las entidades <see cref="ITimestamped"/> modificadas o agregadas.
    /// </summary>
    /// <param name="eventData">Datos contextuales del evento de guardado.</param>
    /// <param name="result">Resultado de la intercepción previa.</param>
    /// <returns>Resultado de la intercepción para continuar el guardado.</returns>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context == null)
            return base.SavingChanges(eventData, result);

        UpdateTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Se ejecuta antes de guardar los cambios (operación asíncrona).
    /// Actualiza los timestamps de las entidades <see cref="ITimestamped"/> modificadas o agregadas.
    /// </summary>
    /// <param name="eventData">Datos contextuales del evento de guardado.</param>
    /// <param name="result">Resultado de la intercepción previa.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado de la intercepción para continuar el guardado asíncrono.</returns>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context == null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        UpdateTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Recorre el ChangeTracker en busca de entidades <see cref="ITimestamped"/>
    /// y actualiza <c>CreatedAt</c> (si es nuevo) o <c>UpdatedAt</c> (si fue modificado).
    /// </summary>
    /// <param name="context">Contexto de base de datos con las entidades a inspeccionar.</param>
    private static void UpdateTimestamps(DbContext context)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<ITimestamped>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.CreatedAt).CurrentValue = now;
                    entry.Property(e => e.UpdatedAt).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(e => e.UpdatedAt).CurrentValue = now;
                    break;
            }
        }
    }
}

/// <summary>
/// Extension methods para configurar timestamps automáticos en el mapeo de entidades de EF Core.
/// </summary>
/// <remarks>
/// Establece valores por defecto a nivel de base de datos mediante <c>HasDefaultValueSql("CURRENT_TIMESTAMP")</c>
/// para las columnas <c>CreatedAt</c> y <c>UpdatedAt</c>.
/// </remarks>
public static class TimestampExtensions
{
    /// <summary>
    /// Configura las propiedades <c>CreatedAt</c> y <c>UpdatedAt</c> como requeridas
    /// con valor por defecto <c>CURRENT_TIMESTAMP</c> en la base de datos.
    /// </summary>
    /// <param name="entity">Constructor de configuración de la entidad.</param>
    public static void ConfigureTimestamps(this EntityTypeBuilder entity)
    {
        entity.Property("CreatedAt")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.Property("UpdatedAt")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}