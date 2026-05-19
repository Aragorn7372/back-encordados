using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Talleres.Dto;

public record TournamentResponseDetailsDto(
    Ulid Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string Logotype,
    List<UserResponseDto> User,
    UserResponseDto Owner,
    List<UserResponseDto> Supevisors
    );