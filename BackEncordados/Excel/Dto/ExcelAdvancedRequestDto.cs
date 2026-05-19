namespace BackEncordados.Excel.Dto;

public class ExcelAdvancedRequestDto
{
    public Ulid TournamentId { get; set; }
    public List<string> Types { get; set; } = new() { "users", "materials", "cuerdas", "tournament", "pedidos" };
    public Dictionary<string, List<string>>? Fields { get; set; }
}