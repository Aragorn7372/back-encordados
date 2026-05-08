namespace BackEncordados.Materials.Dto.Strings;

public record CuerdaResponseDto(
  long Id,
  string Marca,
  string Modelo,
  int Stock,
  double Precio,
  string StringFormat,
  string StringsType
  );