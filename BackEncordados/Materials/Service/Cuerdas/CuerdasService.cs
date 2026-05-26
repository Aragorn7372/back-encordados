using BackEncordados.Common.Dto;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Common.Utils;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Mapper;
using BackEncordados.Materials.Model;
using BackEncordados.Materials.Repository.Strings;
using CSharpFunctionalExtensions;

namespace BackEncordados.Materials.Service.Cuerdas;

/// <summary>
/// Servicio de negocio para operaciones CRUD de cuerdas del inventario.
/// </summary>
/// <remarks>
/// <para>Implementa <see cref="ICuerdasService"/> orquestando operaciones entre
/// el repositorio <see cref="ICuerdasRepository"/>, los mappers y <see cref="ICloudinaryService"/>.</para>
/// <para><b>Flujo de datos por operación:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Operación</term>
///     <description>Pasos</description>
///   </listheader>
///   <item>
///     <term>Create</term>
///     <description>1. Map DTO→Model → 2. Upload imagen Cloudinary (opcional) → 3. Repo.CreateAsync → 4. Map Model→DTO</description>
///   </item>
///   <item>
///     <term>Update</term>
///     <description>1. FindByIdAsync → 2. Aplicar cambios parciales (Marca, Modelo, Stock, Precio, Calibre, etc.) → 3. Delete imagen anterior (si nueva) → 4. Upload nueva imagen → 5. Repo.UpdateAsync</description>
///   </item>
///   <item>
///     <term>Delete</term>
///     <description>1. FindByIdAsync → 2. Delete imagen Cloudinary (si no es default) → 3. Repo.DeleteAsync (soft-delete)</description>
///   </item>
/// </list>
/// <para>Usa el patrón Result (CSharpFunctionalExtensions) con errores tipados <see cref="CuerdaError"/>.</para>
/// </remarks>
public class CuerdasService(ILogger<CuerdasService> logger, ICuerdasRepository repository, ICloudinaryService cloudinary):ICuerdasService
{
    /// <summary>
    /// Busca todas las cuerdas con paginación, filtros y ordenamiento.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Delega en el repositorio <c>ICuerdasRepository.FindAllAsync</c>
    ///   que aplica filtros por Marca, Modelo, StringFormat, StringsType, más paginación y ordenamiento.</description></item>
    ///   <item><description>Calcula <c>TotalPages</c> como <c>Ceiling(TotalCount / PageSize)</c>.
    ///   Si <c>PageSize = 0</c>, TotalPages = 0 (evita división por cero).</description></item>
    ///   <item><description>Mapea cada entidad <see cref="Cuerdas"/> a <see cref="CuerdaResponseDto"/>
    ///   mediante <c>ToDto(cloudinary)</c>, que resuelve la URL de la imagen.</description></item>
    ///   <item><description>Retorna un <see cref="PageResponseDto{T}"/> con metadatos completos
    ///   de paginación (totalPages, totalElements, pageSize, pageNumber, etc.).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="filter">DTO con filtros de búsqueda, paginación y ordenamiento para cuerdas.</param>
    /// <returns>Respuesta paginada con lista de cuerdas DTO y metadatos.</returns>
    public async Task<PageResponseDto<CuerdaResponseDto>> FindAllAsync(CuerdaFilterdto filter)
    {
        logger.LogInformation("CuerdasService::FindAllAsync");
        var paged= await repository.FindAllAsync(filter);
        int totalPages = filter.Size > 0 ? (int)Math.Ceiling(paged.TotalCount / (double)filter.Size) : 0;
        return new PageResponseDto<CuerdaResponseDto>(
            Content: paged.Items.Select(item => item.ToDto(cloudinary)).ToList(),
            TotalPages: totalPages,
            TotalElements: paged.TotalCount,
            PageSize: filter.Size,
            PageNumber: filter.Page,    
            TotalPageElements: paged.Items.Count(),
            SortBy: filter.SortBy,
            Direction: filter.Direction
        );
    }

    /// <summary>
    /// Busca una cuerda por nombre exacto (marca o modelo).
    /// </summary>
    /// <remarks>
    /// <para>Delega en <c>ICuerdasRepository.FindByNameAsync</c> que busca coincidencia
    /// exacta contra <c>Marca</c> O <c>Modelo</c>.</para>
    /// <para>Si se encuentra, mapea la entidad a DTO y retorna <c>Result.Success</c>.
    /// Si no se encuentra, retorna <c>Result.Failure</c> con <see cref="CuerdaNotFoundError"/>.</para>
    /// </remarks>
    /// <param name="name">Nombre a buscar (coincide con Marca o Modelo).</param>
    /// <returns>Result con <see cref="CuerdaResponseDto"/> si se encuentra; <see cref="CuerdaNotFoundError"/> si no existe.</returns>
    public async Task<Result<CuerdaResponseDto, CuerdaError>> FindByNameAsync(string name) {
        logger.LogInformation("CuerdasService::FindByNameAsync");
        return await repository.FindByNameAsync(name) is { } result
            ? Result.Success<CuerdaResponseDto, CuerdaError>(result.ToDto(cloudinary))
                .Tap(() => logger.LogInformation($"CuerdaService::FindByNameAsync({name})"))
            : Result.Failure<CuerdaResponseDto, CuerdaError>(new CuerdaNotFoundError())
                .TapError(() => logger.LogInformation("Cuerda with name {Name} not found", name));
    }

    /// <summary>
    /// Busca una cuerda por su ID numérico.
    /// </summary>
    /// <remarks>
    /// <para>Delega en <c>ICuerdasRepository.FindByIdAsync</c> que excluye registros
    /// con soft-delete. Si se encuentra, mapea la entidad a DTO.</para>
    /// <para><b>Respuestas:</b></para>
    /// <list type="bullet">
    ///   <item><description>Cuerda encontrada → <c>Result.Success</c> con DTO.</description></item>
    ///   <item><description>Cuerda no encontrada o eliminada → <c>Result.Failure</c> con <see cref="CuerdaNotFoundError"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">ID numérico de la cuerda a buscar.</param>
    /// <returns>Result con <see cref="CuerdaResponseDto"/> si existe; <see cref="CuerdaNotFoundError"/> si no.</returns>
    public async Task<Result<CuerdaResponseDto, CuerdaError>> FindByIdAsync(long id)
    {
        logger.LogInformation("CuerdasService::FindByIdAsync");
        return await repository.FindByIdAsync(id) is { } result
            ? Result.Success<CuerdaResponseDto, CuerdaError>(result.ToDto(cloudinary))
                .Tap(()=>logger.LogInformation($"CuerdaService::FindByIdAsync({id})"))
            : Result.Failure<CuerdaResponseDto, CuerdaError>(new CuerdaNotFoundError())
                .TapError((() => logger.LogInformation("Cuerda con id {Id} not found", id)));
    }

    /// <summary>
    /// Crea una nueva cuerda en el inventario.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Mapea <see cref="CuerdaRequestDto"/> a entidad <see cref="Cuerdas"/>
    ///   mediante <c>ToModel()</c> (convierte strings de StringFormat y StringsType a enums).</description></item>
    ///   <item><description>Si el request incluye una imagen (<c>Imagen</c> no nula):
    ///   <list type="bullet">
    ///     <item><description>Sube la imagen a Cloudinary con <c>UploadWithAutoNameAsync</c>.</description></item>
    ///     <item><description>Asigna la URL y el PublicId retornados a la entidad.</description></item>
    ///   </list></description></item>
    ///   <item><description>Persiste la entidad mediante <c>ICuerdasRepository.CreateAsync</c>.</description></item>
    ///   <item><description>Mapea la entidad persistida a DTO y retorna <c>Result.Success</c>.
    ///   Si falla la persistencia, retorna <see cref="ConflictError"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="request">DTO con los datos de la cuerda a crear (incluye imagen opcional en FormData).</param>
    /// <returns>Result con <see cref="CuerdaResponseDto"/> de la cuerda creada, o <see cref="ConflictError"/> si falló la creación.</returns>
    public async Task<Result<CuerdaResponseDto, CuerdaError>> CreateAsync(CuerdaRequestDto request)
    {
        var cuerda = request.ToModel();
        if (request.Imagen is not null)
        {
            var upload = await cloudinary.UploadWithAutoNameAsync(request.Imagen, cuerda.Id.ToString(), CloudinaryConstants.FOLDER_MATERIES);
            cuerda.ImageUrl = upload.ImageUrl;
            cuerda.CloudinaryPublicId = upload.PublicId;
        }
        return await repository.CreateAsync(cuerda) is { } result
            ? Result.Success<CuerdaResponseDto, CuerdaError>(result.ToDto(cloudinary))
                .Tap(() => logger.LogInformation("Cuerda created with id {Id}", result.Id))
            : Result.Failure<CuerdaResponseDto, CuerdaError>(new ConflictError("no se pudo crear la cuerda"))
                .TapError(() => logger.LogError("Failed to create Cuerda"));
    }

    /// <summary>
    /// Actualiza una cuerda existente, manejando reemplazo de imagen Cloudinary.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Busca la cuerda existente por <paramref name="id"/> en el repositorio.</description></item>
    ///   <item><description>Si no existe → retorna <c>Result.Failure</c> con <see cref="CuerdaNotFoundError"/>.</description></item>
    ///   <item><description>Aplica cambios parciales a la cuerda encontrada:</description></item>
    ///   <list type="bullet">
    ///     <item><description><c>Marca</c> — si no está vacía.</description></item>
    ///     <item><description><c>Modelo</c> — si no está vacío.</description></item>
    ///     <item><description><c>Precio</c> — si >= 0.</description></item>
    ///     <item><description><c>Stock</c> — si >= 0.</description></item>
    ///     <item><description><c>Calibre</c> — si > 0.</description></item>
    ///     <item><description><c>StringFormat</c> y <c>StringsType</c> — si no están vacíos, parsea string a enum.</description></item>
    ///   </list>
    ///   <item><description>Si el request incluye una nueva imagen:
    ///   <list type="bullet">
    ///     <item><description>Si la cuerda tenía una imagen personalizada (no default), la elimina de Cloudinary.</description></item>
    ///     <item><description>Sube la nueva imagen a Cloudinary y actualiza URL + PublicId.</description></item>
    ///   </list></description></item>
    ///   <item><description>Persiste los cambios mediante <c>ICuerdasRepository.UpdateAsync</c>.</description></item>
    ///   <item><description>Retorna el DTO actualizado o error según el resultado.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">ID de la cuerda a actualizar.</param>
    /// <param name="request">DTO con los campos a actualizar (solo se aplican valores no nulos/vacíos).</param>
    /// <returns>Result con <see cref="CuerdaResponseDto"/> actualizado, o error específico.</returns>
    public async Task<Result<CuerdaResponseDto, CuerdaError>> UpdateAsync(long id,CuerdaPatchDto request) {
        var cuerda = await repository.FindByIdAsync(id);
        if (cuerda == null)
            return  Result.Failure<CuerdaResponseDto, CuerdaError>(new CuerdaNotFoundError())
                .Tap(() => logger.LogInformation("Cuerda with id {Id} found", id));
        if(!string.IsNullOrEmpty(request.Marca)) cuerda.Marca = request.Marca;
        if(!string.IsNullOrEmpty(request.Modelo)) cuerda.Modelo = request.Modelo;
        if (request.Precio >= 0) cuerda.Precio = request.Precio;
        if (request.Stock >= 0) cuerda.Stock = request.Stock;
        if (request.Calibre > 0) cuerda.Calibre = request.Calibre;
        if(!string.IsNullOrEmpty(request.StringFormat)) 
            cuerda.StringFormat= Enum.Parse<FormatoCuerda>(request.StringFormat, true);
        if (!string.IsNullOrEmpty(request.StringsType)) 
            cuerda.StringsType = Enum.Parse<StringsType>(request.StringsType, true);
        if (request.Imagen is not null)
        {
            if (cuerda.ImageUrl != CloudinaryConstants.DEFAULT_IMAGE_MATERIALES)
                await cloudinary.DeleteAsync(cuerda.CloudinaryPublicId!);
            var upload = await cloudinary.UploadWithAutoNameAsync(request.Imagen, id.ToString(), CloudinaryConstants.FOLDER_MATERIES);
            cuerda.ImageUrl = upload.ImageUrl;
            cuerda.CloudinaryPublicId = upload.PublicId;
        }
        return await repository.UpdateAsync(cuerda,id) is { } result
            ? Result.Success<CuerdaResponseDto, CuerdaError>(result.ToDto(cloudinary))
                .Tap(() => logger.LogInformation("Cuerda with id {Id} updated successfully", id))
            : Result.Failure<CuerdaResponseDto, CuerdaError>(new CuerdaNotFoundError())
                .TapError(() => logger.LogWarning("Cuerda with id {Id} not found for update", id));
    }

    /// <summary>
    /// Elimina lógicamente una cuerda (soft-delete) y limpia su imagen de Cloudinary si existe.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Busca la cuerda por <paramref name="id"/> en el repositorio.</description></item>
    ///   <item><description>Si no existe → retorna <c>Result.Failure</c> con <see cref="CuerdaNotFoundError"/>.</description></item>
    ///   <item><description>Si la cuerda tiene una imagen personalizada (diferente de <c>DEFAULT_IMAGE_MATERIALES</c>):
    ///   <list type="bullet">
    ///     <item><description>Elimina la imagen de Cloudinary mediante <c>ICloudinaryService.DeleteAsync</c>.</description></item>
    ///   </list></description></item>
    ///   <item><description>Delega en el repositorio para marcar <c>IsDeleted = true</c> (soft-delete).</description></item>
    ///   <item><description>Retorna <c>Result.Success</c> con <c>Unit.Value</c> si se eliminó correctamente.</description></item>
    /// </list>
    /// <para><b>Nota:</b> La eliminación de Cloudinary se realiza antes del soft-delete en BD
    /// para evitar orfanar imágenes si la BD falla después de eliminar la imagen.</para>
    /// </remarks>
    /// <param name="id">ID de la cuerda a eliminar lógicamente.</param>
    /// <returns>Result con <c>Unit</c> si se eliminó; <see cref="CuerdaNotFoundError"/> si no existe.</returns>
    public async Task<Result<Unit, CuerdaError>> DeleteAsync(long id)
    {
        logger.LogInformation("eliminando cuerda con id {Id}", id);
        var cuerda = await repository.FindByIdAsync(id);
        if (cuerda == null)
            return Result.Failure<Unit, CuerdaError>(new CuerdaNotFoundError())
                .TapError(() => logger.LogWarning("Cuerda with id {Id} not found for deletion", id));
        if (cuerda.ImageUrl != CloudinaryConstants.DEFAULT_IMAGE_MATERIALES)
            await cloudinary.DeleteAsync(cuerda.CloudinaryPublicId!);
        return await repository.DeleteAsync(id)
            ? Result.Success<Unit, CuerdaError>(new Unit())
                .Tap(() => logger.LogInformation("Cuerda with id {Id} deleted successfully", id))
            : Result.Failure<Unit, CuerdaError>(new CuerdaNotFoundError())
                .TapError(() => logger.LogWarning("Cuerda with id {Id} not found for deletion", id));
    }
}