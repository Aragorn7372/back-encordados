using BackEncordados.Common.Dto;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Common.Utils;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Mapper;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using CSharpFunctionalExtensions;

namespace BackEncordados.Usuarios.Service.CrudService;

public class UserService(ILogger<UserService> logger, IUserRepository repository,ICloudinaryService cloudinary) : IUserService
{
    public async Task<Result<UserResponseDto, AuthError>> FindByIdAsync(Ulid id)
    {
        return await repository.FindByIdAsync(id) is { } user
            ? Result.Success<UserResponseDto, AuthError>(user.ToDto(cloudinary))
            : Result.Failure<UserResponseDto, AuthError>(new UserNotFoundError("User not found"));
    }

    public async Task<Result<Unit,AuthError>> DeleteUserAsync(Ulid id)
    {
        return await repository.FindByIdAsync(id) is { } user
        ? await Result.Success<Unit, AuthError>(Unit.Value).TapAsync(async _ => {
            logger.LogInformation("Deletando usuario con ID: {Id}", id);
            await repository.DeleteAsync(id);
            if (user.ImageUrl != CloudinaryConstants.DEFAULT_IMAGE_USUARIOS) 
                await cloudinary.DeleteAsync(user.CloudinaryPublicId!);
        })
        : Result.Failure<Unit, AuthError>(new UserNotFoundError("User not found"))
            .TapError((() => logger.LogInformation("Deletando usuario con ID: {Id}", id)));
        
    }

    public async Task<Result<bool, AuthError>> GiveRoleToUserAsync(Ulid id, string role)
    {
        return await repository.UserChageRoleAsync(id, role)
            ? Result.Success<bool, AuthError>(true)
            : Result.Failure<bool, AuthError>(new AuthError("User not found or user with that role"));
    }

    public async Task<PageResponseDto<UserWithIdDto>> GetAllUsersAsync(FilterUserDto filter)
    {
        var paged = await repository.FindAllAsync(filter);
        int totalPages = filter.Size > 0 ? (int)Math.Ceiling(paged.TotalCount / (double)filter.Size) : 0;
        return new PageResponseDto<UserWithIdDto>(
            Content: paged.Items.Select(item => item.ToDtoWithId(cloudinary)).ToList(),
            TotalPages: totalPages,
            TotalElements: paged.TotalCount,
            PageSize: filter.Size,
            PageNumber: filter.Page,
            TotalPageElements: paged.Items.Count(),
            SortBy: filter.SortBy,
            Direction: filter.Direction
        );
    }

    public async Task<Result<UserResponseDto, AuthError>> PatchUserAsync(Ulid id, UserRequestDto request)
    {
        logger.LogInformation("Actualizando usuario con ID: {Id}", id);

        var user = await repository.FindByIdAsync(id);
        if (user is null)
            return Result.Failure<UserResponseDto, AuthError>(new UserNotFoundError("User not found"));

        string? username = request.Username?.Trim();
        string? email = request.Email?.Trim();
        if(request.Avatar is not null) {
            if(user.ImageUrl != CloudinaryConstants.DEFAULT_IMAGE_USUARIOS) await cloudinary.DeleteAsync(user.CloudinaryPublicId!);
            var upload= await cloudinary.UploadWithAutoNameAsync(request.Avatar,id.ToString(),CloudinaryConstants.FOLDER_USUARIOS);
            user.ImageUrl = upload.ImageUrl;
            user.CloudinaryPublicId = upload.PublicId;
        }

        if (request.Name is not null && request.Name.Trim().Length > 0)
            user.Name = request.Name.Trim();

        if (request.Telefono is not null)
            user.Phone = request.Telefono.Trim();

        if (!string.IsNullOrEmpty(username))
            user.Username = username;

        if (!string.IsNullOrEmpty(email))
            user.Email = email;

        var updated = await repository.UpdateAsync(user);
        return Result.Success<UserResponseDto, AuthError>(updated.ToDto(cloudinary));
    }

    public async Task<Result<UserResponseDto, AuthError>> CreateContacto(ContactoPostRequestDto request) {
        logger.LogInformation("Creando contacto para el usuario con nombre: {Name}",request.Name);
        return await repository.SaveAsync(request.ToModel()) is { } user
            ? Result.Success<UserResponseDto, AuthError>(user.ToDto(cloudinary))
                .Tap((() => logger.LogInformation("Contacto creado con ID: {Id}", user.Id)))
            : Result.Failure<UserResponseDto, AuthError>(new ConflictError("Error creating contact"))
                .TapError((() => logger.LogError("Error creating contact for user with name: {Name}", request.Name)));
    }

    public async Task<Result<Unit, AuthError>> CreateEncoderAsync(Ulid userId) {
        logger.LogInformation("Creando encordador para usuario con ID: {UserId}", userId);
        
        var user = await repository.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<Unit, AuthError>(new UserNotFoundError("User not found"));
        
        if (user.Role == User.UserRoles.ENCORDER)
            return Result.Failure<Unit, AuthError>(new ConflictError("User already is an encorder"));
        
        user.Role = User.UserRoles.ENCORDER;
        
        await repository.UpdateAsync(user);
        logger.LogInformation("Usuario {UserId} asignado como ENCORDER", userId);
        
        return Result.Success<Unit, AuthError>(Unit.Value);
    }
}