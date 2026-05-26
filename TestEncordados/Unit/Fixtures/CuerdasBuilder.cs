using BackEncordados.Materials.Model;

namespace TestEncordados.Unit.Fixtures;

public static class CuerdasBuilder
{
    public static Cuerdas Create(
        long? id = null,
        Ulid? tournamentId = null,
        string marca = "Babolat",
        string modelo = "Pro Tour",
        int stock = 10,
        double precio = 25.99,
        double calibre = 1.25,
        FormatoCuerda stringFormat = FormatoCuerda.Reel,
        StringsType stringsType = StringsType.Polyester,
        bool isDeleted = false)
    {
        return new Cuerdas
        {
            Id = id ?? 1L,
            TournamentId = tournamentId ?? Ulid.NewUlid(),
            Marca = marca,
            Modelo = modelo,
            Stock = stock,
            Precio = precio,
            Calibre = calibre,
            StringFormat = stringFormat,
            StringsType = stringsType,
            IsDeleted = isDeleted
        };
    }

    public static Cuerdas PolyesterReel(Ulid? tournamentId = null) =>
        Create(tournamentId: tournamentId, stringFormat: FormatoCuerda.Reel, stringsType: StringsType.Polyester);

    public static Cuerdas SyntheticGutSet(Ulid? tournamentId = null) =>
        Create(tournamentId: tournamentId, stringFormat: FormatoCuerda.Set, stringsType: StringsType.SyntheticGut);

    public static Cuerdas NaturalGutReel(Ulid? tournamentId = null) =>
        Create(tournamentId: tournamentId, stringFormat: FormatoCuerda.Reel, stringsType: StringsType.NaturalGut);

    public static Cuerdas WithStock(int stock, Ulid? tournamentId = null) =>
        Create(tournamentId: tournamentId, stock: stock);

    public static Cuerdas WithPrice(double price, Ulid? tournamentId = null) =>
        Create(tournamentId: tournamentId, precio: price);
}