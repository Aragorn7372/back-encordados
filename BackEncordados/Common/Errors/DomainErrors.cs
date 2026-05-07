namespace BackEncordados.Common.Errors;

public record DomainErrors(string Error)
{
    public string Error { get; set; } = Error;
};