namespace BackEncordados.Excel.Dto;

public class TournamentExcelRowDto
{
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RacketCount { get; set; }
    public decimal TotalPrice { get; set; }
}