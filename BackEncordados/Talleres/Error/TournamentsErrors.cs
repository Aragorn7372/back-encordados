using BackEncordados.Common.Errors;

namespace BackEncordados.Talleres.Error;

/// <summary>
/// Record base para errores del módulo de torneos (entidad <see cref="Tournaments"/>).
/// Agrupa errores relacionados con la creación, modificación y gestión de torneos,
/// asignación de trabajadores y supervisores, y configuración de máquinas.
/// </summary>
/// <remarks>
/// <para>Subtipos que heredan de este record:</para>
/// <list type="bullet">
///   <item><description><see cref="ConflictError"/> — HTTP 409, duplicados (título de torneo duplicado, etc.).</description></item>
///   <item><description><see cref="TournamentNotFoundError"/> — HTTP 404, torneo no encontrado.</description></item>
///   <item><description><see cref="ValidationError"/> — HTTP 400, datos inválidos.</description></item>
/// </list>
/// <para>Estos errores son lanzados por <c>TournamentService</c> y capturados por <see cref="GlobalExceptionHandler"/>.</para>
/// </remarks>
/// <param name="Error">Mensaje descriptivo del error específico de torneos.</param>
public record TournamentsErrors(
    string Error) : DomainErrors(Error);

/// <summary>
/// Error de conflicto para torneos. Se produce al intentar crear un torneo
/// con un título que ya existe, o al violar restricciones de integridad
/// referencial (ej: asignar un usuario inexistente como worker o supervisor).
/// </summary>
/// <remarks>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 409 Conflict.</remarks>
/// <example>new ConflictError("Ya existe un torneo con el título 'Torneo Madrileño 2025'")</example>
public record ConflictError(string Error):TournamentsErrors(Error);

/// <summary>
/// Error cuando no se encuentra un torneo específico.
/// Se produce al buscar, actualizar o eliminar un torneo por Id que no existe.
/// </summary>
/// <remarks>
/// <para>Mensaje por defecto: "Tournament not found".</para>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 404 Not Found.</para>
/// </remarks>
/// <example>new TournamentNotFoundError("Torneo con Id 999 no encontrado")</example>
public record TournamentNotFoundError(string Error="Tournament not found") : TournamentsErrors(Error);

/// <summary>
/// Error de validación para datos inválidos de torneos.
/// Se produce cuando los datos de entrada incumplen reglas del dominio.
/// </summary>
/// <remarks>
/// <para>Validaciones que pueden disparar este error:</para>
/// <list type="bullet">
///   <item><description><c>Title</c> vacío o mayor a 200 caracteres.</description></item>
///   <item><description><c>StartTournament</c> posterior a <c>EndTournament</c>.</description></item>
///   <item><description><c>Owner</c> vacío o inválido.</description></item>
///   <item><description><c>Logotype</c> URL inválida o mayor a 500 caracteres.</description></item>
/// </list>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 400 Bad Request.</para>
/// </remarks>
/// <param name="Error">Mensaje describiendo la validación fallida.</param>
/// <example>new ValidationError("La fecha de inicio no puede ser posterior a la fecha de fin")</example>
public record ValidationError(string Error): TournamentsErrors(Error);