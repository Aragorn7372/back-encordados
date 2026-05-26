using CSharpFunctionalExtensions;

namespace BackEncordados.Common.Utils;

/// <summary>
/// Extensiones utilitarias para el patrón Result de CSharpFunctionalExtensions.
/// </summary>
public static class TapAsyncClass {
    /// <summary>
    /// Ejecuta un efecto secundario asíncrono sobre el valor de un <see cref="Result{T,E}"/> exitoso,
    /// retornando el mismo Result sin modificarlo.
    /// </summary>
    /// <remarks>
    /// <para>Inspirado en el operador <c>tap</c> de Rust y Elm. Permite encadenar efectos secundarios
    /// (logging, envío de notificaciones, invalidación de caché) dentro de una cadena funcional
    /// sin alterar el flujo del Result.</para>
    /// <para>Ejemplo de uso:</para>
    /// <code>
    /// await GetUser(id)
    ///     .TapAsync(user =&gt; _logger.LogInformation("User loaded: {Id}", user.Id))
    ///     .TapAsync(user =&gt; _cache.RemoveAsync($"user_{user.Id}"));
    /// </code>
    /// </remarks>
    /// <typeparam name="T">Tipo del valor en caso de éxito.</typeparam>
    /// <typeparam name="E">Tipo del error en caso de fallo.</typeparam>
    /// <param name="result">Result sobre el que actuar.</param>
    /// <param name="func">Función asíncrona a ejecutar si el Result es exitoso.</param>
    /// <returns>El mismo <paramref name="result"/> sin modificar.</returns>
    public static async Task<Result<T, E>> TapAsync<T, E>(
        this Result<T, E> result,
        Func<T, Task> func){
        if (result.IsSuccess)
            await func(result.Value);

        return result;
    }
}