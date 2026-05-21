using BackEncordados.Excel.Archive;
using BackEncordados.Excel.Dto;
using ClosedXML.Excel;
using FluentAssertions;

namespace TestEncordados.Integration.Excel.Archive;

public class ExcelArchiveManagerTests
{
    private readonly ExcelArchiveManager _manager = new();

    private static ExcelAdvancedDataDto CreateSampleData()
    {
        return new ExcelAdvancedDataDto
        {
            Users =
            [
                new ExcelUsersDto
                {
                    Username = "user1", Name = "User One", Email = "u1@test.com",
                    Phone = "123456789", TournamentId = "T1"
                }
            ],
            Materials =
            [
                new ExcelMaterialsDto { Marca = "BrandA", Modelo = "ModelA", Stock = 5, Precio = 10.99, Type = "TipoA" }
            ],
            Cuerdas =
            [
                new ExcelCuerdasDto { Marca = "StringX", Modelo = "ModelX", Stock = 10, Precio = 15.50, StringFormat = "Format1", StringsType = "Type1" }
            ],
            Tournament =
            [
                new ExcelTournamentDto
                {
                    Title = "Test Tournament", StartTournament = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                    EndTournament = new DateTime(2026, 1, 18, 10, 0, 0, DateTimeKind.Utc)
                }
            ],
            Pedidos =
            [
                new ExcelPedidosDto { Machine = "M1", PlayerId = "P1", Comments = "Test", Price = 50.0 }
            ],
            PedidoLineas =
            [
                new ExcelPedidoLineasDto { PedidoId = "", RaquetModel = "Pro Staff", Nudos = 2 }
            ]
        };
    }

    private static List<TournamentExcelRowDto> CreateSampleSimpleData()
    {
        return
        [
            new TournamentExcelRowDto { Username = "player1", Name = "Player One", RacketCount = 5, TotalPrice = 100.50M }
        ];
    }

    private static void AssertSheet(Action<IXLWorksheet> assert, byte[] excelBytes)
    {
        using var ms = new MemoryStream(excelBytes);
        using var workbook = new XLWorkbook(ms);
        assert(workbook.Worksheet(1));
    }

    private static void AssertSheets(Action<IEnumerable<IXLWorksheet>> assert, byte[] excelBytes)
    {
        using var ms = new MemoryStream(excelBytes);
        using var workbook = new XLWorkbook(ms);
        assert(workbook.Worksheets);
    }

    #region CreateExcelAsync

    [Test]
    public async Task CreateExcelAsync_WithData_ReturnsExcelWithHeadersAndRows()
    {
        var bytes = await _manager.CreateExcelAsync(CreateSampleSimpleData(), "TestTourney");

        AssertSheet(ws =>
        {
            ws.Cell(1, 1).GetString().Should().Be("Username");
            ws.Cell(1, 2).GetString().Should().Be("Name");
            ws.Cell(1, 3).GetString().Should().Be("Raquetas Encordadas");
            ws.Cell(1, 4).GetString().Should().Be("Precio Total");
            ws.Cell(2, 1).GetString().Should().Be("player1");
            ws.Cell(2, 2).GetString().Should().Be("Player One");
            ws.Cell(2, 3).GetString().Should().Be("5");
            ws.Cell(2, 4).GetValue<decimal>().Should().Be(100.50M);
        }, bytes);
    }

    [Test]
    public async Task CreateExcelAsync_WithEmptyData_ReturnsExcelWithOnlyHeaders()
    {
        var bytes = await _manager.CreateExcelAsync([], "TestTourney");

        AssertSheet(ws =>
        {
            ws.Cell(1, 1).GetString().Should().Be("Username");
            ws.RangeUsed().RowCount().Should().Be(1);
        }, bytes);
    }

    [Test]
    public async Task CreateExcelAsync_ReturnsNonEmptyByteArray()
    {
        var bytes = await _manager.CreateExcelAsync(CreateSampleSimpleData(), "TestTourney");

        bytes.Should().NotBeEmpty();
    }

    #endregion

    #region CreateAdvancedExcelAsync

    [Test]
    public async Task CreateAdvancedExcelAsync_WithAllTypes_CreatesAllSheets()
    {
        var data = CreateSampleData();
        var types = new List<string> { "users", "materials", "cuerdas", "tournament", "pedidos" };

        var bytes = await _manager.CreateAdvancedExcelAsync(data, types, "TestTourney");

        AssertSheets(sheets =>
        {
            var names = sheets.Select(s => s.Name).ToList();
            names.Should().Contain(["Usuarios", "Materiales", "Cuerdas", "Torneo", "Pedidos", "PedidoLineas"]);
            names.Should().HaveCount(6);
        }, bytes);
    }

    [Test]
    public async Task CreateAdvancedExcelAsync_WithSpecificTypes_CreatesOnlyRequestedSheets()
    {
        var data = CreateSampleData();
        var types = new List<string> { "cuerdas" };

        var bytes = await _manager.CreateAdvancedExcelAsync(data, types, "TestTourney");

        AssertSheets(sheets =>
        {
            var names = sheets.Select(s => s.Name).ToList();
            names.Should().ContainSingle().Which.Should().Be("Cuerdas");
        }, bytes);
    }

    [Test]
    public async Task CreateAdvancedExcelAsync_WithEmptyData_CreatesSinDatosSheet()
    {
        var data = new ExcelAdvancedDataDto();
        var types = new List<string> { "users" };

        var bytes = await _manager.CreateAdvancedExcelAsync(data, types, "TestTourney");

        AssertSheets(sheets =>
        {
            var names = sheets.Select(s => s.Name).ToList();
            names.Should().ContainSingle().Which.Should().Be("Sin Datos");
        }, bytes);
    }

    [Test]
    public async Task CreateAdvancedExcelAsync_SkipsSheetWhenTypeRequestedButListEmpty()
    {
        var data = new ExcelAdvancedDataDto
        {
            Users = [new ExcelUsersDto { Username = "u1" }]
        };
        var types = new List<string> { "users", "materials" };

        var bytes = await _manager.CreateAdvancedExcelAsync(data, types, "TestTourney");

        AssertSheets(sheets =>
        {
            var names = sheets.Select(s => s.Name).ToList();
            names.Should().Contain("Usuarios");
            names.Should().NotContain("Materiales");
        }, bytes);
    }

    [Test]
    public async Task CreateAdvancedExcelAsync_WithPedidos_CreatesPedidoAndPedidoLineasSheets()
    {
        var data = new ExcelAdvancedDataDto
        {
            Pedidos = [new ExcelPedidosDto { Machine = "M1", PlayerId = "P1" }],
            PedidoLineas = [new ExcelPedidoLineasDto { PedidoId = "", RaquetModel = "Pro" }]
        };
        var types = new List<string> { "pedidos" };

        var bytes = await _manager.CreateAdvancedExcelAsync(data, types, "TestTourney");

        AssertSheets(sheets =>
        {
            var names = sheets.Select(s => s.Name).ToList();
            names.Should().Contain(["Pedidos", "PedidoLineas"]);
        }, bytes);
    }

    #endregion

    #region ReadExcelAsync

    [Test]
    public async Task ReadExcelAsync_Roundtrip_PreservesAllData()
    {
        var original = CreateSampleData();
        var types = new List<string> { "users", "materials", "cuerdas", "tournament", "pedidos" };
        var excelBytes = await _manager.CreateAdvancedExcelAsync(original, types, "TestTourney");

        var result = await _manager.ReadExcelAsync(new MemoryStream(excelBytes));

        result.Users.Should().HaveCount(1);
        result.Users[0].Username.Should().Be("user1");
        result.Materials.Should().HaveCount(1);
        result.Materials[0].Marca.Should().Be("BrandA");
        result.Cuerdas.Should().HaveCount(1);
        result.Cuerdas[0].Marca.Should().Be("StringX");
        result.Tournament.Should().HaveCount(1);
        result.Tournament[0].Title.Should().Be("Test Tournament");
        result.Pedidos.Should().HaveCount(1);
        result.Pedidos[0].Machine.Should().Be("M1");
        result.PedidoLineas.Should().HaveCount(1);
        result.PedidoLineas[0].RaquetModel.Should().Be("Pro Staff");
    }

    [Test]
    public async Task ReadExcelAsync_WithPartialSheets_ReturnsPartialData()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Usuarios");
        ws.Cell(1, 1).Value = "Id";
        ws.Cell(1, 2).Value = "Username";
        ws.Cell(1, 3).Value = "Name";
        ws.Cell(1, 4).Value = "Email";
        ws.Cell(1, 5).Value = "Phone";
        ws.Cell(1, 6).Value = "TournamentId";
        ws.Cell(2, 1).Value = "";
        ws.Cell(2, 2).Value = "partial_user";
        ws.Cell(2, 3).Value = "Partial";
        ws.Cell(2, 4).Value = "p@test.com";
        ws.Cell(2, 5).Value = "";
        ws.Cell(2, 6).Value = "";
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        var result = await _manager.ReadExcelAsync(ms);

        result.Users.Should().HaveCount(1);
        result.Users[0].Username.Should().Be("partial_user");
        result.Users[0].Phone.Should().BeNull();
        result.Materials.Should().BeEmpty();
        result.Cuerdas.Should().BeEmpty();
        result.Tournament.Should().BeEmpty();
        result.Pedidos.Should().BeEmpty();
    }

    [Test]
    public async Task ReadExcelAsync_WithEmptyWorkbook_ReturnsEmptyData()
    {
        using var workbook = new XLWorkbook();
        workbook.Worksheets.Add("Empty");
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        var result = await _manager.ReadExcelAsync(ms);

        result.Users.Should().BeEmpty();
        result.Materials.Should().BeEmpty();
        result.Cuerdas.Should().BeEmpty();
        result.Tournament.Should().BeEmpty();
        result.Pedidos.Should().BeEmpty();
        result.PedidoLineas.Should().BeEmpty();
    }

    [Test]
    public async Task ReadExcelAsync_ParsesNumbersCorrectly()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Materiales");
        ws.Cell(1, 1).Value = "Id";
        ws.Cell(1, 2).Value = "TournamentId";
        ws.Cell(1, 3).Value = "Marca";
        ws.Cell(1, 4).Value = "Modelo";
        ws.Cell(1, 5).Value = "Stock";
        ws.Cell(1, 6).Value = "Precio";
        ws.Cell(1, 7).Value = "Type";
        ws.Cell(2, 1).Value = 42;
        ws.Cell(2, 2).Value = "T1";
        ws.Cell(2, 3).Value = "BrandX";
        ws.Cell(2, 4).Value = "ModelX";
        ws.Cell(2, 5).Value = 99;
        ws.Cell(2, 6).Value = 123.45;
        ws.Cell(2, 7).Value = "TipoX";
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        var result = await _manager.ReadExcelAsync(ms);

        result.Materials.Should().HaveCount(1);
        result.Materials[0].Id.Should().Be(42);
        result.Materials[0].Stock.Should().Be(99);
        result.Materials[0].Precio.Should().Be(123.45);
    }

    [Test]
    public async Task ReadExcelAsync_SkipsRowsWithMissingRequiredFields()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Usuarios");
        ws.Cell(1, 1).Value = "Id";
        ws.Cell(1, 2).Value = "Username";
        ws.Cell(1, 3).Value = "Name";
        ws.Cell(1, 4).Value = "Email";
        ws.Cell(1, 5).Value = "Phone";
        ws.Cell(1, 6).Value = "TournamentId";
        ws.Cell(2, 1).Value = "";
        ws.Cell(2, 2).Value = "";
        ws.Cell(2, 3).Value = "";
        ws.Cell(2, 4).Value = "";
        ws.Cell(2, 5).Value = "";
        ws.Cell(2, 6).Value = "";
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        var result = await _manager.ReadExcelAsync(ms);

        result.Users.Should().BeEmpty();
    }

    [Test]
    public async Task ReadExcelAsync_ReadsNullPhoneAsNull()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Usuarios");
        ws.Cell(1, 1).Value = "Id";
        ws.Cell(1, 2).Value = "Username";
        ws.Cell(1, 3).Value = "Name";
        ws.Cell(1, 4).Value = "Email";
        ws.Cell(1, 5).Value = "Phone";
        ws.Cell(1, 6).Value = "TournamentId";
        ws.Cell(2, 1).Value = "";
        ws.Cell(2, 2).Value = "user_no_phone";
        ws.Cell(2, 3).Value = "No Phone";
        ws.Cell(2, 4).Value = "np@test.com";
        ws.Cell(2, 5).Value = "";
        ws.Cell(2, 6).Value = "";
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        var result = await _manager.ReadExcelAsync(ms);

        result.Users.Should().HaveCount(1);
        result.Users[0].Phone.Should().BeNull();
        result.Users[0].Username.Should().Be("user_no_phone");
    }

    #endregion
}
