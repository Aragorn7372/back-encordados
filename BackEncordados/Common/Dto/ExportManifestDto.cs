namespace BackEncordados.Common.Dto;

public class ExportManifestDto
{
    public string Version { get; set; } = "1.0";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = "Full database export";
    public List<ExportEntityInfo> Entities { get; set; } = new();
}

public class ExportEntityInfo
{
    public string Name { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public string FileName { get; set; } = string.Empty;
}