using BackEncordados.Common.Dto;
using BackEncordados.Common.Utils;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using CSharpFunctionalExtensions;

namespace BackEncordados.Usuarios.Service.CrudService;

public interface IUserService
{
    Task<Result<UserResponseDto, AuthError>> FindByIdAsync(Ulid id);
    Task<Result<Unit, AuthError>> DeleteUserAsync(Ulid id);
    Task<Result<bool,AuthError>> GiveRoleToUserAsync(Ulid id, string role);
    Task<PageResponseDto<UserWithIdDto>> GetAllUsersAsync(FilterUserDto filter);
    Task<Result<UserResponseDto, AuthError>> PatchUserAsync(Ulid id, UserRequestDto request);
    Task<Result<UserResponseDto, AuthError>> CreateContacto(ContactoPostRequestDto request);
    Task<Result<Unit, AuthError>> CreateEncoderAsync(Ulid userId);
}