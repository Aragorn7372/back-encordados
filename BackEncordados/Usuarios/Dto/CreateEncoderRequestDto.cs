using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Usuarios.Dto;

/// <summary>
/// DTO de solicitud para promocionar un usuario existente al rol de Encordador (ENCORDER).
/// </summary>
/// <remarks>
/// <para>El usuario identificado por <see cref="UserId"/> debe existir previamente en el sistema.</para>
/// <para>La operación asigna el rol <c>ENCORDER</c> y puede requerir permisos de administrador.</para>
/// </remarks>
public class CreateEncoderRequestDto
{
    /// <summary>Identificador ULID del usuario que será promocionado a Encordador.</summary>
    [Required]
    public Ulid UserId { get; set; }
}