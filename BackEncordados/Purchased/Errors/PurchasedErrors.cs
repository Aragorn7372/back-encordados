using BackEncordados.Common.Errors;

namespace BackEncordados.Purchased.Errors;

/// <summary>
/// Record base para errores del módulo de pedidos (entidades <see cref="Pedidos"/> y <see cref="PedidoLinea"/>).
/// Agrupa errores relacionados con la creación, modificación y gestión de pedidos de encordado,
/// incluyendo validación de estados, concurrencia y conflictos de integridad.
/// </summary>
/// <remarks>
/// <para>Subtipos que heredan de este record:</para>
/// <list type="bullet">
///   <item><description><see cref="ConflictError"/> — HTTP 409, duplicados o integridad referencial.</description></item>
///   <item><description><see cref="PurchasedNotFoundError"/> — HTTP 404, pedido no encontrado.</description></item>
///   <item><description><see cref="ValidationError"/> — HTTP 400, datos inválidos.</description></item>
///   <item><description><see cref="InvalidStatusError"/> — HTTP 400, transición de estado no permitida.</description></item>
///   <item><description><see cref="ConcurrencyError"/> — HTTP 409, conflicto de concurrencia optimista (token Version).</description></item>
/// </list>
/// <para>Estos errores son lanzados por <c>PedidoService</c> y capturados por <see cref="GlobalExceptionHandler"/>.</para>
/// </remarks>
/// <param name="Error">Mensaje descriptivo del error específico de pedidos.</param>
public record PurchasedErrors(string Error): DomainErrors(Error);

/// <summary>
/// Error de conflicto para pedidos. Se produce al intentar crear un pedido
/// que duplica uno existente o al violar restricciones de integridad referencial
/// (ej: crear una línea de pedido con un pedido padre inexistente).
/// </summary>
/// <remarks>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 409 Conflict.</remarks>
/// <example>new ConflictError("Ya existe un pedido activo para el mismo jugador y máquina")</example>
public record ConflictError(string Error):PurchasedErrors(Error);

/// <summary>
/// Error cuando no se encuentra un pedido.
/// Se produce al buscar, actualizar o eliminar un pedido por Id que no existe en la base de datos.
/// </summary>
/// <remarks>
/// <para>Mensaje por defecto: "Purchased not found".</para>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 404 Not Found.</para>
/// </remarks>
/// <example>new PurchasedNotFoundError("Pedido con Id 999 no encontrado")</example>
public record PurchasedNotFoundError(string Error="Purchased not found") : PurchasedErrors(Error);

/// <summary>
/// Error de validación para datos inválidos de pedidos.
/// Se produce cuando los datos de entrada no cumplen las reglas de validación del dominio.
/// </summary>
/// <remarks>
/// <para>Validaciones que pueden disparar este error:</para>
/// <list type="bullet">
///   <item><description><c>Machine</c> vacío o mayor a 100 caracteres.</description></item>
///   <item><description><c>Comments</c> mayor a 1000 caracteres.</description></item>
///   <item><description><c>Price</c> negativo o cero.</description></item>
///   <item><description><c>PlayerId</c> o <c>AssignedTo</c> vacíos.</description></item>
///   <item><description>Modelo de raqueta (<c>RaquetModel</c>) vacío o mayor a 200 caracteres en líneas.</description></item>
/// </list>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 400 Bad Request.</para>
/// </remarks>
/// <param name="Error">Mensaje describiendo la validación fallida.</param>
/// <example>new ValidationError("El campo Machine es obligatorio")</example>
public record ValidationError(string Error): PurchasedErrors(Error);

/// <summary>
/// Error cuando se intenta realizar una transición de estado no permitida en un pedido o línea de pedido.
/// Por ejemplo, intentar cambiar de COMPLETED a PENDING, o de CANCELED a IN_PROGRESS.
/// </summary>
/// <remarks>
/// <para>Las transiciones de estado válidas están definidas en la lógica de negocio de <c>PedidoService</c>.</para>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 400 Bad Request.</para>
/// </remarks>
/// <example>new InvalidStatusError("No se puede cambiar un pedido cancelado a en progreso")</example>
public record InvalidStatusError(string Error) : PurchasedErrors(Error);

/// <summary>
/// Error de concurrencia optimista. Se produce cuando el token <c>Version</c>
/// de un usuario no coincide con el almacenado en la base de datos,
/// indicando que otro usuario modificó el registro entre la lectura y la escritura.
/// </summary>
/// <remarks>
/// <para>Mensaje por defecto: "El usuario fue modificado por otra operación. Intente de nuevo."</para>
/// <para>Está directamente relacionado con <see cref="VersionInterceptor"/> que incrementa
/// automáticamente la propiedad <c>Version</c> de <see cref="User"/> en cada modificación.</para>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 409 Conflict.</para>
/// </remarks>
/// <example>new ConcurrencyError()</example>
public record ConcurrencyError(string Error = "El usuario fue modificado por otra operación. Intente de nuevo.") : PurchasedErrors(Error);