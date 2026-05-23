using BackEncordados.Common.Database.Config;
using BackEncordados.Export.Dto;
using BackEncordados.Materials.Model;
using Microsoft.EntityFrameworkCore;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Model;

namespace BackEncordados.Export.Repository;

public class ExportRepository(
    UserDbContext userDbContext,
    MaterialsDbContext materialsDbContext,
    PedidosDbContext pedidosDbContext,
    TalleresDbContext talleresDbContext,
    ILogger<ExportRepository> logger
) : IExportRepository
{
    public async Task<ExportDataDto> GetAllDataAsync()
    {
        logger.LogInformation("Fetching all data from database");

        var data = new ExportDataDto();

        data.Tournaments = await talleresDbContext.Partidos.IgnoreQueryFilters().ToListAsync();
        logger.LogInformation("Fetched {Count} tournaments", data.Tournaments.Count);

        data.Users = await userDbContext.Users.IgnoreQueryFilters().ToListAsync();
        logger.LogInformation("Fetched {Count} users", data.Users.Count);

        data.Materials = await materialsDbContext.Materiales.IgnoreQueryFilters().ToListAsync();
        logger.LogInformation("Fetched {Count} materials", data.Materials.Count);

        data.Cuerdas = await materialsDbContext.Cuerdas.IgnoreQueryFilters().ToListAsync();
        logger.LogInformation("Fetched {Count} cuerdas", data.Cuerdas.Count);

        var pedidos = await pedidosDbContext.Pedidos.ToListAsync();
        var lineas = await pedidosDbContext.PedidoLineas.ToListAsync();

        foreach (var pedido in pedidos)
        {
            pedido.Lineas = lineas.Where(l => l.PedidoId == pedido.Id).ToList();
        }
        data.Pedidos = pedidos;
        logger.LogInformation("Fetched {Count} pedidos", data.Pedidos.Count);

        return data;
    }

    public async Task ClearAllDataAsync()
    {
        logger.LogInformation("Clearing all data in reverse order");

        if (userDbContext.Database.IsInMemory())
        {
            await ClearAllDataInMemoryAsync();
        }
        else
        {
            await ClearAllDataProductionAsync();
        }
    }

    private async Task ClearAllDataInMemoryAsync()
    {
        logger.LogInformation("Using in-memory delete strategy");

        var pedidoLineas = await pedidosDbContext.PedidoLineas.ToListAsync();
        pedidosDbContext.PedidoLineas.RemoveRange(pedidoLineas);
        
        var pedidos = await pedidosDbContext.Pedidos.ToListAsync();
        pedidosDbContext.Pedidos.RemoveRange(pedidos);
        await pedidosDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared pedidos");

        var cuerdas = await materialsDbContext.Cuerdas.ToListAsync();
        materialsDbContext.Cuerdas.RemoveRange(cuerdas);

        var materials = await materialsDbContext.Materiales.ToListAsync();
        materialsDbContext.Materiales.RemoveRange(materials);
        await materialsDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared materials");

        var users = await userDbContext.Users.IgnoreQueryFilters().ToListAsync();
        userDbContext.Users.RemoveRange(users);
        await userDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared users");

        var tournaments = await talleresDbContext.Partidos.IgnoreQueryFilters().ToListAsync();
        talleresDbContext.Partidos.RemoveRange(tournaments);
        await talleresDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared tournaments");
    }

    private async Task ClearAllDataProductionAsync()
    {
        logger.LogInformation("Using production delete strategy (ExecuteDeleteAsync for PostgreSQL, RemoveRange for MongoDB)");

        var pedidoLineas = await pedidosDbContext.PedidoLineas.ToListAsync();
        pedidosDbContext.PedidoLineas.RemoveRange(pedidoLineas);

        var pedidos = await pedidosDbContext.Pedidos.ToListAsync();
        pedidosDbContext.Pedidos.RemoveRange(pedidos);

        await pedidosDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared pedidos");

        await materialsDbContext.Cuerdas.ExecuteDeleteAsync();
        await materialsDbContext.Materiales.ExecuteDeleteAsync();
        logger.LogInformation("Cleared materials");

        await userDbContext.Users.ExecuteDeleteAsync();
        logger.LogInformation("Cleared users");

        var tournaments = await talleresDbContext.Partidos.IgnoreQueryFilters().ToListAsync();
        talleresDbContext.Partidos.RemoveRange(tournaments);
        await talleresDbContext.SaveChangesAsync();
        logger.LogInformation("Cleared tournaments");
    }

    public async Task ImportDataAsync(ExportDataDto data)
    {
        logger.LogInformation("Importing data in correct order");

        if (data.Tournaments.Any())
        {
            var tournamentAssignments = new List<(Ulid TournamentId, List<WorkerMachineAssignment> Assignments)>();

            foreach (var tournament in data.Tournaments)
            {
                tournamentAssignments.Add((tournament.Id, tournament.WorkerMachineAssignments.ToList()));
                tournament.WorkerMachineAssignments = new List<WorkerMachineAssignment>();
                await talleresDbContext.Partidos.AddAsync(tournament);
            }
            await talleresDbContext.SaveChangesAsync();
            logger.LogInformation("Imported {Count} tournaments", data.Tournaments.Count);

            long nextAssignmentId = 1;
            foreach (var (tournamentId, assignments) in tournamentAssignments)
            {
                var existingTournament = await talleresDbContext.Partidos.FirstOrDefaultAsync(t => t.Id == tournamentId);
                if (existingTournament != null)
                {
                    foreach (var assignment in assignments)
                    {
                        existingTournament.WorkerMachineAssignments.Add(new WorkerMachineAssignment
                        {
                            Id = nextAssignmentId++,
                            WorkerId = assignment.WorkerId,
                            MachineName = assignment.MachineName
                        });
                    }
                }
            }
            await talleresDbContext.SaveChangesAsync();
            logger.LogInformation("Imported worker machine assignments for {Count} tournaments", tournamentAssignments.Count);
        }

        if (data.Users.Any())
        {
            await userDbContext.Users.AddRangeAsync(data.Users);
            await userDbContext.SaveChangesAsync();
            logger.LogInformation("Imported {Count} users", data.Users.Count);
        }

        if (data.Materials.Any())
        {
            await materialsDbContext.Materiales.AddRangeAsync(data.Materials);
            await materialsDbContext.SaveChangesAsync();
            logger.LogInformation("Imported {Count} materials", data.Materials.Count);
        }

        if (data.Cuerdas.Any())
        {
            await materialsDbContext.Cuerdas.AddRangeAsync(data.Cuerdas);
            await materialsDbContext.SaveChangesAsync();
            logger.LogInformation("Imported {Count} cuerdas", data.Cuerdas.Count);
        }

        if (data.Pedidos.Any())
        {
            await pedidosDbContext.Pedidos.AddRangeAsync(data.Pedidos);
            await pedidosDbContext.SaveChangesAsync();
            logger.LogInformation("Imported {Count} pedidos", data.Pedidos.Count);
        }

        logger.LogInformation("Data import completed");
    }
}