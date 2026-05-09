namespace BackEncordados.Talleres.Dto;

public record FilterTournamentDto(
    string Search,
    Ulid? UserId,
    int Page = 0,
    int Size = 10,
    string SortBy = "name",
    string Direction = "asc") {
    public Ulid? UserId { get; set; } = UserId;
};