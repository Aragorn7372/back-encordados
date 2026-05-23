namespace BackEncordados.Materials.Dto.Strings;

public record CuerdaResponseDto(
  long Id,
  Ulid TournamentId,
  string Marca,
  string Modelo,
  int Stock,
  double Precio,
  string StringFormat,
  string StringsType,
  string ImageUrl
  );