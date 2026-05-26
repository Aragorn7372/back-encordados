namespace BackEncordados.Usuarios.Dto;

/// <summary>
/// DTO de respuesta con la información mínima de un usuario para vistas públicas o listados.
/// </summary>
/// <remarks>
/// <para>Contiene los datos esenciales: nombre de usuario, URL de avatar, nombre completo y bonos acumulados.</para>
/// <para>Se utiliza en respuestas de endpoints que no requieren exponer datos sensibles como email o teléfono.</para>
/// </remarks>
/// <param name="Username">Nombre de usuario único.</param>
/// <param name="ImageUrl">URL pública de la imagen de avatar.</param>
/// <param name="Name">Nombre completo del usuario.</param>
/// <param name="Bonos">Cantidad de bonos acumulados por el usuario.</param>
public record UserResponseDto(
    string Username,
    string ImageUrl,
    string Name,
    double Bonos
    );