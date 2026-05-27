using BackEncordados.Common.Database.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BackEncordados.Infraestructure.Migrations;

public class MaterialsDbContextFactory : IDesignTimeDbContextFactory<MaterialsDbContext>
{
    public MaterialsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MaterialsDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL_MATERIALS")
            ?? "Host=localhost;Database=encordados_materials;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);
        return new MaterialsDbContext(optionsBuilder.Options);
    }
}
