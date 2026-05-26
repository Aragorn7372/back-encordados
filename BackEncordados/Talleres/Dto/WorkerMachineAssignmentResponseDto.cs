using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Talleres.Dto;

/// <summary>
/// DTO de respuesta con la información de una asignación trabajador-máquina.
/// </summary>
/// <remarks>
/// <para>Incluye el nombre de la máquina y los datos del usuario asignado
/// (username, nombre, imagen y bonos).</para>
/// <para>Se utiliza en las respuestas de endpoints que listan asignaciones de máquinas
/// dentro de un torneo.</para>
/// </remarks>
/// <param name="MachineName">Nombre de la máquina asignada.</param>
/// <param name="User">DTO con la información pública del trabajador asignado.</param>
public record WorkerMachineAssignmentResponseDto(
    string MachineName,
    UserResponseDto User
    );