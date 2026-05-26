using BackEncordados.Common.Service.Email;
using FluentAssertions;

namespace TestEncordados.Unit.Common.Service.Email;

public class OrderItemEmailDtoTests
{
    [Test]
    public void Constructor_SetsProperties()
    {
        var dto = new OrderItemEmailDto("Pro Staff 97", 2, 299.99m);

        dto.ProductName.Should().Be("Pro Staff 97");
        dto.Quantity.Should().Be(2);
        dto.Price.Should().Be(299.99m);
    }

    [Test]
    public void Equality_SameValues_AreEqual()
    {
        var dto1 = new OrderItemEmailDto("Pure Aero", 1, 249.99m);
        var dto2 = new OrderItemEmailDto("Pure Aero", 1, 249.99m);

        dto1.Should().Be(dto2);
        (dto1 == dto2).Should().BeTrue();
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var dto1 = new OrderItemEmailDto("Pure Aero", 1, 249.99m);
        var dto2 = new OrderItemEmailDto("Pure Aero", 2, 249.99m);

        dto1.Should().NotBe(dto2);
        (dto1 == dto2).Should().BeFalse();
    }

    [Test]
    public void Deconstruct_ReturnsCorrectValues()
    {
        var dto = new OrderItemEmailDto("Speed Pro", 3, 179.50m);

        var (name, qty, price) = dto;

        name.Should().Be("Speed Pro");
        qty.Should().Be(3);
        price.Should().Be(179.50m);
    }

    [Test]
    public void ToString_ContainsPropertyValues()
    {
        var dto = new OrderItemEmailDto("Liquidmetal", 1, 199.99m);

        var str = dto.ToString();

        str.Should().Contain("Liquidmetal");
        str.Should().Contain("1");
        str.Should().Contain("199");
    }
}
