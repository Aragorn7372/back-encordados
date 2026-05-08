using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Errors;
using BackEncordados.Materials.Mapper;
using BackEncordados.Materials.Model;
using BackEncordados.Materials.Repository.Materials;
using CSharpFunctionalExtensions;

namespace BackEncordados.Materials.Service.Materials;

public class MaterialsService(ILogger<MaterialsService> logger, IMaterialsRepository repository):IMaterialsService {
    
    public async Task<PageResponseDto<MaterialResponseDto>> FindAllAsync(MaterialFilterDto filter) {
        logger.LogInformation("CuerdasService::FindAllAsync");
        var paged= await repository.FindAllAsync(filter);
        int totalPages = filter.Size > 0 ? (int)Math.Ceiling(paged.TotalCount / (double)filter.Size) : 0;
        return new PageResponseDto<MaterialResponseDto>(
            Content: paged.Items.Select(item => item.ToDto()).ToList(),
            TotalPages: totalPages,
            TotalElements: paged.TotalCount,
            PageSize: filter.Size,
            PageNumber: filter.Page,
            TotalPageElements: paged.Items.Count(),
            SortBy: filter.SortBy,
            Direction: filter.Direction
        );
    }

    public async Task<Result<MaterialResponseDto, MaterialError>> FindByIdAsync(long id) {
        logger.LogInformation("Buscando material con id {Id}", id);
        return await repository.FindByIdAsync(id) is { } result
            ? Result.Success<MaterialResponseDto, MaterialError>(result.ToDto())
                .Tap(() => logger.LogInformation("Material con id {Id} encontrado", id))
            : Result.Failure<MaterialResponseDto, MaterialError>(new MaterialNotFoundError())
                .TapError(() => logger.LogInformation("Material con id {Id} no encontrado", id));
    }

    public async Task<Result<MaterialResponseDto, MaterialError>> CreateAsync(MaterialRequestDto request) {
        logger.LogInformation("Creando material con nombre {Modelo}", request.Modelo);
        return await repository.CreateAsync(request.ToModel()) is { } result
            ? Result.Success<MaterialResponseDto, MaterialError>(result.ToDto())
                .Tap(() => logger.LogInformation("Material creado con id {Id}", result.Id))
            : Result.Failure<MaterialResponseDto, MaterialError>(new MaterialConflictError("No se pudo crear el material"))
                .TapError(() => logger.LogError("Error al crear el material"));
    }

    public async Task<Result<MaterialResponseDto, MaterialError>> UpdateAsync(long id, MaterialPatchDto request) {
        logger.LogInformation("Actualizando material con id {Id}", id);
        var material= await repository.FindByIdAsync(id);
        if (material == null) return Result.Failure<MaterialResponseDto, MaterialError>(new MaterialNotFoundError())
                .Tap(() => logger.LogInformation("Material con id {Id} no encontrado para actualizar", id));
        if (string.IsNullOrEmpty(request.Modelo)) material.Modelo = request.Modelo;
        if (request.Precio >= 0) material.Precio = request.Precio;
        if (request.Stock >= 0) material.Stock = request.Stock;
        if (string.IsNullOrEmpty(request.Type)) material.Type = Enum.Parse<MaterialType>(request.Type, true);
        var updated = await repository.UpdateAsync(material,id);
        return updated is { } result
            ? Result.Success<MaterialResponseDto, MaterialError>(result.ToDto())
                .Tap(() => logger.LogInformation("Material con id {Id} actualizado", id))
            : Result.Failure<MaterialResponseDto, MaterialError>(new MaterialConflictError("No se pudo actualizar el material"))
                .TapError(() => logger.LogError("Error al actualizar el material con id {Id}", id));
    }

    public async Task<Result<Unit, MaterialError>> DeleteAsync(long id) {
        logger.LogInformation("Eliminando material con id {Id}", id);
        return await repository.DeleteAsync(id) 
            ? Result.Success<Unit, MaterialError>(Unit.Value)
                .Tap(() => logger.LogInformation("Material con id {Id} eliminado", id))
            : Result.Failure<Unit, MaterialError>(new MaterialNotFoundError())
                .TapError(() => logger.LogInformation("Material con id {Id} no encontrado para eliminar", id));
    }
}