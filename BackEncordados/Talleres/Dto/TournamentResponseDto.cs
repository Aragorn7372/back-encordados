namespace BackEncordados.Talleres.Dto;

public record TournamentResponseDto(
    long Id,
    string Name,
    DateTime EndTournament,
    DateTime StartTournament,
    string Logotype);