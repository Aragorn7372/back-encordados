namespace BackEncordados.Excel.Dto;

/// <summary>
/// DTO que representa los datos generales de un torneo en la exportación a Excel.
/// </summary>
/// <remarks>
/// <para>Corresponde a la hoja "Torneo" en el archivo Excel de exportación avanzada.</para>
/// <para>Incluye información del torneo como fechas, responsable (owner),
/// listas de personal (<see cref="WorkersList"/>, <see cref="SupervisorList"/>)
/// y la URL del logotipo (<see cref="Logotype"/>).</para>
/// <para>Las listas de trabajadores y supervisores se almacenan como strings
/// separados por comas dentro de una sola celda.</para>
/// </remarks>
public class ExcelTournamentDto
{
    /// <summary>Identificador único del torneo (ULID en string).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nombre de usuario del propietario del torneo.</summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>Título o nombre del torneo.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Fecha y hora de inicio del torneo.</summary>
    public DateTime StartTournament { get; set; }

    /// <summary>Fecha y hora de finalización del torneo.</summary>
    public DateTime EndTournament { get; set; }

    /// <summary>URL del logotipo del torneo en Cloudinary.</summary>
    public string Logotype { get; set; } = string.Empty;

    /// <summary>Lista de trabajadores asignados al torneo (nombres separados por coma).</summary>
    public string WorkersList { get; set; } = string.Empty;

    /// <summary>Lista de supervisores asignados al torneo (nombres separados por coma).</summary>
    public string SupervisorList { get; set; } = string.Empty;
}