namespace BackEncordados.Materials.Dto.Materials;

public record MaterialFilterDto(
    string Search,
    int Page = 0,
    int Size = 10,
    string SortBy = "name",
    string Direction = "asc");