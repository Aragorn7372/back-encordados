using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Talleres.Model;

namespace TestEncordados.Unit.Fixtures;

public static class TournamentBuilder
{
    public static Tournaments Create(
        Ulid? id = null,
        Ulid? owner = null,
        string title = "Test Tournament",
        bool isDeleted = false)
    {
        return new Tournaments
        {
            Id = id ?? Ulid.NewUlid(),
            Owner = owner ?? Ulid.NewUlid(),
            Title = title,
            StartTournament = DateTime.UtcNow,
            EndTournament = DateTime.UtcNow.AddDays(7),
            IsDeleted = isDeleted,
            Logotype = "https://res.cloudinary.com/test/image/upload/v1/defaults/tournament.png",
            WorkersList = new List<Ulid>(),
            SupervisorList = new List<Ulid>(),
            WorkerMachineAssignments = new List<WorkerMachineAssignment>()
        };
    }

    public static Tournaments ActiveTournament(Ulid? id = null) =>
        Create(id: id, isDeleted: false);

    public static Tournaments DeletedTournament(Ulid? id = null) =>
        Create(id: id, isDeleted: true);

    public static Tournaments WithOwner(Ulid owner) =>
        Create(owner: owner);
}