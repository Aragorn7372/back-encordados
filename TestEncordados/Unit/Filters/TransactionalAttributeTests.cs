using BackEncordados.Common.Utils;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace TestEncordados.Unit.Filters;

public class TransactionalAttributeTests
{
    [Test]
    public void Constructor_NoContextTypes_ThrowsArgumentException()
    {
        var act = () => new TransactionalAttribute();

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_NullContextTypes_ThrowsArgumentException()
    {
        var act = () => new TransactionalAttribute(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_InvalidContextType_ThrowsArgumentException()
    {
        var act = () => new TransactionalAttribute(typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*no heredan de DbContext*");
    }

    [Test]
    public void Constructor_ValidDbContextType_DoesNotThrow()
    {
        var act = () => new TransactionalAttribute(typeof(TestDbContext));

        act.Should().NotThrow();
    }

    [Test]
    public void Constructor_MultipleValidDbContextTypes_DoesNotThrow()
    {
        var act = () => new TransactionalAttribute(typeof(TestDbContext), typeof(SecondTestDbContext));

        act.Should().NotThrow();
    }

    [Test]
    public void Constructor_MixedValidAndInvalidTypes_ThrowsArgumentException()
    {
        var act = () => new TransactionalAttribute(typeof(TestDbContext), typeof(string));

        act.Should().Throw<ArgumentException>();
    }
}

public class TestDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("TestDb_Transactional1");
    }
}

public class SecondTestDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("TestDb_Transactional2");
    }
}