using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Usuarios.Model;

namespace TestEncordados.Unit.Fixtures;

public static class UserBuilder
{
    public static User Create(
        Ulid? id = null,
        string username = "testuser",
        string name = "Test User",
        string email = "test@example.com",
        string role = User.UserRoles.USER,
        Ulid? tournamentId = null,
        bool isDeleted = false,
        double bonos = 0.0,
        string? imageUrl = null)
    {
        return new User
        {
            Id = id ?? Ulid.NewUlid(),
            Username = username,
            Name = name,
            Email = email,
            Phone = "+1234567890",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = role,
            TournamentId = tournamentId,
            IsDeleted = isDeleted,
            ImageUrl = imageUrl ?? CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
            Bonos = bonos,
            Version = 1
        };
    }

    public static User StandardUser(Ulid? id = null) =>
        Create(id: id, role: User.UserRoles.USER);

    public static User AdminUser(Ulid? id = null) =>
        Create(id: id, username: "admin", email: "admin@example.com", role: User.UserRoles.ADMIN);

    public static User EncorderUser(Ulid? id = null) =>
        Create(id: id, username: "encorder", email: "encorder@example.com", role: User.UserRoles.ENCORDER);

    public static User OwnerUser(Ulid? id = null) =>
        Create(id: id, username: "owner", email: "owner@example.com", role: User.UserRoles.OWNER);

    public static User DeletedUser(Ulid? id = null) =>
        Create(id: id, isDeleted: true);
}