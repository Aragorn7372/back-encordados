namespace BackEncordados.Excel.Dto;

public class ExcelAdvancedDataDto
{
    public List<ExcelUsersDto> Users { get; set; } = new();
    public List<ExcelMaterialsDto> Materials { get; set; } = new();
    public List<ExcelCuerdasDto> Cuerdas { get; set; } = new();
    public List<ExcelTournamentDto> Tournament { get; set; } = new();
    public List<ExcelPedidosDto> Pedidos { get; set; } = new();
    public List<ExcelPedidoLineasDto> PedidoLineas { get; set; } = new();
}