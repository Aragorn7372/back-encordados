namespace BackEncordados.Talleres.Dto;

/// <summary>
/// DTO de respuesta con la información básica de un torneo.
/// </summary>
/// <remarks>
/// <para>Contiene los datos esenciales del torneo: identificador, nombre, fechas y logotipo.</para>
/// <para>Se utiliza en respuestas de listados y vistas resumidas.</para>
/// </remarks>
/// <param name="Id">Identificador ULID del torneo.</param>
/// <param name="Name">Nombre del torneo.</param>
/// <param name="EndTournament">Fecha de finalización del torneo.</param>
/// <param name="StartTournament">Fecha de inicio del torneo.</param>
/// <param name="Logotype">URL pública del logotipo del torneo.</param>
public record TournamentResponseDto(
    Ulid Id,
    string Name,
    DateTime EndTournament,
    DateTime StartTournament,
    string Logotype);