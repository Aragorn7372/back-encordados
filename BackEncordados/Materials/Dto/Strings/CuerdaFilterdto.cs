namespace BackEncordados.Materials.Dto.Strings;

public record CuerdaFilterdto(
    long? TournamentId,
    string Search,
    int Page = 0,
    int Size = 10,
    string SortBy = "name",
    string Direction = "asc");