using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Service.Auth;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;

namespace BackEncordados.Usuarios.Controller;

/// <summary>
/// Controlador de API para autenticación de usuarios.
/// </summary>
/// <remarks>
/// <para>Proporciona cuatro endpoints de autenticación:</para>
/// <list type="table">
///   <listheader>
///     <term>Endpoint</term>
///     <description>Método</description>
///     <description>Uso</description>
///   </listheader>
///   <item>
///     <term><c>POST api/auth/signup</c></term>
///     <description><c>SignUp</c></description>
///     <description>Registro de nuevo usuario con username, email y password.</description>
///   </item>
///   <item>
///     <term><c>POST api/auth/signin</c></term>
///     <description><c>SignIn</c></description>
///     <description>Inicio de sesión con credenciales, devuelve JWT.</description>
///   </item>
///   <item>
///     <term><c>POST api/auth/change-password-request</c></term>
///     <description><c>SentEmailRequest</c></description>
///     <description>Solicitud de cambio de contraseña vía email.</description>
///   </item>
///   <item>
///     <term><c>POST api/auth/change-password/{id}</c></term>
///     <description><c>ChangePassword</c></description>
///     <description>Ejecuta el cambio de contraseña con GUID de verificación.</description>
///   </item>
/// </list>
/// <para>Los errores se mapean: 400 (validación), 401 (credenciales inválidas), 404 (no encontrado), 409 (conflicto), 500 (error interno).</para>
/// </remarks>
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
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>authService.SignUpAsync</c> con los datos de registro.</description></item>
    ///   <item><description>Si es exitoso, retorna 201 Created con el token JWT y datos del usuario.</description></item>
    ///   <item><description>Si hay validación fallida → 400 BadRequest.</description></item>
    ///   <item><description>Si el username o email ya existen → 409 Conflict.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="dto">Datos de registro (username, email, password).</param>
    /// <returns>201 Created con AuthResponseDto (token + datos de usuario).</returns>
    /// <response code="201">Usuario registrado correctamente.</response>
    /// <response code="400">Error de validación en los campos.</response>
    /// <response code="409">El username o email ya están en uso.</response>
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
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>authService.SignInAsync</c> con las credenciales.</description></item>
    ///   <item><description>Si las credenciales son válidas, retorna 200 OK con el token JWT.</description></item>
    ///   <item><description>Si el usuario no existe o la contraseña es incorrecta → 401 Unauthorized.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="dto">Credenciales de acceso (username, password).</param>
    /// <returns>200 OK con AuthResponseDto (token + datos de usuario).</returns>
    /// <response code="200">Inicio de sesión exitoso.</response>
    /// <response code="401">Credenciales inválidas.</response>
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

    /// <summary>
    /// Solicita un email de restablecimiento de contraseña.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>authService.GetEmailAsync</c> con la dirección de email.</description></item>
    ///   <item><description>Si el email existe, almacena una clave de caché con expiración de 60 minutos y envía el email.
    ///   <c>Ok()</c> se retorna tanto si el email existe como si no (por seguridad, para no revelar qué emails están registrados).</description></item>
    ///   <item><description>Si el email no existe, se retorna NotFound para depuración (comportamiento actual).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="email">Dirección de correo electrónico del usuario.</param>
    /// <response code="200">Email de recuperación enviado (o email no encontrado, dependiendo de configuración).</response>
    /// <response code="404">Email no encontrado en el sistema.</response>
    [HttpPost("change-password-request")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SentEmailRequest([FromBody] string email)
    {
        logger.LogInformation("enviando email de recuperacion a {Email}", email);
        var resultado = await authService.GetEmailAsync(email);
        
        if (resultado.IsSuccess) return Ok();
        

        var error = resultado.Error;
        return error switch
        {
            UserNotFoundError => NotFound(new { message = error.Error }),
            _ => StatusCode(500, new { message = error.Error })
        };
    }

    /// <summary>
    /// Ejecuta el cambio de contraseña usando el GUID de verificación.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>authService.ChangePasswordAsync</c> con el GUID y el DTO de nueva contraseña.</description></item>
    ///   <item><description>Si es exitoso, retorna 204 NoContent y la clave de caché se invalida.</description></item>
    ///   <item><description>Si el GUID expiró o es inválido → 400 BadRequest con <see cref="PasswordChangeExpiredTimeout"/>.</description></item>
    ///   <item><description>Si se intenta usar una contraseña anterior → 400 BadRequest con <see cref="ValidationError"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">GUID de verificación recibido por email.</param>
    /// <param name="dto">DTO con nueva contraseña y confirmación.</param>
    /// <response code="204">Contraseña actualizada correctamente.</response>
    /// <response code="400">GUID expirado, inválido, o contraseña repetida.</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpPost("change-password/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequestDto dto) {
        logger.LogInformation("cambiando contraseña del usuario con id {Id}", id);
        var resultado = await authService.ChangePasswordAsync(id, dto);

        if (resultado.IsSuccess) return NoContent();


        var error = resultado.Error;
        return error switch {
            PasswordChangeExpiredTimeout => BadRequest(new { message = error.Error }),
            UserNotFoundError => NotFound(new { message = error.Error }),
            ValidationError validationError => BadRequest(new { message = validationError.Error }),
            _ => StatusCode(500, new { message = error.Error })
        };
    }

}