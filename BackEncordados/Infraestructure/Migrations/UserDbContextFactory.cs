using BackEncordados.Common.Database.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BackEncordados.Infraestructure.Migrations;

public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL_USER")
            ?? "Host=localhost;Database=encordados_users;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);
        return new UserDbContext(optionsBuilder.Options);
    }
}
