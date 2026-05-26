using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Talleres.Dto;

/// <summary>
/// DTO de respuesta con información detallada de un torneo, incluyendo usuarios, propietario y supervisores.
/// </summary>
/// <remarks>
/// <para>Extiende <see cref="TournamentResponseDto"/> añadiendo listas de usuarios asociados,
/// el propietario del torneo y los supervisores asignados.</para>
/// <para>Se utiliza en respuestas de endpoints que requieren visibilidad completa de la estructura del torneo.</para>
/// </remarks>
/// <param name="Id">Identificador ULID del torneo.</param>
/// <param name="Name">Nombre del torneo.</param>
/// <param name="StartDate">Fecha de inicio del torneo.</param>
/// <param name="EndDate">Fecha de finalización del torneo.</param>
/// <param name="Logotype">URL pública del logotipo del torneo.</param>
/// <param name="User">Lista de usuarios (rol USER) asociados al torneo.</param>
/// <param name="Owner">Propietario del torneo.</param>
/// <param name="Supevisors">Lista de supervisores asignados al torneo.</param>
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