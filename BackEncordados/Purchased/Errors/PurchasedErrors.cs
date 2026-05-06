using BackEncordados.Common.Errors;

namespace BackEncordados.Purchased.Errors;

public record PurchasedErrors(string Error): DomainErrors(Error);
public record ConflictError(string Error):PurchasedErrors(Error);
       
public record PurchasedNotFoundError(string Error="Purchased not found") : PurchasedErrors(Error);


public record ValidationError(string Error): PurchasedErrors(Error);

public record InvalidStatusError(string Error) : PurchasedErrors(Error);