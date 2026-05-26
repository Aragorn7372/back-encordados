using BackEncordados.Usuarios.Model;

namespace BackEncordados.Export.Repository;

public interface IExportUserRepository
{
    Task<List<User>> GetAllUsersAsync();
    Task ClearUsersAsync();
    Task ImportUsersAsync(List<User> users);
}
