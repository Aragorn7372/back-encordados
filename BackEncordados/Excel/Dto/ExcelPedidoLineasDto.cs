namespace BackEncordados.Excel.Dto;

public class ExcelPedidoLineasDto
{
    public string Id { get; set; } = string.Empty;
    public string PedidoId { get; set; } = string.Empty;
    public string RaquetModel { get; set; } = string.Empty;
    public byte Nudos { get; set; }
    public DateTime DateString { get; set; }
    public bool Logotype { get; set; }
    public string Color { get; set; } = string.Empty;
    public string StringV { get; set; } = string.Empty;
    public double TensionV { get; set; }
    public short PreStetchV { get; set; }
    public string StringH { get; set; } = string.Empty;
    public double TensionH { get; set; }
    public short PreStetchH { get; set; }
    public string Status { get; set; } = string.Empty;
}