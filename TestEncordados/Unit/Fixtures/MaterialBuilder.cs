using BackEncordados.Materials.Model;

namespace TestEncordados.Unit.Fixtures;

public static class MaterialBuilder
{
    public static Material Create(
        long? id = null,
        Ulid? tournamentId = null,
        string marca = "Head",
        string modelo = "Pro",
        int stock = 10,
        double precio = 25.99,
        MaterialType type = MaterialType.Grip,
        bool isDeleted = false)
    {
        return new Material
        {
            Id = id ?? 1L,
            TournamentId = tournamentId ?? Ulid.NewUlid(),
            Marca = marca,
            Modelo = modelo,
            Stock = stock,
            Precio = precio,
            Type = type,
            IsDeleted = isDeleted
        };
    }

    public static Material Grip(Ulid? tournamentId = null) =>
        Create(tournamentId: tournamentId, type: MaterialType.Grip);

    public static Material Overgrip(Ulid? tournamentId = null) =>
        Create(tournamentId: tournamentId, type: MaterialType.Overgrip);

    public static Material WithStock(int stock, Ulid? tournamentId = null) =>
        Create(tournamentId: tournamentId, stock: stock);

    public static Material WithPrice(double price, Ulid? tournamentId = null) =>
        Create(tournamentId: tournamentId, precio: price);
}