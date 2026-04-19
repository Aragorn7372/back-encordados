namespace BackEncordados.Usuarios.Errors;

/// <summary>
/// Errores de autenticación y registro (HTTP 401, 409, 400).
/// </summary>
public record AuthError(
    string Error)
{
    public string Error { get; set; } = Error;
}

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