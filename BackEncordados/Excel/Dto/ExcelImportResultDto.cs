namespace BackEncordados.Excel.Dto;

public class ExcelImportResultDto
{
    public int UsersCreated { get; set; }
    public int UsersUpdated { get; set; }
    public int MaterialsCreated { get; set; }
    public int MaterialsUpdated { get; set; }
    public int CuerdasCreated { get; set; }
    public int CuerdasUpdated { get; set; }
    public int TournamentsCreated { get; set; }
    public int TournamentsUpdated { get; set; }
    public int PedidosCreated { get; set; }
    public int PedidosUpdated { get; set; }
    public int PedidosLineasUpdated { get; set; }
    public List<string> Errors { get; set; } = new();
}