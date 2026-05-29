using BackEncordados.Usuarios.Model;

namespace BackEncordados.Export.Repository;

public interface IUserExportRepository
{
    Task<List<User>> GetUsersDataAsync();
    Task ClearUsersAsync();
    Task ImportUsersAsync(List<User> users);
}
