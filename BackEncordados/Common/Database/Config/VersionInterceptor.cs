using BackEncordados.Usuarios.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BackEncordados.Common.Database.Config;

/// <summary>
/// Interceptor de EF Core que incrementa automáticamente el token de concurrencia <c>Version</c>
/// de las entidades <see cref="User"/> cada vez que se modifican.
/// </summary>
/// <remarks>
/// <para>Este interceptor se registra en el pipeline de <c>AddDbContext</c> y se ejecuta
/// tanto en operaciones síncronas como asíncronas antes de guardar los cambios en la base de datos.</para>
/// <para><b>Comportamiento:</b></para>
/// <list type="bullet">
///   <item><description>Detecta entidades <see cref="User"/> con estado <c>Modified</c> en el <c>ChangeTracker</c>.</description></item>
///   <item><description>Lee el valor original de la propiedad <c>Version</c> y lo incrementa en 1.</description></item>
///   <item><description>EF Core utiliza este valor en la cláusula <c>WHERE</c> del <c>UPDATE</c> para detectar conflictos de concurrencia optimista.</description></item>
/// </list>
/// <para><b>Relación con <c>UserDbContext</c>:</b></para>
/// <list type="bullet">
///   <item><description>La propiedad <c>Version</c> se configura como <c>IsConcurrencyToken()</c> en <see cref="UserDbContext.OnModelCreating"/>.</description></item>
///   <item><description>Sin este interceptor, el token de concurrencia nunca se incrementaría automáticamente.</description></item>
/// </list>
/// </remarks>
public class VersionInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Se ejecuta antes de guardar los cambios en la base de datos (operación síncrona).
    /// Incrementa el token de concurrencia <c>Version</c> de todas las entidades <see cref="User"/> modificadas.
    /// </summary>
    /// <param name="eventData">Datos contextuales del evento de guardado, incluyendo el <c>DbContext</c> actual.</param>
    /// <param name="result">Resultado de la intercepción previa en el pipeline.</param>
    /// <returns>Resultado de la intercepción para continuar con el proceso de guardado.</returns>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateVersion(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Se ejecuta antes de guardar los cambios en la base de datos (operación asíncrona).
    /// Incrementa el token de concurrencia <c>Version</c> de todas las entidades <see cref="User"/> modificadas.
    /// </summary>
    /// <param name="eventData">Datos contextuales del evento de guardado, incluyendo el <c>DbContext</c> actual.</param>
    /// <param name="result">Resultado de la intercepción previa en el pipeline.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
    /// <returns>Resultado de la intercepción para continuar con el proceso de guardado asíncrono.</returns>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateVersion(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Incrementa en 1 la propiedad <c>Version</c> de todas las entidades <see cref="User"/>
    /// marcadas como <c>Modified</c> en el ChangeTracker.
    /// </summary>
    /// <remarks>
    /// <para>Método compartido por las variantes síncrona y asíncrona del interceptor.</para>
    /// <para>Si el contexto es <c>null</c>, la operación se omite silenciosamente (defensa contra contextos no inicializados).</para>
    /// </remarks>
    /// <param name="context">Contexto de base de datos del cual se extraen las entidades modificadas. Puede ser <c>null</c>.</param>
    private static void UpdateVersion(DbContext? context)
    {
        if (context is null) return;

        var entries = context.ChangeTracker
            .Entries<User>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var prop = entry.Property(x => x.Version);
            prop.CurrentValue = (long)prop.OriginalValue + 1;
        }
    }
}