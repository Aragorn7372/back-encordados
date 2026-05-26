using BackEncordados.Common.Dto;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Service.Cuerdas;
using BackEncordados.Materials.Validator.Strings;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEncordados.Materials.Controller;

/// <summary>
/// Controlador API para la gestión de cuerdas del inventario
/// (polyester, multifilamento, tripa natural, sintético, híbrido).
/// </summary>
/// <remarks>
/// <para>Proporciona seis endpoints RESTful para operaciones CRUD completas sobre
/// la entidad <see cref="Cuerdas"/>, con autenticación JWT y políticas de
/// autorización diferenciadas por rol y operación.</para>
/// <para><b>Tabla de endpoints:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <term>Ruta</term>
///     <description>Política requerida</description>
///     <description>Códigos de respuesta</description>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term>GET</term>
///     <term><c>/api/Cuerdas</c></term>
///     <description>RequireEncorderRole</description>
///     <description>200, 400, 401, 403, 500</description>
///     <description>Lista paginada con filtros y ordenamiento.</description>
///   </item>
///   <item>
///     <term>GET</term>
///     <term><c>/api/Cuerdas/{id:long}</c></term>
///     <description>RequireOwnerRole</description>
///     <description>200, 404, 500</description>
///     <description>Búsqueda por ID numérico.</description>
///   </item>
///   <item>
///     <term>GET</term>
///     <term><c>/api/Cuerdas/name/{name}</c></term>
///     <description>RequireOwnerRole</description>
///     <description>200, 404, 500</description>
///     <description>Búsqueda por nombre exacto (marca o modelo).</description>
///   </item>
///   <item>
///     <term>POST</term>
///     <term><c>/api/Cuerdas</c></term>
///     <description>RequireOwnerRole</description>
///     <description>201, 400, 409, 500</description>
///     <description>Creación con validación FluentValidation y subida opcional de imagen a Cloudinary.</description>
///   </item>
///   <item>
///     <term>PUT</term>
///     <term><c>/api/Cuerdas/{id:long}</c></term>
///     <description>RequireOwnerRole</description>
///     <description>200, 404, 409, 500</description>
///     <description>Actualización parcial (incluye reemplazo de imagen Cloudinary).</description>
///   </item>
///   <item>
///     <term>DELETE</term>
///     <term><c>/api/Cuerdas/{id:long}</c></term>
///     <description>RequireOwnerRole</description>
///     <description>200, 404, 500</description>
///     <description>Eliminación lógica (soft-delete) + limpieza de imagen Cloudinary.</description>
///   </item>
/// </list>
/// <para>Los errores de dominio se mapean mediante pattern matching sobre el Result
/// retornado por el servicio (CSharpFunctionalExtensions).</para>
/// </remarks>
/// <param name="logger">Logger para seguimiento de operaciones entrantes.</param>
/// <param name="service">Servicio de lógica de negocio para cuerdas (<see cref="ICuerdasService"/>).</param>
/// <param name="validator">Validador FluentValidation para <see cref="CuerdaRequestDto"/>.</param>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CuerdasController(
    ILogger<CuerdasController> logger, 
    ICuerdasService service,
    IValidator<CuerdaRequestDto> validator) : ControllerBase{
    
    /// <summary>
    /// Obtiene todas las cuerdas con paginación, filtros y ordenamiento dinámico.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Construye un <see cref="CuerdaFilterdto"/> con los parámetros recibidos.</description></item>
    ///   <item><description>Delega en <c>ICuerdasService.FindAllAsync</c> que aplica filtros (TournamentId, Search),
    ///   ordenamiento (SortBy + Direction soportando Marca, Modelo, Stock, Precio, Calibre, StringFormat, StringsType)
    ///   y paginación (Page + Size).</description></item>
    ///   <item><description>Retorna <c>200 OK</c> con un <see cref="PageResponseDto{T}"/> que incluye
    ///   la lista de cuerdas, total de páginas, total de elementos y metadatos.</description></item>
    /// </list>
    /// <para>Las cuerdas eliminadas lógicamente nunca se incluyen en los resultados.</para>
    /// <para>Requiere policy <c>RequireEncorderRole</c>.</para>
    /// </remarks>
    /// <param name="tournamentId">Filtro opcional por ID de torneo (ULID).</param>
    /// <param name="sortBy">Campo de ordenamiento: "id", "marca", "modelo", "stock", "precio", "calibre", "stringformat", "stringstype". Default: "id".</param>
    /// <param name="page">Número de página (0-indexed). Default: 0.</param>
    /// <param name="size">Elementos por página. Default: 10.</param>
    /// <param name="direction">"asc" o "desc". Default: "asc".</param>
    /// <param name="search">Término de búsqueda (LIKE sobre Marca, Modelo, StringFormat, StringsType, Id).</param>
    /// <returns>Respuesta paginada con la lista de cuerdas DTO.</returns>
    /// <response code="200">Lista paginada de cuerdas generada correctamente.</response>
    /// <response code="400">Parámetros de consulta inválidos.</response>
    /// <response code="401">Token JWT ausente o inválido.</response>
    /// <response code="403">El rol no cumple la política RequireEncorderRole.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PageResponseDto<CuerdaResponseDto>),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(policy: "RequireEncorderRole")]
    public async Task<IActionResult> GetAll(
        [FromQuery] Ulid? tournamentId,
        [FromQuery] string sortBy = "id",
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string direction = "asc",
        [FromQuery] string search = "") {
        logger.LogInformation("Get all cuerdas with search {Search}, sortBy {SortBy}, page {Page}, size {Size}, direction {Direction} and tournamentId {TournamentId}",
            search, sortBy, page, size, direction, tournamentId);
        var filter = new CuerdaFilterdto(
            TournamentId: tournamentId,
            Search: search,
            SortBy: sortBy,
            Page: page,
            Size: size,
            Direction: direction);
        return Ok(await service.FindAllAsync(filter));
    }

    /// <summary>
    /// Obtiene una cuerda por su ID numérico.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>ICuerdasService.FindByIdAsync(id)</c>.</description></item>
    ///   <item><description>Si existe y no está eliminada → <c>200 OK</c> con el DTO.</description></item>
    ///   <item><description>Si no existe o fue eliminada → <c>404 NotFound</c> con mensaje de error.</description></item>
    ///   <item><description>Si error no contemplado → <c>500 InternalServerError</c>.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="id">ID numérico de la cuerda a buscar.</param>
    /// <returns>DTO de la cuerda encontrada.</returns>
    /// <response code="200">Cuerda encontrada y retornada.</response>
    /// <response code="404">Cuerda no encontrada (no existe o fue eliminada lógicamente).</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(CuerdaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireOwnerRole")]
    public async Task<IActionResult> GetById(long id) {
        logger.LogInformation("Get cuerda by id {Id}", id);
        return await service.FindByIdAsync(id).Match(
            cuerda => Ok(cuerda),
            error => error switch {
                CuerdaNotFoundError => NotFound(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    /// <summary>
    /// Obtiene una cuerda por su nombre exacto (marca o modelo).
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>ICuerdasService.FindByNameAsync(name)</c>.</description></item>
    ///   <item><description>Busca coincidencia exacta contra <c>Marca</c> O <c>Modelo</c>.</description></item>
    ///   <item><description>Si existe → <c>200 OK</c> con el DTO.</description></item>
    ///   <item><description>Si no existe → <c>404 NotFound</c>.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="name">Nombre a buscar (coincide con Marca o Modelo exacto).</param>
    /// <returns>DTO de la cuerda encontrada.</returns>
    /// <response code="200">Cuerda encontrada por nombre.</response>
    /// <response code="404">No existe una cuerda con ese nombre.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("name/{name}")]
    [ProducesResponseType(typeof(CuerdaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireOwnerRole")]
    public async Task<IActionResult> GetByName(string name) {
        logger.LogInformation("Get cuerda by name {Name}", name);
        return await service.FindByNameAsync(name).Match(
            cuerda => Ok(cuerda),
            error => error switch {
                CuerdaNotFoundError => NotFound(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    /// <summary>
    /// Crea una nueva cuerda en el inventario. Acepta FormData con imagen opcional.
    /// </summary>
    /// <remarks>
    /// <para><b>Validaciones:</b></para>
    /// <list type="bullet">
    ///   <item><description>Ejecuta validación FluentValidation mediante <c>IValidator&lt;CuerdaRequestDto&gt;</c>
    ///   (verifica TournamentId existente, StringFormat y StringsType válidos).</description></item>
    ///   <item><description>Si la validación falla → <c>400 BadRequest</c> con errores por campo.</description></item>
    /// </list>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Valida el DTO de entrada con FluentValidation.</description></item>
    ///   <item><description>Si inválido → <c>400</c>.</description></item>
    ///   <item><description>Llama a <c>ICuerdasService.CreateAsync(request)</c> que mapea el DTO a entidad,
    ///   sube la imagen a Cloudinary (si se incluyó), y persiste en BD.</description></item>
    ///   <item><description>Si éxito → <c>201 Created</c> con Location header a <c>GetById</c>.</description></item>
    ///   <item><description>Si conflicto → <c>409 Conflict</c>.</description></item>
    ///   <item><description>Si error interno → <c>500</c>.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="request">DTO con datos de la cuerda (Marca, Modelo, Stock, Precio, Calibre, StringFormat, StringsType)
    /// más imagen opcional en IFormFile mediante multipart/form-data.</param>
    /// <returns>DTO de la cuerda creada con su ID asignado.</returns>
    /// <response code="201">Cuerda creada exitosamente. Location header con URL del recurso.</response>
    /// <response code="400">Datos de entrada inválidos (validación FluentValidation fallida).</response>
    /// <response code="409">Conflicto: cuerda duplicada o error de integridad.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CuerdaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireOwnerRole")]
    public async Task<IActionResult> Create([FromForm] CuerdaRequestDto request) {
        logger.LogInformation("Create cuerda with name {Name}", request.Modelo);
        
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        
        return await service.CreateAsync(request).Match(
            onSuccess: cuerda => CreatedAtAction(nameof(GetById), new { id = cuerda.Id }, cuerda),
            onFailure: error => error switch {
                ConflictError => Conflict(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    /// <summary>
    /// Actualiza una cuerda existente. Acepta FormData con imagen opcional para reemplazar la actual.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>ICuerdasService.UpdateAsync(id, request)</c> que busca la cuerda,
    ///   aplica cambios parciales (Marca, Modelo, Stock, Precio, Calibre, StringFormat, StringsType)
    ///   y reemplaza la imagen en Cloudinary si se provee una nueva.</description></item>
    ///   <item><description>Si éxito → <c>200 OK</c> con el DTO actualizado.</description></item>
    ///   <item><description>Si no existe → <c>404 NotFound</c>.</description></item>
    ///   <item><description>Si conflicto → <c>409 Conflict</c>.</description></item>
    ///   <item><description>Si error interno → <c>500</c>.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="id">ID de la cuerda a actualizar.</param>
    /// <param name="request">DTO con campos a actualizar (valores no nulos/vacíos se aplican).
    /// Imagen opcional en IFormFile.</param>
    /// <returns>DTO de la cuerda con los cambios aplicados.</returns>
    /// <response code="200">Cuerda actualizada correctamente.</response>
    /// <response code="404">Cuerda no encontrada.</response>
    /// <response code="409">Conflicto al persistir los cambios.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(CuerdaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireOwnerRole")]
    public async Task<IActionResult> Update(long id, [FromForm] CuerdaPatchDto request) {
        logger.LogInformation("Update cuerda with id {Id}", id);
        return await service.UpdateAsync(id, request).Match(
            onSuccess: cuerda => Ok(cuerda),
            onFailure: error => error switch {
                CuerdaNotFoundError => NotFound(new { message = error.Error }),
                ConflictError => Conflict(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    /// <summary>
    /// Elimina lógicamente una cuerda (soft-delete). También limpia su imagen de Cloudinary.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>ICuerdasService.DeleteAsync(id)</c> que busca la cuerda,
    ///   elimina su imagen de Cloudinary (si no es la default), y marca <c>IsDeleted = true</c>.</description></item>
    ///   <item><description>Si éxito → <c>200 OK</c> con mensaje de confirmación.</description></item>
    ///   <item><description>Si no existe → <c>404 NotFound</c>.</description></item>
    ///   <item><description>Si error interno → <c>500</c>.</description></item>
    /// </list>
    /// <para>El registro no se borra físicamente. Queda en BD con <c>IsDeleted = true</c>
    /// para preservar integridad referencial con pedidos históricos.</para>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="id">ID de la cuerda a eliminar lógicamente.</param>
    /// <response code="200">Cuerda eliminada correctamente (soft-delete + imagen limpiada).</response>
    /// <response code="404">Cuerda no encontrada.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(CuerdaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireOwnerRole")]
    public async Task<IActionResult> Delete(long id) {
        logger.LogInformation("Delete cuerda with id {Id}", id);
        return await service.DeleteAsync(id).Match(
            onSuccess: _ => Ok(new { message = "cuerda deleted successfully" }),
            onFailure: error => error switch {
                CuerdaNotFoundError => NotFound(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

}