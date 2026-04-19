using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Model;

namespace BackEncordados.Usuarios.Mapper;

public static class UserMapper
{
    public static UserResponseDto ToDto(this User user)
    {
        return new UserResponseDto(
            user.Username,
            user.Email,
            user.Phone,
            user.ImageUrl,
            user.Name
            );
    }
    
}