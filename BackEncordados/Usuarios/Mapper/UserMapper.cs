using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Model;

namespace BackEncordados.Usuarios.Mapper;

public static class UserMapper
{
    public static UserResponseDto ToDto(this User user,ICloudinaryService cloudinary)
    {
        return new UserResponseDto(
            user.Username,
            cloudinary.ResolveImageUrl(user.ImageUrl, CloudinaryConstants.FOLDER_USUARIOS),
            user.Name
            );
    }
    public static UserWithIdDto ToDtoWithId(this User user,ICloudinaryService cloudinary)
    {
        return new UserWithIdDto(
            user.Id.ToString(),
            user.Username,
            cloudinary.ResolveImageUrl(user.ImageUrl, CloudinaryConstants.FOLDER_USUARIOS),
            user.Name
            );
    }
    public static User ToModel(this ContactoPostRequestDto request) {
        return new User {
            Name = request.Name,
            Email = request.Email ?? Guid.NewGuid().ToString(),
            Phone = request.Phone,
            Username = Ulid.NewUlid().ToString(),
            TournamentId = request.TournamentId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), workFactor: 11),
            Role = User.UserRoles.USER
        };
    }
    
}
