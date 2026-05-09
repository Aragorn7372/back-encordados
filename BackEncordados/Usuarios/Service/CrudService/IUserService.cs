using BackEncordados.Common.Dto;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using CSharpFunctionalExtensions;

namespace BackEncordados.Usuarios.Service.CrudService;

public interface IUserService
{
    Task<Result<UserResponseDto, AuthError>> FindByIdAsync(Ulid id);
    Task DeleteUserAsync(Ulid id);
    Task<Result<bool,AuthError>> GiveRoleToUserAsync(Ulid id, string role);
    Task<PageResponseDto<UserResponseDto>> GetAllUsersAsync(FilterUserDto filter);
    Task<Result<UserResponseDto, AuthError>> PatchUserAsync(Ulid id, UserRequestDto request);
}