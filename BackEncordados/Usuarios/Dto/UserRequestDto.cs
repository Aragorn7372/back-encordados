using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BackEncordados.Usuarios.Dto;

/// <summary>
/// DTO de solicitud para crear o actualizar un usuario.
/// </summary>
/// <remarks>
/// <para>Todos los campos son opcionales, permitiendo actualizaciones parciales.</para>
/// <para>La propiedad <see cref="Telefono"/> aplica una limpieza automática eliminando caracteres no numéricos
/// y el prefijo '+' internacional, almacenando solo dígitos.</para>
/// <para>El campo <see cref="Avatar"/> acepta un archivo de imagen para la foto de perfil.</para>
/// </remarks>
public class  UserRequestDto
{
    /// <summary>Nombre completo del usuario. Mínimo 1 carácter.</summary>
    [MinLength(1, ErrorMessage = "El nombre debe de tener más de un caracter")]
    public string? Name { get; set; } = default!;

    /// <summary>Dirección de correo electrónico. Debe tener formato válido si se proporciona.</summary>
    [EmailAddress]
    public string? Email { get; set; } = default!;

    private string? _telefono;

    /// <summary>Número de teléfono con prefijo internacional (ej: 34612345678). Se limpia automáticamente de caracteres no dígito.</summary>
    [RegularExpression(@"^[1-9]\d{6,14}$", ErrorMessage = "El teléfono debe ser un número válido con prefijo internacional (ej: 34612345678, 15551234567)")]
    public string? Telefono
    {
        get => _telefono;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _telefono = "";
                return;
            }

            string cleaned = Regex.Replace(value, @"[^\d+]", "");

            if (cleaned.StartsWith('+'))
                cleaned = cleaned.Substring(1);

            _telefono = cleaned;
        }
    }
    /// <summary>Nombre de usuario único. Mínimo 1 carácter.</summary>
    [MinLength(1, ErrorMessage = "El nombre de usuario debe tener mas de 1 letra")]
    public string? Username { get; set; }
    /// <summary>Archivo de imagen para el avatar del usuario.</summary>
    public IFormFile? Avatar { get; set; }
}