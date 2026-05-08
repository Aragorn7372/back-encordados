namespace BackEncordados.Materials.Errors;

public record CuerdaError(string Error)
{
    public string Error {get; set;} = Error;
};
public record ConflictError(string Error):CuerdaError(Error);

public record CuerdaNotFoundError(string Error="Cuerda not found"):CuerdaError(Error);
public record ValidationError(string Error):CuerdaError(Error);

