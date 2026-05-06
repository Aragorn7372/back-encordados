using BackEncordados.Common.Errors;

namespace BackEncordados.Talleres.Error;

public record TournamentsErrors(
    string Error) : DomainErrors(Error);


/// <summary>Crea error para username duplicado.</summary>
/// <returns>ConflictError (HTTP 409).</returns>
public record ConflictError(string Error):TournamentsErrors(Error);
       
public record TournamentNotFoundError(string Error="Tournament not found") : TournamentsErrors(Error);


/// <summary>Crea error de validación simple.</summary>
/// <param name="Error">Mensaje de error.</param>
/// <returns>ValidationError (HTTP 400).</returns>
public record ValidationError(string Error): TournamentsErrors(Error);