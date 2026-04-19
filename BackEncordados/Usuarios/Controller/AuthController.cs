using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Service.Auth;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;

namespace BackEncordados.Usuarios.Controller;

/// <summary>
/// Controlador de API para autenticación de usuarios.
/// Endpoints: SignUp (registro) y SignIn (login) con JWT.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger
) : ControllerBase
{
    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// </summary>
    /// <param name="dto">Datos de registro (username, email, password).</param>
    /// <returns>201 Created con la respuesta de autenticación, o 400/409 si hay errores.</returns>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignUp([FromBody] RegisterDto dto)
    {
        logger.LogInformation("Signup request received for user: {Username}", dto.Username);

        var resultado = await authService.SignUpAsync(dto);

        return resultado.Match(
            response => CreatedAtAction(nameof(SignUp), response),
            error => error switch
            {
                ValidationError validationError => BadRequest(new { message = validationError.Error }),
                ConflictError conflictError => Conflict(new { message = conflictError.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }

    /// <summary>
    /// Inicia sesión y devuelve un token JWT.
    /// </summary>
    /// <param name="dto">Credenciales de acceso (username, password).</param>
    /// <returns>200 OK con el token JWT, o 401 si las credenciales son inválidas.</returns>
    [HttpPost("signin")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn([FromBody] LoginDto dto)
    {
        logger.LogInformation("Petición de inicio de sesión recibida para usuario: {Username}", dto.Username);

        var resultado = await authService.SignInAsync(dto);

        return resultado.Match(
            response => Ok(response),
            error => error switch
            {
                UnauthorizedError unauthorizedError => Unauthorized(new { message = unauthorizedError.Error }),
                ValidationError validationError => BadRequest(new { message = validationError.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }
}