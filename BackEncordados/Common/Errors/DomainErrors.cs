namespace BackEncordados.Common.Errors;

/// <summary>
/// Record base para todos los errores de dominio de la aplicación.
/// Implementa el patrón Result mediante <c>CSharpFunctionalExtensions</c>,
/// permitiendo propagar errores de forma explícita como tipos en lugar de excepciones.
/// </summary>
/// <remarks>
/// <para>Todos los errores específicos del dominio heredan de este record, formando
/// una jerarquía que permite al <see cref="GlobalExceptionHandler"/> mapear cada tipo
/// a un código HTTP específico (400, 401, 404, 409, 422, 500).</para>
///
/// <para><b>Jerarquía de errores:</b></para>
/// <list type="bullet">
///   <item><description><c>AuthError</c> → autenticación y autorización (401, 409, 400).</description></item>
///   <item><description><c>CuerdaError</c> → cuerdas/tensores (404, 409, 400).</description></item>
///   <item><description><c>MaterialError</c> → materiales (404, 409, 400).</description></item>
///   <item><description><c>PurchasedErrors</c> → pedidos (404, 409, 400).</description></item>
///   <item><description><c>TournamentsErrors</c> → torneos (404, 409, 400).</description></item>
/// </list>
///
/// <para><b>Uso típico con Result Pattern:</b></para>
/// <code>
/// return Result.Failure&lt;T, CuerdaNotFoundError&gt;(new CuerdaNotFoundError());
/// </code>
/// </remarks>
/// <param name="Error">Mensaje descriptivo del error ocurrido. Se propaga a través de la jerarquía de herencia.</param>
public record DomainErrors(string Error)
{
    /// <summary>
    /// Mensaje descriptivo del error ocurrido.
    /// Se establece en el constructor base y puede ser sobrescrito por los registros derivados.
    /// Es el único campo expuesto en las respuestas de error de la API.
    /// </summary>
    /// <example>"Cuerda not found", "Credenciales inválidas", "El torneo no existe"</example>
    public string Error { get; set; } = Error;
};