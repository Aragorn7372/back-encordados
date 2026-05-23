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

public class CuerdasService(ILogger<CuerdasService> logger, ICuerdasRepository repository, ICloudinaryService cloudinary):ICuerdasService
{
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

    public async Task<Result<CuerdaResponseDto, CuerdaError>> FindByNameAsync(string name) {
        logger.LogInformation("CuerdasService::FindByNameAsync");
        return await repository.FindByNameAsync(name) is { } result
            ? Result.Success<CuerdaResponseDto, CuerdaError>(result.ToDto(cloudinary))
                .Tap(() => logger.LogInformation($"CuerdaService::FindByNameAsync({name})"))
            : Result.Failure<CuerdaResponseDto, CuerdaError>(new CuerdaNotFoundError())
                .TapError(() => logger.LogInformation("Cuerda with name {Name} not found", name));
    }

    public async Task<Result<CuerdaResponseDto, CuerdaError>> FindByIdAsync(long id)
    {
        logger.LogInformation("CuerdasService::FindByIdAsync");
        return await repository.FindByIdAsync(id) is { } result
            ? Result.Success<CuerdaResponseDto, CuerdaError>(result.ToDto(cloudinary))
                .Tap(()=>logger.LogInformation($"CuerdaService::FindByIdAsync({id})"))
            : Result.Failure<CuerdaResponseDto, CuerdaError>(new CuerdaNotFoundError())
                .TapError((() => logger.LogInformation("Cuerda con id {Id} not found", id)));
    }

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

    public async Task<Result<CuerdaResponseDto, CuerdaError>> UpdateAsync(long id,CuerdaPatchDto request) {
        var cuerda = await repository.FindByIdAsync(id);
        if (cuerda == null)
            return  Result.Failure<CuerdaResponseDto, CuerdaError>(new CuerdaNotFoundError())
                .Tap(() => logger.LogInformation("Cuerda with id {Id} found", id));
        if(!string.IsNullOrEmpty(request.Marca)) cuerda.Marca = request.Marca;
        if(!string.IsNullOrEmpty(request.Modelo)) cuerda.Modelo = request.Modelo;
        if (request.Precio >= 0) cuerda.Precio = request.Precio;
        if (request.Stock >= 0) cuerda.Stock = request.Stock;
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