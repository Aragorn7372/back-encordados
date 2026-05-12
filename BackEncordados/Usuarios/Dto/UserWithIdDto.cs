namespace BackEncordados.Usuarios.Dto;

public record UserWithIdDto(
    string UserId,
    string Username,
    string ImageUrl,
    string Name
);