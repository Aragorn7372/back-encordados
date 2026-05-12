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
       
public record UserNotFoundError(string Error) : AuthError(Error);


/// <summary>Crea error de validación simple.</summary>
/// <param name="Error">Mensaje de error.</param>
/// <returns>ValidationError (HTTP 400).</returns>
public record ValidationError(string Error): AuthError(Error);
public record PasswordChangeExpiredTimeout(string Error="el para cambiar la contraseña expiró o no se ha encontrado el usuario vuelva a intentarlo en otro momento o vuelva a solicitar el cambio de contraseña") : AuthError(Error);