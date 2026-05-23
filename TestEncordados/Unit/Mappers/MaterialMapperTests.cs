using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Mapper;
using BackEncordados.Materials.Model;
using FluentAssertions;
using Moq;
using TestEncordados.Unit.Fixtures;

namespace TestEncordados.Unit.Mappers;

public class MaterialMapperTests
{
    private static readonly Mock<ICloudinaryService> MockCloudinary = new();

    static MaterialMapperTests()
    {
        MockCloudinary.Setup(c => c.ResolveImageUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("https://res.cloudinary.com/test/image/upload/v1/test.jpg");
    }

    [Test]
    public void ToDto_ValidMaterial_ReturnsCorrectDto()
    {
        var material = MaterialBuilder.Create(
            id: 1L,
            marca: "Head",
            modelo: "Pro",
            stock: 10,
            precio: 25.99,
            type: MaterialType.Grip);

        var result = material.ToDto(MockCloudinary.Object);

        result.Id.Should().Be(1L);
        result.TournamentId.Should().Be(material.TournamentId);
        result.Marca.Should().Be("Head");
        result.Modelo.Should().Be("Pro");
        result.Stock.Should().Be(10);
        result.Precio.Should().Be(25.99);
        result.MaterialType.Should().Be("Grip");
        result.ImageUrl.Should().Be("https://res.cloudinary.com/test/image/upload/v1/test.jpg");
    }

    [Test]
    public void ToDto_AllEnumTypes_ReturnsCorrectStringValue()
    {
        var types = new[] { MaterialType.Grip, MaterialType.Overgrip, MaterialType.LeadTape, MaterialType.Silicone, MaterialType.Otro };

        foreach (var type in types)
        {
            var material = MaterialBuilder.Create(type: type);
            var result = material.ToDto(MockCloudinary.Object);
            result.MaterialType.Should().Be(type.ToString());
        }
    }

    [Test]
    public void ToModel_ValidDto_ReturnsCorrectMaterial()
    {
        var dto = new MaterialRequestDto
        {
            Marca = "Wilson",
            TournamentId = Ulid.NewUlid(),
            Modelo = "Blade",
            Stock = 20,
            Precio = 35.50,
            Type = "Overgrip"
        };

        var result = dto.ToModel();

        result.Marca.Should().Be("Wilson");
        result.Modelo.Should().Be("Blade");
        result.Stock.Should().Be(20);
        result.Precio.Should().Be(35.50);
        result.Type.Should().Be(MaterialType.Overgrip);
        result.TournamentId.Should().Be(dto.TournamentId);
    }

    [Test]
    public void ToModel_TypeIsCaseInsensitive_ParsesCorrectly()
    {
        var typeStrings = new[] { "grip", "GRIP", "Grip" };

        foreach (var typeString in typeStrings)
        {
            var dto = new MaterialRequestDto
            {
                Marca = "Head",
                TournamentId = Ulid.NewUlid(),
                Modelo = "Extreme",
                Stock = 10,
                Precio = 20.0,
                Type = typeString
            };

            var result = dto.ToModel();
            result.Type.Should().Be(MaterialType.Grip);
        }
    }

    [Test]
    public void ToModel_WithMaxValues_ReturnsCorrectMaterial()
    {
        var tournamentId = Ulid.NewUlid();
        var dto = new MaterialRequestDto
        {
            Marca = new string('a', 100),
            TournamentId = tournamentId,
            Modelo = new string('b', 100),
            Stock = int.MaxValue,
            Precio = double.MaxValue,
            Type = "Otro"
        };

        var result = dto.ToModel();

        result.Marca.Should().Be(dto.Marca);
        result.Modelo.Should().Be(dto.Modelo);
        result.Stock.Should().Be(int.MaxValue);
        result.Precio.Should().Be(double.MaxValue);
        result.Type.Should().Be(MaterialType.Otro);
    }
}