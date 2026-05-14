using BackEncordados.Common.Errors;

namespace BackEncordados.Purchased.Errors;

public record PurchasedErrors(string Error): DomainErrors(Error);
public record ConflictError(string Error):PurchasedErrors(Error);
       
public record PurchasedNotFoundError(string Error="Purchased not found") : PurchasedErrors(Error);


public record ValidationError(string Error): PurchasedErrors(Error);

public record InvalidStatusError(string Error) : PurchasedErrors(Error);

public record ConcurrencyError(string Error = "El usuario fue modificado por otra operación. Intente de nuevo.") : PurchasedErrors(Error);