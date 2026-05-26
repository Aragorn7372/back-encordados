using BackEncordados.Common.Dto;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Service.Materials;
using BackEncordados.Materials.Validator.Materials;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CSharpFunctionalExtensions;

namespace BackEncordados.Materials.Controller;

/// <summary>
/// Controlador API para la gestión de materiales del inventario
/// (grips, overgrips, lead tape, amortiguadores, bumpers, etc.).
/// </summary>
/// <remarks>
/// <para>Proporciona seis endpoints RESTful para operaciones CRUD completas sobre
/// la entidad <see cref="Material"/>, con autenticación JWT y políticas de
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
///     <term><c>/api/Materials</c></term>
///     <description>RequireEncorderRole</description>
///     <description>200, 400, 401, 403, 500</description>
///     <description>Lista paginada con filtros y ordenamiento.</description>
///   </item>
///   <item>
///     <term>GET</term>
///     <term><c>/api/Materials/{id:long}</c></term>
///     <description>RequireOwnerRole</description>
///     <description>200, 404, 500</description>
///     <description>Búsqueda por ID numérico.</description>
///   </item>
///   <item>
///     <term>GET</term>
///     <term><c>/api/Materials/name/{name}</c></term>
///     <description>RequireOwnerRole</description>
///     <description>200, 404, 500</description>
///     <description>Búsqueda por nombre exacto del modelo.</description>
///   </item>
///   <item>
///     <term>POST</term>
///     <term><c>/api/Materials</c></term>
///     <description>RequireOwnerRole</description>
///     <description>201, 400, 409, 500</description>
///     <description>Creación con validación FluentValidation y subida opcional de imagen a Cloudinary.</description>
///   </item>
///   <item>
///     <term>PUT</term>
///     <term><c>/api/Materials/{id:long}</c></term>
///     <description>RequireOwnerRole</description>
///     <description>200, 404, 409, 500</description>
///     <description>Actualización parcial (reemplazo de imagen Cloudinary incluido).</description>
///   </item>
///   <item>
///     <term>DELETE</term>
///     <term><c>/api/Materials/{id:long}</c></term>
///     <description>RequireAdminRole</description>
///     <description>200, 404, 500</description>
///     <description>Eliminación lógica (soft-delete) + limpieza de imagen Cloudinary.</description>
///   </item>
/// </list>
/// <para>Los errores de dominio se mapean mediante pattern matching sobre el tipo
/// de error retornado por el servicio (Result pattern de CSharpFunctionalExtensions).</para>
/// </remarks>
/// <param name="logger">Logger para seguimiento de operaciones entrantes.</param>
/// <param name="service">Servicio de lógica de negocio para materiales (<see cref="IMaterialsService"/>).</param>
/// <param name="validator">Validador FluentValidation para <see cref="MaterialRequestDto"/>.</param>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MaterialsController(
    ILogger<MaterialsController> logger, 
    IMaterialsService service,
    IValidator<MaterialRequestDto> validator) : ControllerBase{
    
    /// <summary>
    /// Obtiene todos los materiales con paginación, filtros y ordenamiento dinámico.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Construye un <see cref="MaterialFilterDto"/> con los parámetros recibidos.</description></item>
    ///   <item><description>Delega en <c>IMaterialsService.FindAllAsync</c> que aplica filtros (TournamentId, Search),
    ///   ordenamiento (SortBy + Direction) y paginación (Page + Size).</description></item>
    ///   <item><description>Retorna <c>200 OK</c> con un <see cref="PageResponseDto{T}"/> que incluye
    ///   la lista de materiales, total de páginas, total de elementos y metadatos de paginación.</description></item>
    /// </list>
    /// <para>Los materiales eliminados lógicamente (soft-delete) nunca se incluyen en los resultados.</para>
    /// <para>Requiere policy <c>RequireEncorderRole</c> — cualquier encordador puede listar.</para>
    /// </remarks>
    /// <param name="tournamentId">Filtro opcional por ID de torneo (ULID). Si es nulo, retorna materiales de todos los torneos.</param>
    /// <param name="sortBy">Campo por el que ordenar: "id", "marca", "modelo", "stock", "precio". Default: "id".</param>
    /// <param name="page">Número de página (0-indexed). Default: 0.</param>
    /// <param name="size">Cantidad de elementos por página. Default: 10.</param>
    /// <param name="direction">Dirección de ordenamiento: "asc" o "desc". Default: "asc".</param>
    /// <param name="search">Término de búsqueda para filtrar por Marca, Modelo, Type o Id (LIKE %texto%).</param>
    /// <returns>Respuesta paginada con la lista de materiales DTO.</returns>
    /// <response code="200">Lista paginada de materiales generada correctamente.</response>
    /// <response code="400">Parámetros de consulta inválidos (page/size negativos, sortBy no soportado).</response>
    /// <response code="401">Token JWT ausente o inválido.</response>
    /// <response code="403">El rol del usuario no cumple la política RequireEncorderRole.</response>
    /// <response code="500">Error interno del servidor al ejecutar la consulta.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PageResponseDto<MaterialResponseDto>),StatusCodes.Status200OK)]
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
        logger.LogInformation("Get all materials with search {Search}, sortBy {SortBy}, page {Page}, size {Size} and direction {Direction}",
            search, sortBy, page, size, direction);
        var filter = new MaterialFilterDto(
            TournamentId: tournamentId,
            Search: search,
            SortBy: sortBy,
            Page: page,
            Size: size,
            Direction: direction);
        return Ok(await service.FindAllAsync(filter));
    }

    /// <summary>
    /// Obtiene un material por su ID numérico.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>IMaterialsService.FindByIdAsync(id)</c>.</description></item>
    ///   <item><description>Si existe y no está eliminado → <c>200 OK</c> con el DTO.</description></item>
    ///   <item><description>Si no existe o fue eliminado → <c>404 NotFound</c> con mensaje de error.</description></item>
    ///   <item><description>Si ocurre un error no contemplado → <c>500 InternalServerError</c>.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="id">ID numérico del material a buscar.</param>
    /// <returns>DTO del material encontrado.</returns>
    /// <response code="200">Material encontrado y retornado.</response>
    /// <response code="404">Material no encontrado (no existe o fue eliminado lógicamente).</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(MaterialResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireOwnerRole")]
    public async Task<IActionResult> GetById(long id) {
        logger.LogInformation("Get material by id {Id}", id);
        return await service.FindByIdAsync(id).Match(
            material => Ok(material),
            error => error switch {
                MaterialNotFoundError => NotFound(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    /// <summary>
    /// Obtiene un material por el nombre exacto de su modelo.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>IMaterialsService.FindByNameAsync(name)</c> con comparación exacta.</description></item>
    ///   <item><description>Si existe → <c>200 OK</c> con el DTO del material.</description></item>
    ///   <item><description>Si no existe → <c>404 NotFound</c>.</description></item>
    /// </list>
    /// <para>La comparación es case-insensitive y exacta contra <c>Modelo</c>.</para>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="name">Nombre exacto del modelo a buscar (ej: "Pro Overgrip").</param>
    /// <returns>DTO del material encontrado.</returns>
    /// <response code="200">Material encontrado por nombre.</response>
    /// <response code="404">No existe un material con ese nombre de modelo.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("name/{name}")]
    [ProducesResponseType(typeof(MaterialResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireOwnerRole")]
    public async Task<IActionResult> GetByName(string name) {
        logger.LogInformation("Get material by name {Name}", name);
        return await service.FindByNameAsync(name).Match(
            material => Ok(material),
            error => error switch {
                MaterialNotFoundError => NotFound(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    /// <summary>
    /// Crea un nuevo material en el inventario. Acepta FormData con imagen opcional.
    /// </summary>
    /// <remarks>
    /// <para><b>Validaciones:</b></para>
    /// <list type="bullet">
    ///   <item><description>Ejecuta validación FluentValidation mediante <c>IValidator&lt;MaterialRequestDto&gt;</c>
    ///   (verifica TournamentId existente, Type válido, y campos obligatorios).</description></item>
    ///   <item><description>Si la validación falla → <c>400 BadRequest</c> con lista de errores por campo.</description></item>
    /// </list>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Valida el DTO de entrada con FluentValidation.</description></item>
    ///   <item><description>Si inválido → retorna <c>400</c> con los errores de validación.</description></item>
    ///   <item><description>Llama a <c>IMaterialsService.CreateAsync(request)</c> que mapea el DTO a entidad,
    ///   sube la imagen a Cloudinary (si se incluyó), y persiste en BD.</description></item>
    ///   <item><description>Si éxito → <c>201 Created</c> con el DTO del material creado y Location header
    ///   apuntando a <c>GetById</c>.</description></item>
    ///   <item><description>Si conflicto (duplicado) → <c>409 Conflict</c>.</description></item>
    ///   <item><description>Si error interno → <c>500 InternalServerError</c>.</description></item>
    /// </list>
    /// <para>Requiere policy <c>RequireOwnerRole</c> — solo propietarios pueden crear materiales.</para>
    /// </remarks>
    /// <param name="request">DTO con datos del material. Incluye campos obligatorios (Marca, Modelo, Stock, Precio, Type)
    /// y opcionalmente una imagen (IFormFile) en multipart/form-data.</param>
    /// <returns>DTO del material creado con su ID asignado.</returns>
    /// <response code="201">Material creado exitosamente. Incluye Location header con la URL del nuevo recurso.</response>
    /// <response code="400">Datos de entrada inválidos (validación FluentValidation fallida).</response>
    /// <response code="409">Conflicto: ya existe un material con los mismos datos (marca+modelo duplicados).</response>
    /// <response code="500">Error interno del servidor al crear el material.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MaterialResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireOwnerRole")]
    public async Task<IActionResult> Create([FromForm] MaterialRequestDto request) {
        logger.LogInformation("Create material with name {Name}", request.Modelo);
        
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        
        return await service.CreateAsync(request).Match(
            onSuccess: material => CreatedAtAction(nameof(GetById), new { id = material.Id }, material),
            onFailure: error => error switch {
                MaterialConflictError => Conflict(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    /// <summary>
    /// Actualiza un material existente. Acepta FormData con imagen opcional para reemplazar la actual.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>IMaterialsService.UpdateAsync(id, request)</c> que busca el material,
    ///   aplica cambios parciales y sube nueva imagen a Cloudinary si se provee.</description></item>
    ///   <item><description>Si éxito → <c>200 OK</c> con el DTO actualizado.</description></item>
    ///   <item><description>Si no existe → <c>404 NotFound</c>.</description></item>
    ///   <item><description>Si conflicto (error de integridad al persistir) → <c>409 Conflict</c>.</description></item>
    ///   <item><description>Si error interno → <c>500</c>.</description></item>
    /// </list>
    /// <para><b>Nota sobre la imagen:</b> Si se envía una nueva imagen, el servicio elimina la anterior
    /// de Cloudinary (a menos que sea la imagen por defecto) antes de subir la nueva.</para>
    /// <para>Requiere policy <c>RequireOwnerRole</c>.</para>
    /// </remarks>
    /// <param name="id">ID del material a actualizar.</param>
    /// <param name="request">DTO con campos a actualizar (valores no nulos/vacíos se aplican; el resto se ignora).
    /// Imagen opcional en IFormFile.</param>
    /// <returns>DTO del material con los cambios aplicados.</returns>
    /// <response code="200">Material actualizado correctamente.</response>
    /// <response code="404">Material no encontrado.</response>
    /// <response code="409">Conflicto al persistir los cambios.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(MaterialResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireOwnerRole")]
    public async Task<IActionResult> Update(long id, [FromForm] MaterialPatchDto request) {
        logger.LogInformation("Update material with id {Id}", id);
        return await service.UpdateAsync(id, request).Match(
            onSuccess: material => Ok(material),
            onFailure: error => error switch {
                MaterialNotFoundError => NotFound(new { message = error.Error }),
                MaterialConflictError => Conflict(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    /// <summary>
    /// Elimina lógicamente un material (soft-delete). También limpia su imagen de Cloudinary.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>IMaterialsService.DeleteAsync(id)</c> que busca el material,
    ///   elimina su imagen de Cloudinary (si no es la default), y marca <c>IsDeleted = true</c>.</description></item>
    ///   <item><description>Si éxito → <c>200 OK</c> con mensaje de confirmación.</description></item>
    ///   <item><description>Si no existe → <c>404 NotFound</c>.</description></item>
    ///   <item><description>Si error interno → <c>500</c>.</description></item>
    /// </list>
    /// <para><b>Nota:</b> El registro no se borra físicamente. Queda en BD con <c>IsDeleted = true</c>
    /// para preservar la integridad referencial con pedidos históricos.</para>
    /// <para>Requiere policy <c>RequireAdminRole</c> — solo administradores pueden eliminar materiales.</para>
    /// </remarks>
    /// <param name="id">ID del material a eliminar lógicamente.</param>
    /// <response code="200">Material eliminado correctamente (soft-delete + imagen limpiada).</response>
    /// <response code="404">Material no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(MaterialResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(policy: "RequireAdminRole")]
    public async Task<IActionResult> Delete(long id) {
        logger.LogInformation("Delete material with id {Id}", id);
        return await service.DeleteAsync(id).Match(
            onSuccess: _ => Ok(new { message = "Material deleted successfully" }),
            onFailure: error => error switch {
                MaterialNotFoundError => NotFound(new { message = error.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

}