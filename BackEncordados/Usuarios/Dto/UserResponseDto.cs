namespace BackEncordados.Usuarios.Dto;

public record UserResponseDto(
    string Username,
    string Email,
    string PhoneNumber,
    string ImageUrl,
    string Name
    );