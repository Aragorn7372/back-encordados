namespace BackEncordados.Materials.Errors;

/// <summary>
/// Record base para errores del módulo de cuerdas/tensores (entidad <see cref="Cuerdas"/>).
/// Agrupa todos los errores relacionados con el catálogo de cuerdas: búsqueda,
/// creación, actualización y eliminación de registros de cuerdas.
/// </summary>
/// <remarks>
/// <para>Subtipos que heredan de este record:</para>
/// <list type="bullet">
///   <item><description><see cref="ConflictError"/> — HTTP 409, duplicados o violación de integridad referencial.</description></item>
///   <item><description><see cref="CuerdaNotFoundError"/> — HTTP 404, cuerda no encontrada por Id.</description></item>
///   <item><description><see cref="ValidationError"/> — HTTP 400, datos inválidos (marca vacía, precio negativo, etc.).</description></item>
/// </list>
/// <para>Estos errores son lanzados por <c>CuerdaService</c> y capturados por <see cref="GlobalExceptionHandler"/>.</para>
/// </remarks>
/// <param name="Error">Mensaje descriptivo del error específico de cuerdas.</param>
public record CuerdaError(string Error)
{
    public string Error {get; set;} = Error;
};

/// <summary>
/// Error de conflicto para cuerdas. Se produce cuando se intenta crear o actualizar
/// una cuerda que duplica una ya existente, o cuando se viola una restricción de integridad
/// referencial (ej: eliminar una cuerda referenciada por un pedido).
/// </summary>
/// <remarks>
/// Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 409 Conflict.
/// </remarks>
/// <example>new ConflictError("Ya existe una cuerda con la misma marca y modelo")</example>
public record ConflictError(string Error):CuerdaError(Error);

/// <summary>
/// Error cuando no se encuentra una cuerda en el catálogo.
/// Se produce al buscar, actualizar o eliminar una cuerda por Id que no existe en la base de datos.
/// </summary>
/// <remarks>
/// <para>Mensaje por defecto: "Cuerda not found".</para>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 404 Not Found.</para>
/// </remarks>
/// <example>new CuerdaNotFoundError("Cuerda con Id 999 no encontrada")</example>
public record CuerdaNotFoundError(string Error="Cuerda not found"):CuerdaError(Error);

/// <summary>
/// Error de validación para datos inválidos de cuerdas.
/// Se produce cuando los datos de entrada no cumplen las reglas de validación del dominio.
/// </summary>
/// <remarks>
/// <para>Validaciones que pueden disparar este error:</para>
/// <list type="bullet">
///   <item><description><c>Marca</c> vacía o mayor a 100 caracteres.</description></item>
///   <item><description><c>Modelo</c> vacío o mayor a 100 caracteres.</description></item>
///   <item><description><c>Stock</c> negativo.</description></item>
///   <item><description><c>Precio</c> negativo o cero.</description></item>
///   <item><description><c>StringFormat</c> o <c>StringsType</c> inválidos.</description></item>
/// </list>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 400 Bad Request.</para>
/// </remarks>
/// <param name="Error">Mensaje describiendo la validación fallida específica.</param>
/// <example>new ValidationError("El campo Marca es obligatorio")</example>
public record ValidationError(string Error):CuerdaError(Error);

