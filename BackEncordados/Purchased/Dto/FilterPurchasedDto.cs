namespace BackEncordados.Purchased.Dto;

public record FilterPurchasedDto(
    bool? isEncorder,
    bool? isUser,
    string? userId,
    string Search,
    int Page = 0,
    int Size = 10,
    string SortBy = "name",
    string Direction = "asc");