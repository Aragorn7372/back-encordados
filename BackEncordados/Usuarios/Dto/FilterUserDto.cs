namespace BackEncordados.Usuarios.Dto;

public record FilterUserDto(
    bool? FindUsers,
    bool? FindEncorders,
    string Search,
    int Page = 0,
    int Size = 10,
    string SortBy = "name",
    string Direction = "asc");
