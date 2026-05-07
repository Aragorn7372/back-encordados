namespace BackEncordados.Talleres.Dto;

public record FilterTournamentDto(
    string Search,
    Guid? UserId,
    int Page = 0,
    int Size = 10,
    string SortBy = "name",
    string Direction = "asc") {
    public Guid? UserId { get; set; } = UserId;
};