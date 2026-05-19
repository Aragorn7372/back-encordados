namespace BackEncordados.Excel.Dto;

public class ExcelCuerdasDto
{
    public long Id { get; set; }
    public string TournamentId { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Stock { get; set; }
    public double Precio { get; set; }
    public string StringFormat { get; set; } = string.Empty;
    public string StringsType { get; set; } = string.Empty;
}