using BackEncordados.Common.Utils;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using CSharpFunctionalExtensions;

namespace BackEncordados.Usuarios.Service.Auth;

/// <summary>
/// Contrato del servicio de autenticación.
/// </summary>
public interface IAuthService
{
    /// <summary>Registra un nuevo usuario.</summary>
    /// <param name="dto">Datos de registro.</param>
    /// <returns>Resultado con respuesta de autenticación.</returns>
    Task<Result<AuthResponseDto, AuthError>> SignUpAsync(RegisterDto dto);

    /// <summary>Inicia sesión con credenciales.</summary>
    /// <param name="dto">Credenciales de acceso.</param>
    /// <returns>Resultado con respuesta de autenticación.</returns>
    Task<Result<AuthResponseDto, AuthError>> SignInAsync(LoginDto dto);
    
    Task<Result<Unit, AuthError>> ChangePasswordAsync(Guid guid,ChangePasswordRequestDto dto);
    Task<Result<Unit, AuthError>> GetEmailAsync(string userEmail);
}