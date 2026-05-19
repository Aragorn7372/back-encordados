namespace BackEncordados.Excel.Dto;

public class ExcelTournamentDto
{
    public string Id { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartTournament { get; set; }
    public DateTime EndTournament { get; set; }
    public string Logotype { get; set; } = string.Empty;
    public string WorkersList { get; set; } = string.Empty;
    public string SupervisorList { get; set; } = string.Empty;
}