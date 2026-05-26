namespace BackEncordados.Usuarios.Dto;

public record UserWithIdDto(
    string UserId,
    string Username,
    string ImageUrl,
    string Name,
    string Email,
    string Role,
    string? TournamentId
);
