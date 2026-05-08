using BackEncordados.Materials.Model;

namespace BackEncordados.Materials.Dto.Materials;

public record MaterialResponseDto(
    long Id,
    string Marca,
    string Modelo,
    int Stock,
    double Precio,
    string MaterialType
    );