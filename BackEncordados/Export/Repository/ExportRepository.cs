using BackEncordados.Common.Database.Config;
using BackEncordados.Export.Dto;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Export.Repository;

public class ExportRepository(
    IMaterialsExportRepository materialsRepo,
    IUserExportRepository userRepo,
    ITalleresExportRepository talleresRepo,
    IPedidosExportRepository pedidosRepo,
    UserDbContext userDbContext,
    ILogger<ExportRepository> logger
) : IExportRepository
{
    public async Task<ExportDataDto> GetAllDataAsync()
    {
        logger.LogInformation("Fetching all data from database");

        var data = new ExportDataDto();

        data.Tournaments = await talleresRepo.GetTournamentsDataAsync();
        data.Users = await userRepo.GetUsersDataAsync();

        var (materials, cuerdas) = await materialsRepo.GetMaterialsDataAsync();
        data.Materials = materials;
        data.Cuerdas = cuerdas;

        data.Pedidos = await pedidosRepo.GetPedidosDataAsync();

        return data;
    }

    public async Task ClearAllDataAsync()
    {
        logger.LogInformation("Clearing all data in reverse order");

        if (userDbContext.Database.IsInMemory())
        {
            await pedidosRepo.ClearPedidosAsync();
            await materialsRepo.ClearMaterialsAsync();
            await userRepo.ClearUsersAsync();
            await talleresRepo.ClearTournamentsAsync();
        }
        else
        {
            await pedidosRepo.ClearPedidosAsync();
            await materialsRepo.ClearMaterialsAsync();
            await userRepo.ClearUsersAsync();
            await talleresRepo.ClearTournamentsAsync();
        }
    }

    public async Task ImportDataAsync(ExportDataDto data)
    {
        logger.LogInformation("Importing data in correct order");

        await talleresRepo.ImportTournamentsAsync(data.Tournaments);
        await userRepo.ImportUsersAsync(data.Users);
        await materialsRepo.ImportMaterialsAsync(data.Materials);
        await materialsRepo.ImportCuerdasAsync(data.Cuerdas);
        await pedidosRepo.ImportPedidosAsync(data.Pedidos);

        await materialsRepo.ResyncSequencesAsync();

        logger.LogInformation("Data import completed");
    }
}
