using BackEncordados.Common.Dto;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Common.Utils;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Mapper;
using BackEncordados.Materials.Model;
using BackEncordados.Materials.Repository.Materials;
using CSharpFunctionalExtensions;

namespace BackEncordados.Materials.Service.Materials;

/// <summary>
/// Servicio de negocio para operaciones CRUD de materiales del inventario.
/// </summary>
/// <remarks>
/// <para>Implementa <see cref="IMaterialsService"/> orquestando operaciones entre
/// el repositorio <see cref="IMaterialsRepository"/>, los mappers y <see cref="ICloudinaryService"/>.</para>
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
///     <description>1. FindByIdAsync → 2. Aplicar cambios parciales → 3. Delete imagen anterior (si nueva) → 4. Upload nueva imagen → 5. Repo.UpdateAsync</description>
///   </item>
///   <item>
///     <term>Delete</term>
///     <description>1. FindByIdAsync → 2. Delete imagen Cloudinary (si no es default) → 3. Repo.DeleteAsync (soft-delete)</description>
///   </item>
/// </list>
/// <para>Usa el patrón Result (CSharpFunctionalExtensions) con errores tipados <see cref="MaterialError"/>.</para>
/// </remarks>
public class MaterialsService(ILogger<MaterialsService> logger, IMaterialsRepository repository, ICloudinaryService cloudinary):IMaterialsService {
    
    /// <summary>
    /// Busca todos los materiales con paginación, filtros y ordenamiento.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Delega en el repositorio <c>IMaterialsRepository.FindAllAsync</c>
    ///   que aplica filtros, ordenamiento y paginación.</description></item>
    ///   <item><description>Calcula <c>TotalPages</c> como <c>Ceiling(TotalCount / PageSize)</c>.
    ///   Si <c>PageSize = 0</c>, TotalPages = 0 (evita división por cero).</description></item>
    ///   <item><description>Mapea cada entidad <see cref="Material"/> a <see cref="MaterialResponseDto"/>
    ///   mediante <c>ToDto(cloudinary)</c>, que resuelve la URL de la imagen.</description></item>
    ///   <item><description>Retorna un <see cref="PageResponseDto{T}"/> con metadatos completos
    ///   de paginación.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="filter">DTO con filtros de búsqueda, paginación y ordenamiento.</param>
    /// <returns>Respuesta paginada con lista de materiales DTO y metadatos (totalPages, totalElements, pageSize, etc.).</returns>
    public async Task<PageResponseDto<MaterialResponseDto>> FindAllAsync(MaterialFilterDto filter) {
        logger.LogInformation("CuerdasService::FindAllAsync");
        var paged= await repository.FindAllAsync(filter);
        int totalPages = filter.Size > 0 ? (int)Math.Ceiling(paged.TotalCount / (double)filter.Size) : 0;
        return new PageResponseDto<MaterialResponseDto>(
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
    /// Busca un material por nombre exacto del modelo.
    /// </summary>
    /// <remarks>
    /// <para>Delega en <c>IMaterialsRepository.FindByNameAsync</c> con comparación exacta.
    /// Si se encuentra, mapea la entidad a DTO y retorna <c>Result.Success</c>.
    /// Si no se encuentra, retorna <c>Result.Failure</c> con <see cref="MaterialNotFoundError"/>.</para>
    /// <para>Usa <c>Tap</c>/<c>TapError</c> para logging sin modificar el flujo del Result.</para>
    /// </remarks>
    /// <param name="name">Nombre exacto del modelo a buscar.</param>
    /// <returns>Result con <see cref="MaterialResponseDto"/> si se encuentra; <see cref="MaterialNotFoundError"/> si no existe.</returns>
    public async Task<Result<MaterialResponseDto, MaterialError>> FindByNameAsync(string name) {
        logger.LogInformation("Buscando material con nombre {Modelo}", name);
        return await repository.FindByNameAsync(name) is { } result
            ? Result.Success<MaterialResponseDto, MaterialError>(result.ToDto(cloudinary))
                .Tap(() => logger.LogInformation("Material con nombre {Modelo} encontrado", name))
            : Result.Failure<MaterialResponseDto, MaterialError>(new MaterialNotFoundError())
                .TapError(() => logger.LogInformation("Material con nombre {Modelo} no encontrado", name));
    }

    /// <summary>
    /// Busca un material por su ID numérico.
    /// </summary>
    /// <remarks>
    /// <para>Delega en <c>IMaterialsRepository.FindByIdAsync</c> que excluye registros
    /// con soft-delete. Si se encuentra, mapea la entidad a DTO.</para>
    /// <para><b>Respuestas:</b></para>
    /// <list type="bullet">
    ///   <item><description>Material encontrado → <c>Result.Success</c> con DTO.</description></item>
    ///   <item><description>Material no encontrado o eliminado → <c>Result.Failure</c> con <see cref="MaterialNotFoundError"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">ID numérico del material a buscar.</param>
    /// <returns>Result con <see cref="MaterialResponseDto"/> si existe; <see cref="MaterialNotFoundError"/> si no.</returns>
    public async Task<Result<MaterialResponseDto, MaterialError>> FindByIdAsync(long id) {
        logger.LogInformation("Buscando material con id {Id}", id);
        return await repository.FindByIdAsync(id) is { } result
            ? Result.Success<MaterialResponseDto, MaterialError>(result.ToDto(cloudinary))
                .Tap(() => logger.LogInformation("Material con id {Id} encontrado", id))
            : Result.Failure<MaterialResponseDto, MaterialError>(new MaterialNotFoundError())
                .TapError(() => logger.LogInformation("Material con id {Id} no encontrado", id));
    }

    /// <summary>
    /// Crea un nuevo material en el inventario.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Mapea <see cref="MaterialRequestDto"/> a entidad <see cref="Material"/>
    ///   mediante <c>ToModel()</c> (convierte string de Type a enum <see cref="MaterialType"/>).</description></item>
    ///   <item><description>Si el request incluye una imagen (<c>Imagen</c> no nula):
    ///   <list type="bullet">
    ///     <item><description>Sube la imagen a Cloudinary con <c>UploadWithAutoNameAsync</c>.</description></item>
    ///     <item><description>Asigna la URL y el PublicId retornados a la entidad.</description></item>
    ///   </list></description></item>
    ///   <item><description>Persiste la entidad mediante <c>IMaterialsRepository.CreateAsync</c>.</description></item>
    ///   <item><description>Mapea la entidad persistida a DTO y retorna <c>Result.Success</c>.
    ///   Si falla la persistencia, retorna <c>MaterialConflictError</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="request">DTO con los datos del material a crear (incluye imagen opcional en FormData).</param>
    /// <returns>Result con <see cref="MaterialResponseDto"/> del material creado, o <see cref="MaterialConflictError"/> si falló la creación.</returns>
    public async Task<Result<MaterialResponseDto, MaterialError>> CreateAsync(MaterialRequestDto request) {
        logger.LogInformation("Creando material con nombre {Modelo}", request.Modelo);
        var material = request.ToModel();
        if (request.Imagen is not null)
        {
            var upload = await cloudinary.UploadWithAutoNameAsync(request.Imagen, material.Id.ToString(), CloudinaryConstants.FOLDER_MATERIES);
            material.ImageUrl = upload.ImageUrl;
            material.CloudinaryPublicId = upload.PublicId;
        }
        return await repository.CreateAsync(material) is { } result
            ? Result.Success<MaterialResponseDto, MaterialError>(result.ToDto(cloudinary))
                .Tap(() => logger.LogInformation("Material creado con id {Id}", result.Id))
            : Result.Failure<MaterialResponseDto, MaterialError>(new MaterialConflictError("No se pudo crear el material"))
                .TapError(() => logger.LogError("Error al crear el material"));
    }

    /// <summary>
    /// Actualiza un material existente, manejando reemplazo de imagen Cloudinary.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el material existente por <paramref name="id"/> en el repositorio.</description></item>
    ///   <item><description>Si no existe → retorna <c>Result.Failure</c> con <see cref="MaterialNotFoundError"/>.</description></item>
    ///   <item><description>Aplica cambios parciales al material encontrado:</description></item>
    ///   <list type="bullet">
    ///     <item><description><c>Modelo</c> — si no está vacío.</description></item>
    ///     <item><description><c>Precio</c> — si >= 0.</description></item>
    ///     <item><description><c>Stock</c> — si >= 0.</description></item>
    ///     <item><description><c>Type</c> — si no está vacío, parsea string a <see cref="MaterialType"/>.</description></item>
    ///   </list>
    ///   <item><description>Si el request incluye una nueva imagen:
    ///   <list type="bullet">
    ///     <item><description>Si el material tenía una imagen personalizada (no default), la elimina de Cloudinary.</description></item>
    ///     <item><description>Sube la nueva imagen a Cloudinary y actualiza URL + PublicId.</description></item>
    ///   </list></description></item>
    ///   <item><description>Persiste los cambios mediante <c>IMaterialsRepository.UpdateAsync</c>.</description></item>
    ///   <item><description>Retorna el DTO actualizado o error según el resultado.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">ID del material a actualizar.</param>
    /// <param name="request">DTO con los campos a actualizar (solo se aplican los valores no nulos/vacíos).</param>
    /// <returns>Result con <see cref="MaterialResponseDto"/> actualizado, o error específico.</returns>
    public async Task<Result<MaterialResponseDto, MaterialError>> UpdateAsync(long id, MaterialPatchDto request) {
        logger.LogInformation("Actualizando material con id {Id}", id);
        var material= await repository.FindByIdAsync(id);
        if (material == null) return Result.Failure<MaterialResponseDto, MaterialError>(new MaterialNotFoundError())
                .Tap(() => logger.LogInformation("Material con id {Id} no encontrado para actualizar", id));
        if (!string.IsNullOrEmpty(request.Modelo)) material.Modelo = request.Modelo;
        if (request.Precio >= 0) material.Precio = request.Precio;
        if (request.Stock >= 0) material.Stock = request.Stock;
        if (!string.IsNullOrEmpty(request.Type)) material.Type = Enum.Parse<MaterialType>(request.Type, true);
        if (request.Imagen is not null)
        {
            if (material.ImageUrl != CloudinaryConstants.DEFAULT_IMAGE_MATERIALES)
                await cloudinary.DeleteAsync(material.CloudinaryPublicId!);
            var upload = await cloudinary.UploadWithAutoNameAsync(request.Imagen, id.ToString(), CloudinaryConstants.FOLDER_MATERIES);
            material.ImageUrl = upload.ImageUrl;
            material.CloudinaryPublicId = upload.PublicId;
        }
        var updated = await repository.UpdateAsync(material,id);
        return updated is { } result
            ? Result.Success<MaterialResponseDto, MaterialError>(result.ToDto(cloudinary))
                .Tap(() => logger.LogInformation("Material con id {Id} actualizado", id))
            : Result.Failure<MaterialResponseDto, MaterialError>(new MaterialConflictError("No se pudo actualizar el material"))
                .TapError(() => logger.LogError("Error al actualizar el material con id {Id}", id));
    }

    /// <summary>
    /// Elimina lógicamente un material (soft-delete) y limpia su imagen de Cloudinary si existe.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el material por <paramref name="id"/> en el repositorio.</description></item>
    ///   <item><description>Si no existe → retorna <c>Result.Failure</c> con <see cref="MaterialNotFoundError"/>.</description></item>
    ///   <item><description>Si el material tiene una imagen personalizada (diferente de <c>DEFAULT_IMAGE_MATERIALES</c>):
    ///   <list type="bullet">
    ///     <item><description>Elimina la imagen de Cloudinary mediante <c>ICloudinaryService.DeleteAsync</c>.</description></item>
    ///   </list></description></item>
    ///   <item><description>Delega en el repositorio para marcar <c>IsDeleted = true</c> (soft-delete).</description></item>
    ///   <item><description>Retorna <c>Result.Success</c> con <c>Unit.Value</c> si se eliminó correctamente.</description></item>
    /// </list>
    /// <para><b>Nota:</b> La eliminación de Cloudinary se realiza antes del soft-delete en BD
    /// para evitar orfanar imágenes si la BD falla después de eliminar la imagen.</para>
    /// </remarks>
    /// <param name="id">ID del material a eliminar lógicamente.</param>
    /// <returns>Result con <c>Unit</c> si se eliminó; error específico si no.</returns>
    public async Task<Result<Unit, MaterialError>> DeleteAsync(long id) {
        logger.LogInformation("Eliminando material con id {Id}", id);
        var material = await repository.FindByIdAsync(id);
        if (material == null)
            return Result.Failure<Unit, MaterialError>(new MaterialNotFoundError())
                .TapError(() => logger.LogInformation("Material con id {Id} no encontrado para eliminar", id));
        if (material.ImageUrl != CloudinaryConstants.DEFAULT_IMAGE_MATERIALES)
            await cloudinary.DeleteAsync(material.CloudinaryPublicId!);
        return await repository.DeleteAsync(id) 
            ? Result.Success<Unit, MaterialError>(Unit.Value)
                .Tap(() => logger.LogInformation("Material con id {Id} eliminado", id))
            : Result.Failure<Unit, MaterialError>(new MaterialNotFoundError())
                .TapError(() => logger.LogInformation("Material con id {Id} no encontrado para eliminar", id));
    }
}