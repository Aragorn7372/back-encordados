namespace BackEncordados.Usuarios.Dto;

/// <summary>
/// DTO de respuesta que incluye el identificador del usuario junto con sus datos básicos.
/// </summary>
/// <remarks>
/// <para>Similar a <see cref="UserResponseDto"/> pero incluye el <see cref="UserId"/> como cadena,
/// útil para operaciones administrativas o referencias cruzadas.</para>
/// </remarks>
/// <param name="UserId">Identificador del usuario en formato cadena (ULID serializado).</param>
/// <param name="Username">Nombre de usuario único.</param>
/// <param name="ImageUrl">URL pública de la imagen de avatar.</param>
/// <param name="Name">Nombre completo del usuario.</param>
public record UserWithIdDto(
    string UserId,
    string Username,
    string ImageUrl,
    string Name,
    string Email,
    string Role,
    string? TournamentId,
    double Bonos = 0
);
