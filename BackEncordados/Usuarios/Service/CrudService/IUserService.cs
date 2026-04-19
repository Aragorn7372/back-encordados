using BackEncordados.Common.Dto;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using CSharpFunctionalExtensions;

namespace BackEncordados.Usuarios.Service.CrudService;

public interface IUserService
{
    Task<Result<UserResponseDto, AuthError>> FindByIdAsync(Guid id);
    Task DeleteUserAsync(Guid id);
    Task<Result<bool,AuthError>> GiveRoleToUserAsync(Guid id, string role);
    Task<PageResponseDto<UserResponseDto>> GetAllUsersAsync(FilterUserDto filter);
    Task<Result<UserResponseDto, AuthError>> PatchUserAsync(Guid id, UserRequestDto request);
}