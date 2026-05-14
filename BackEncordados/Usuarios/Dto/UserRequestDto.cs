using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BackEncordados.Usuarios.Dto;

public class UserRequestDto
{
    [MinLength(1, ErrorMessage = "El nombre debe de tener más de un caracter")]
    public string? Name { get; set; } = default!;

    [EmailAddress]
    public string? Email { get; set; } = default!;

    private string? _telefono;

    [RegularExpression(@"^(\d{9})?$", ErrorMessage = "El teléfono debe tener 9 dígitos o estar vacío")]
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

            // Eliminar espacios, guiones, paréntesis, etc.
            string cleaned = Regex.Replace(value, @"[\s\-().]", "");

            // Si empieza con +34, quitarlo
            if (cleaned.StartsWith("+34"))
                cleaned = cleaned.Substring(3);
            // Si empieza con 0034, quitarlo
            else if (cleaned.StartsWith("0034"))
                cleaned = cleaned.Substring(4);
            // Si empieza con 34 y tiene más de 9 dígitos
            else if (cleaned.StartsWith("34") && cleaned.Length > 9)
                cleaned = cleaned.Substring(2);

            _telefono = cleaned;
        }
    }
    [MinLength(1, ErrorMessage = "El nombre de usuario debe tener mas de 1 letra")]
    public string? Username { get; set; }
    public IFormFile? Avatar { get; set; }
}