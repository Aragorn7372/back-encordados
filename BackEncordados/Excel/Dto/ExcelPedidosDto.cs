namespace BackEncordados.Excel.Dto;

public class ExcelPedidosDto
{
    public string Id { get; set; } = string.Empty;
    public string TournamentId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string Machine { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public double Price { get; set; }
    public string PayStatus { get; set; } = string.Empty;
}