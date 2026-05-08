using BackEncordados.Usuarios.Errors;

namespace BackEncordados.Materials.Errors;

public record MaterialError(string Error)
{
    public string Error { get; set; } = Error;
};
public record MaterialConflictError(string Error):MaterialError(Error);

public record MaterialNotFoundError(string Error="Material not found"):MaterialError(Error);
public record MaterialValidationError(string Error):MaterialError(Error);


