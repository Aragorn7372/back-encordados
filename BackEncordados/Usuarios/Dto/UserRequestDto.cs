using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BackEncordados.Usuarios.Dto;

public class  UserRequestDto
{
    [MinLength(1, ErrorMessage = "El nombre debe de tener más de un caracter")]
    public string? Name { get; set; } = default!;

    [EmailAddress]
    public string? Email { get; set; } = default!;

    private string? _telefono;

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
    [MinLength(1, ErrorMessage = "El nombre de usuario debe tener mas de 1 letra")]
    public string? Username { get; set; }
    public IFormFile? Avatar { get; set; }
}