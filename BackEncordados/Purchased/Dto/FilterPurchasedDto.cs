namespace BackEncordados.Purchased.Dto;

public record FilterPurchasedDto(
    bool? IsEncorder,
    bool? IsUser,
    string? UserId,
    Ulid? TournamentId,
    string Search,
    int Page = 0,
    int Size = 10,
    string SortBy = "createdAt",
    string Direction = "desc") {
    public string? UserId { get; set; } = UserId;
    public bool? IsEncorder { get; set; } = IsEncorder;
    public bool? IsUser { get; set; } = IsUser;
    public Ulid? TournamentId { get; set; } = TournamentId;
};