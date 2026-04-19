using BackEncordados.Common.Dto;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Mapper;
using BackEncordados.Usuarios.Repository;
using CSharpFunctionalExtensions;

namespace BackEncordados.Usuarios.Service.CrudService;

public class UserService(ILogger<UserService> logger, IUserRepository repository) : IUserService
{
    public async Task<Result<UserResponseDto, AuthError>> FindByIdAsync(Guid id)
    {
        return await repository.FindByIdAsync(id) is { } user
            ? Result.Success<UserResponseDto, AuthError>(user.ToDto())
            : Result.Failure<UserResponseDto, AuthError>(new AuthError("User not found"));
    }

    public Task DeleteUserAsync(Guid id)
    {
        return repository.DeleteAsync(id);
    }

    public async Task<Result<bool, AuthError>> GiveRoleToUserAsync(Guid id, string role)
    {
        return await repository.UserChageRoleAsync(id, role)
            ? Result.Success<bool, AuthError>(true)
            : Result.Failure<bool, AuthError>(new AuthError("User not found or user with that role"));
    }

    public async Task<PageResponseDto<UserResponseDto>> GetAllUsersAsync(FilterUserDto filter)
    {
        var paged = await repository.FindAllAsync(filter);
        int totalPages = filter.Size > 0 ? (int)Math.Ceiling(paged.TotalCount / (double)filter.Size) : 0;
        return new PageResponseDto<UserResponseDto>(
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

    public async Task<Result<UserResponseDto, AuthError>> PatchUserAsync(Guid id, UserRequestDto request)
    {
        logger.LogInformation("Actualizando usuario con ID: {Id}", id);

        var user = await repository.FindByIdAsync(id);
        if (user is null)
            return Result.Failure<UserResponseDto, AuthError>(new UserNotFoundError("User not found"));

        string? username = request.Username?.Trim();
        string? email = request.Email?.Trim();

        if (!string.IsNullOrEmpty(email) && !email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var otherByEmail = await repository.FindByEmailAsync(email);
            if (otherByEmail is not null && otherByEmail.Id != user.Id)
                return Result.Failure<UserResponseDto, AuthError>(new ConflictError("Email already in use"));
        }

        if (!string.IsNullOrEmpty(username) && !username.Equals(user.Username, StringComparison.OrdinalIgnoreCase))
        {
            var otherByUsername = await repository.FindByUsernameAsync(username);
            if (otherByUsername is not null && otherByUsername.Id != user.Id)
                return Result.Failure<UserResponseDto, AuthError>(new ConflictError("Username already in use"));
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
        return Result.Success<UserResponseDto, AuthError>(updated.ToDto());
    }
}