using BackEncordados.Materials.Model;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Model;

namespace BackEncordados.Export.Dto;

public class ExportDataDto
{
    public List<User> Users { get; set; } = new();
    public List<Tournaments> Tournaments { get; set; } = new();
    public List<Material> Materials { get; set; } = new();
    public List<Cuerdas> Cuerdas { get; set; } = new();
    public List<Pedidos> Pedidos { get; set; } = new();
}