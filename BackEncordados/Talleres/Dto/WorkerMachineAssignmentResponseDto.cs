using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Talleres.Dto;

public record WorkerMachineAssignmentResponseDto(
    string MachineName,
    UserResponseDto User
    );