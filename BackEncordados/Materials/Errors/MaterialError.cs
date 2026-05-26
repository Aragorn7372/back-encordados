using BackEncordados.Usuarios.Errors;

namespace BackEncordados.Materials.Errors;

/// <summary>
/// Record base para errores del módulo de materiales (entidad <see cref="Material"/>).
/// Agrupa errores relacionados con grips, overgrips, lead tape, siliconas y otros accesorios.
/// </summary>
/// <remarks>
/// <para>Subtipos que heredan de este record:</para>
/// <list type="bullet">
///   <item><description><see cref="MaterialConflictError"/> — HTTP 409, duplicados o integridad.</description></item>
///   <item><description><see cref="MaterialNotFoundError"/> — HTTP 404, material no encontrado.</description></item>
///   <item><description><see cref="MaterialValidationError"/> — HTTP 400, datos inválidos.</description></item>
/// </list>
/// <para>Estos errores son lanzados por <c>MaterialService</c> y capturados por <see cref="GlobalExceptionHandler"/>.</para>
/// </remarks>
/// <param name="Error">Mensaje descriptivo del error específico de materiales.</param>
public record MaterialError(string Error)
{
    public string Error { get; set; } = Error;
};

/// <summary>
/// Error de conflicto para materiales. Se produce al intentar crear un material
/// que entra en conflicto con uno existente (marca + modelo duplicados),
/// o al violar restricciones de integridad referencial.
/// </summary>
/// <remarks>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 409 Conflict.</remarks>
/// <example>new MaterialConflictError("Ya existe un material con la marca Babolat y modelo Pro Overgrip")</example>
public record MaterialConflictError(string Error):MaterialError(Error);

/// <summary>
/// Error cuando no se encuentra un material en el catálogo.
/// Se produce al buscar, actualizar o eliminar un material por Id inexistente.
/// </summary>
/// <remarks>
/// <para>Mensaje por defecto: "Material not found".</para>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 404 Not Found.</para>
/// </remarks>
/// <example>new MaterialNotFoundError("Material con Id 999 no encontrado")</example>
public record MaterialNotFoundError(string Error="Material not found"):MaterialError(Error);

/// <summary>
/// Error de validación para datos inválidos de materiales.
/// Se produce cuando los datos de entrada incumplen reglas del dominio.
/// </summary>
/// <remarks>
/// <para>Validaciones que pueden disparar este error:</para>
/// <list type="bullet">
///   <item><description><c>Marca</c> vacía o mayor a 100 caracteres.</description></item>
///   <item><description><c>Modelo</c> vacío o mayor a 100 caracteres.</description></item>
///   <item><description><c>Stock</c> negativo.</description></item>
///   <item><description><c>Precio</c> negativo o cero.</description></item>
///   <item><description><c>Type</c> inválido o no perteneciente al enum <see cref="MaterialType"/>.</description></item>
/// </list>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 400 Bad Request.</para>
/// </remarks>
/// <param name="Error">Mensaje describiendo la validación fallida.</param>
/// <example>new MaterialValidationError("El campo Stock no puede ser negativo")</example>
public record MaterialValidationError(string Error):MaterialError(Error);


