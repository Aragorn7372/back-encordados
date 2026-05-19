namespace BackEncordados.Talleres.Dto;

public record TournamentResponseDto(
    Ulid Id,
    string Name,
    DateTime EndTournament,
    DateTime StartTournament,
    string Logotype);