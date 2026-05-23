using BackEncordados.Materials.Model;

namespace BackEncordados.Materials.Dto.Materials;

public record MaterialResponseDto(
    long Id,
    Ulid TournamentId,
    string Marca,
    string Modelo,
    int Stock,
    double Precio,
    string MaterialType,
    string ImageUrl
    );