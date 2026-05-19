namespace BackEncordados.Excel.Dto;

public class ExcelMaterialsDto
{
    public long Id { get; set; }
    public string TournamentId { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Stock { get; set; }
    public double Precio { get; set; }
    public string Type { get; set; } = string.Empty;
}