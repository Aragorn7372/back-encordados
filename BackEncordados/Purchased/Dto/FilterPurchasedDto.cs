namespace BackEncordados.Purchased.Dto;

public record FilterPurchasedDto(
    bool? IsEncorder,
    bool? IsUser,
    string? UserId,
    string Search,
    int Page = 0,
    int Size = 10,
    string SortBy = "name",
    string Direction = "asc");