using BackEncordados.Common.Errors;

namespace BackEncordados.Usuarios.Errors;

/// <summary>
/// Errores de autenticación y registro (HTTP 401, 409, 400).
/// </summary>
public record AuthError(string Error) : DomainErrors(Error);


/// <summary>Crea error para credenciales inválidas.</summary>
/// <returns>UnauthorizedError (HTTP 401).</returns>
public record UnauthorizedError(string Error): AuthError(Error);
   

/// <summary>Crea error para username duplicado.</summary>
/// <returns>ConflictError (HTTP 409).</returns>
public record ConflictError(string Error):AuthError(Error);
       
/// <summary>
/// Error cuando no se encuentra un usuario en el sistema.
/// Se produce al buscar un usuario por Id, email o username que no existe en la base de datos.
/// </summary>
/// <remarks>
/// <para>Se utiliza en operaciones como asignación de torneos, búsqueda de perfil,
/// y verificación de existencia antes de operaciones de actualización.</para>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 404 Not Found.</para>
/// </remarks>
/// <example>new UserNotFoundError("Usuario con email juan@example.com no encontrado")</example>
public record UserNotFoundError(string Error) : AuthError(Error);


/// <summary>Crea error de validación simple (email inválido, password muy corto, etc.).</summary>
/// <param name="Error">Descripción del error de validación.</param>
/// <returns>ValidationError (HTTP 400).</returns>
public record ValidationError(string Error): AuthError(Error);

/// <summary>
/// Error cuando el tiempo límite para cambiar la contraseña ha expirado,
/// o cuando no se encuentra el usuario que solicitó el cambio.
/// </summary>
/// <remarks>
/// <para>Mensaje por defecto: "el para cambiar la contraseña expiró o no se ha encontrado el usuario
/// vuelva a intentarlo en otro momento o vuelva a solicitar el cambio de contraseña".</para>
/// <para>Se produce en el flujo de recuperación de contraseña cuando el token
/// de restablecimiento ha expirado o el correo del usuario no existe.</para>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 400 Bad Request.</para>
/// </remarks>
/// <example>new PasswordChangeExpiredTimeout()</example>
public record PasswordChangeExpiredTimeout(string Error="el para cambiar la contraseña expiró o no se ha encontrado el usuario vuelva a intentarlo en otro momento o vuelva a solicitar el cambio de contraseña") : AuthError(Error);