using BackEncordados.Excel.Dto;
using ClosedXML.Excel;

namespace BackEncordados.Excel.Archive;

public class ExcelArchiveManager : IExcelArchiveManager
{
    public Task<byte[]> CreateExcelAsync(IEnumerable<TournamentExcelRowDto> data, string tournamentName)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Datos del Torneo");

        worksheet.Cell(1, 1).Value = "Username";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Raquetas Encordadas";
        worksheet.Cell(1, 4).Value = "Precio Total";

        var headerRange = worksheet.Range(1, 1, 1, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.Username;
            worksheet.Cell(row, 2).Value = item.Name;
            worksheet.Cell(row, 3).Value = item.RacketCount;
            worksheet.Cell(row, 4).Value = item.TotalPrice;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    public Task<byte[]> CreateAdvancedExcelAsync(ExcelAdvancedDataDto data, List<string> types, string tournamentName)
    {
        using var workbook = new XLWorkbook();

        if (types.Contains("users") && data.Users.Any())
        {
            CreateUsersSheet(workbook, data.Users);
        }

        if (types.Contains("materials") && data.Materials.Any())
        {
            CreateMaterialsSheet(workbook, data.Materials);
        }

        if (types.Contains("cuerdas") && data.Cuerdas.Any())
        {
            CreateCuerdasSheet(workbook, data.Cuerdas);
        }

        if (types.Contains("tournament") && data.Tournament.Any())
        {
            CreateTournamentSheet(workbook, data.Tournament);
        }

        if (types.Contains("pedidos") && data.Pedidos.Any())
        {
            CreatePedidosSheet(workbook, data.Pedidos, data.PedidoLineas);
        }

        if (!workbook.Worksheets.Any())
        {
            var ws = workbook.Worksheets.Add("Sin Datos");
            ws.Cell(1, 1).Value = "No se encontró información para los tipos seleccionados.";
            ws.Range(1, 1, 1, 1).Style.Font.Bold = true;
            ws.Range(1, 1, 1, 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    private static void CreateUsersSheet(IXLWorkbook workbook, List<ExcelUsersDto> users)
    {
        var ws = workbook.Worksheets.Add("Usuarios");
        var headers = new[] { "Id", "Username", "Name", "Email", "Phone", "TournamentId" };
        AddHeaders(ws, headers);

        var row = 2;
        foreach (var u in users)
        {
            ws.Cell(row, 1).Value = u.Id;
            ws.Cell(row, 2).Value = u.Username;
            ws.Cell(row, 3).Value = u.Name;
            ws.Cell(row, 4).Value = u.Email;
            ws.Cell(row, 5).Value = u.Phone ?? "";
            ws.Cell(row, 6).Value = u.TournamentId?.ToString() ?? "";
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void CreateMaterialsSheet(IXLWorkbook workbook, List<ExcelMaterialsDto> materials)
    {
        var ws = workbook.Worksheets.Add("Materiales");
        var headers = new[] { "Id", "TournamentId", "Marca", "Modelo", "Stock", "Precio", "Type" };
        AddHeaders(ws, headers);

        var row = 2;
        foreach (var m in materials)
        {
            ws.Cell(row, 1).Value = m.Id;
            ws.Cell(row, 2).Value = m.TournamentId.ToString();
            ws.Cell(row, 3).Value = m.Marca;
            ws.Cell(row, 4).Value = m.Modelo;
            ws.Cell(row, 5).Value = m.Stock;
            ws.Cell(row, 6).Value = m.Precio;
            ws.Cell(row, 7).Value = m.Type;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void CreateCuerdasSheet(IXLWorkbook workbook, List<ExcelCuerdasDto> cuerdas)
    {
        var ws = workbook.Worksheets.Add("Cuerdas");
        var headers = new[] { "Id", "TournamentId", "Marca", "Modelo", "Stock", "Precio", "StringFormat", "StringsType" };
        AddHeaders(ws, headers);

        var row = 2;
        foreach (var c in cuerdas)
        {
            ws.Cell(row, 1).Value = c.Id;
            ws.Cell(row, 2).Value = c.TournamentId.ToString();
            ws.Cell(row, 3).Value = c.Marca;
            ws.Cell(row, 4).Value = c.Modelo;
            ws.Cell(row, 5).Value = c.Stock;
            ws.Cell(row, 6).Value = c.Precio;
            ws.Cell(row, 7).Value = c.StringFormat;
            ws.Cell(row, 8).Value = c.StringsType;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void CreateTournamentSheet(IXLWorkbook workbook, List<ExcelTournamentDto> tournament)
    {
        var ws = workbook.Worksheets.Add("Torneo");
        var headers = new[] { "Id", "Owner", "Title", "StartTournament", "EndTournament", "Logotype", "WorkersList", "SupervisorList" };
        AddHeaders(ws, headers);

        var row = 2;
        foreach (var t in tournament)
        {
            ws.Cell(row, 1).Value = t.Id.ToString();
            ws.Cell(row, 2).Value = t.Owner;
            ws.Cell(row, 3).Value = t.Title;
            ws.Cell(row, 4).Value = t.StartTournament.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(row, 5).Value = t.EndTournament.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(row, 6).Value = t.Logotype;
            ws.Cell(row, 7).Value = t.WorkersList;
            ws.Cell(row, 8).Value = t.SupervisorList;
            row++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void CreatePedidosSheet(IXLWorkbook workbook, List<ExcelPedidosDto> pedidos, List<ExcelPedidoLineasDto> lineas)
    {
        var ws = workbook.Worksheets.Add("Pedidos");
        var headers = new[] { "Id", "TournamentId", "PlayerId", "AssignedTo", "Machine", "Comments", "Price", "PayStatus" };
        AddHeaders(ws, headers);

        var row = 2;
        foreach (var p in pedidos)
        {
            ws.Cell(row, 1).Value = p.Id;
            ws.Cell(row, 2).Value = p.TournamentId.ToString();
            ws.Cell(row, 3).Value = p.PlayerId;
            ws.Cell(row, 4).Value = p.AssignedTo;
            ws.Cell(row, 5).Value = p.Machine;
            ws.Cell(row, 6).Value = p.Comments ?? "";
            ws.Cell(row, 7).Value = p.Price;
            ws.Cell(row, 8).Value = p.PayStatus;
            row++;
        }
        ws.Columns().AdjustToContents();

        var wsLineas = workbook.Worksheets.Add("PedidoLineas");
        var headersLineas = new[] { "Id", "PedidoId", "RaquetModel", "Nudos", "DateString", "Logotype", "Color", 
            "StringV", "TensionV", "PreStetchV", "StringH", "TensionH", "PreStetchH", "Status" };
        AddHeaders(wsLineas, headersLineas);

        var rowLineas = 2;
        foreach (var l in lineas)
        {
            wsLineas.Cell(rowLineas, 1).Value = l.Id;
            wsLineas.Cell(rowLineas, 2).Value = l.PedidoId;
            wsLineas.Cell(rowLineas, 3).Value = l.RaquetModel;
            wsLineas.Cell(rowLineas, 4).Value = l.Nudos;
            wsLineas.Cell(rowLineas, 5).Value = l.DateString.ToString("yyyy-MM-dd HH:mm");
            wsLineas.Cell(rowLineas, 6).Value = l.Logotype;
            wsLineas.Cell(rowLineas, 7).Value = l.Color;
            wsLineas.Cell(rowLineas, 8).Value = l.StringV;
            wsLineas.Cell(rowLineas, 9).Value = l.TensionV;
            wsLineas.Cell(rowLineas, 10).Value = l.PreStetchV;
            wsLineas.Cell(rowLineas, 11).Value = l.StringH;
            wsLineas.Cell(rowLineas, 12).Value = l.TensionH;
            wsLineas.Cell(rowLineas, 13).Value = l.PreStetchH;
            wsLineas.Cell(rowLineas, 14).Value = l.Status;
            rowLineas++;
        }
        wsLineas.Columns().AdjustToContents();
    }

    private static void AddHeaders(IXLWorksheet ws, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }
        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
    }

    public Task<ExcelAdvancedDataDto> ReadExcelAsync(Stream stream)
    {
        var data = new ExcelAdvancedDataDto();

        using var workbook = new XLWorkbook(stream);

        if (workbook.TryGetWorksheet("Usuarios", out var usersSheet))
        {
            data.Users = ReadUsersSheet(usersSheet);
        }

        if (workbook.TryGetWorksheet("Materiales", out var materialsSheet))
        {
            data.Materials = ReadMaterialsSheet(materialsSheet);
        }

        if (workbook.TryGetWorksheet("Cuerdas", out var cuerdasSheet))
        {
            data.Cuerdas = ReadCuerdasSheet(cuerdasSheet);
        }

        if (workbook.TryGetWorksheet("Torneo", out var tournamentSheet))
        {
            data.Tournament = ReadTournamentSheet(tournamentSheet);
        }

        if (workbook.TryGetWorksheet("Pedidos", out var pedidosSheet) &&
            workbook.TryGetWorksheet("PedidoLineas", out var lineasSheet))
        {
            data.Pedidos = ReadPedidosSheet(pedidosSheet);
            data.PedidoLineas = ReadPedidoLineasSheet(lineasSheet);
        }

        return Task.FromResult(data);
    }

    private static List<ExcelUsersDto> ReadUsersSheet(IXLWorksheet ws)
    {
        var users = new List<ExcelUsersDto>();
        var rows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();

        foreach (var row in rows)
        {
            var idCell = row.Cell(1).GetString();
            var username = row.Cell(2).GetString();
            var name = row.Cell(3).GetString();
            var email = row.Cell(4).GetString();
            var phone = row.Cell(5).GetString();
            var tournamentIdStr = row.Cell(6).GetString();

            if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(email))
            {
                users.Add(new ExcelUsersDto
                {
                    Id = idCell,
                    Username = username,
                    Name = name,
                    Email = email,
                    Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                    TournamentId = string.IsNullOrWhiteSpace(tournamentIdStr) ? null : tournamentIdStr
                });
            }
        }
        return users;
    }

    private static List<ExcelMaterialsDto> ReadMaterialsSheet(IXLWorksheet ws)
    {
        var materials = new List<ExcelMaterialsDto>();
        var rows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();

        foreach (var row in rows)
        {
            var marca = row.Cell(3).GetString();
            var modelo = row.Cell(4).GetString();

            if (!string.IsNullOrWhiteSpace(marca) || !string.IsNullOrWhiteSpace(modelo))
            {
                materials.Add(new ExcelMaterialsDto
                {
                    Id = row.Cell(1).GetValue<long>(),
                    TournamentId = row.Cell(2).GetString(),
                    Marca = marca,
                    Modelo = modelo,
                    Stock = row.Cell(5).GetValue<int>(),
                    Precio = row.Cell(6).GetValue<double>(),
                    Type = row.Cell(7).GetString()
                });
            }
        }
        return materials;
    }

    private static List<ExcelCuerdasDto> ReadCuerdasSheet(IXLWorksheet ws)
    {
        var cuerdas = new List<ExcelCuerdasDto>();
        var rows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();

        foreach (var row in rows)
        {
            var marca = row.Cell(3).GetString();
            var modelo = row.Cell(4).GetString();

            if (!string.IsNullOrWhiteSpace(marca) || !string.IsNullOrWhiteSpace(modelo))
            {
                cuerdas.Add(new ExcelCuerdasDto
                {
                    Id = row.Cell(1).GetValue<long>(),
                    TournamentId = row.Cell(2).GetString(),
                    Marca = marca,
                    Modelo = modelo,
                    Stock = row.Cell(5).GetValue<int>(),
                    Precio = row.Cell(6).GetValue<double>(),
                    StringFormat = row.Cell(7).GetString(),
                    StringsType = row.Cell(8).GetString()
                });
            }
        }
        return cuerdas;
    }

    private static List<ExcelTournamentDto> ReadTournamentSheet(IXLWorksheet ws)
    {
        var tournaments = new List<ExcelTournamentDto>();
        var rows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();

        foreach (var row in rows)
        {
            var title = row.Cell(3).GetString();

            if (!string.IsNullOrWhiteSpace(title))
            {
                var startStr = row.Cell(4).GetString();
                var endStr = row.Cell(5).GetString();

                DateTime.TryParse(startStr, out var startDate);
                DateTime.TryParse(endStr, out var endDate);

                tournaments.Add(new ExcelTournamentDto
                {
                    Id = row.Cell(1).GetString(),
                    Owner = row.Cell(2).GetString(),
                    Title = title,
                    StartTournament = startDate == default ? DateTime.UtcNow : startDate,
                    EndTournament = endDate == default ? DateTime.UtcNow.AddDays(7) : endDate,
                    Logotype = row.Cell(6).GetString(),
                    WorkersList = row.Cell(7).GetString(),
                    SupervisorList = row.Cell(8).GetString()
                });
            }
        }
        return tournaments;
    }

    private static List<ExcelPedidosDto> ReadPedidosSheet(IXLWorksheet ws)
    {
        var pedidos = new List<ExcelPedidosDto>();
        var rows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();

        foreach (var row in rows)
        {
            var machine = row.Cell(5).GetString();

            if (!string.IsNullOrWhiteSpace(machine))
            {
                var price = row.Cell(7).GetValue<double>();

                pedidos.Add(new ExcelPedidosDto
                {
                    Id = row.Cell(1).GetString(),
                    TournamentId = row.Cell(2).GetString(),
                    PlayerId = row.Cell(3).GetString(),
                    AssignedTo = row.Cell(4).GetString(),
                    Machine = machine,
                    Comments = row.Cell(6).GetString(),
                    Price = price,
                    PayStatus = row.Cell(8).GetString()
                });
            }
        }
        return pedidos;
    }

    private static List<ExcelPedidoLineasDto> ReadPedidoLineasSheet(IXLWorksheet ws)
    {
        var lineas = new List<ExcelPedidoLineasDto>();
        var rows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();

        foreach (var row in rows)
        {
            var raquetModel = row.Cell(3).GetString();

            if (!string.IsNullOrWhiteSpace(raquetModel))
            {
                var dateStr = row.Cell(5).GetString();
                DateTime.TryParse(dateStr, out var dateString);

                lineas.Add(new ExcelPedidoLineasDto
                {
                    Id = row.Cell(1).GetString(),
                    PedidoId = row.Cell(2).GetString(),
                    RaquetModel = raquetModel,
                    Nudos = row.Cell(4).GetValue<byte>(),
                    DateString = dateString == default ? DateTime.UtcNow.AddDays(7) : dateString,
                    Logotype = row.Cell(6).GetString().ToLower() == "true",
                    Color = row.Cell(7).GetString(),
                    StringV = row.Cell(8).GetString(),
                    TensionV = row.Cell(9).GetValue<double>(),
                    PreStetchV = row.Cell(10).GetValue<short>(),
                    StringH = row.Cell(11).GetString(),
                    TensionH = row.Cell(12).GetValue<double>(),
                    PreStetchH = row.Cell(13).GetValue<short>(),
                    Status = row.Cell(14).GetString()
                });
            }
        }
        return lineas;
    }
}