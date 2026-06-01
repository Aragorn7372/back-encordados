using BackEncordados.Common.Utils;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestEncordados.Unit.Common.Utils;

public class TransactionalAttributeTests
{
    [Test]
    public void Constructor_WhenNoContextTypes_ThrowsArgumentException()
    {
        var act = () => new TransactionalAttribute();

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Debe proporcionar al menos un tipo de DbContext*");
    }

    [Test]
    public void Constructor_WhenNullContextTypes_ThrowsArgumentException()
    {
        var act = () => new TransactionalAttribute(null!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Debe proporcionar al menos un tipo de DbContext*");
    }

    [Test]
    public void Constructor_WhenInvalidTypeNotDbContext_ThrowsArgumentException()
    {
        var act = () => new TransactionalAttribute(typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*no heredan de DbContext*");
    }

    [Test]
    public void Constructor_WhenMultipleTypesSomeInvalid_ThrowsArgumentException()
    {
        var act = () => new TransactionalAttribute(typeof(DbContext), typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*no heredan de DbContext*");
    }

    [Test]
    public void Constructor_WhenValidContextTypes_DoesNotThrow()
    {
        var act = () => new TransactionalAttribute(typeof(TestDbContext));

        act.Should().NotThrow();
    }

    [Test]
    public async Task OnActionExecutionAsync_WhenDbContextInMemory_SkipsTransaction()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"TransactionalTest_{Guid.NewGuid()}")
            .Options;

        using var dbContext = new TestDbContext(options);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var attr = new TransactionalAttribute(typeof(TestDbContext));
        var ctx = CreateActionExecutingContext(serviceProvider);
        var next = CreateActionDelegateWithResult(new OkResult());

        await attr.OnActionExecutionAsync(ctx, next);

        dbContext.Database.EnsureCreated();
        dbContext.Database.CanConnect().Should().BeTrue();
    }

    [Test]
    public async Task OnActionExecutionAsync_WhenDbContextNullInDi_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var attr = new TransactionalAttribute(typeof(TestDbContext));
        var ctx = CreateActionExecutingContext(serviceProvider);
        var nextResult = new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            null!)
        {
            Result = new OkResult()
        };

        var act = async () => await attr.OnActionExecutionAsync(ctx, () => Task.FromResult(nextResult));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No se pudo obtener la instancia de*");
    }

    [Test]
    public async Task OnActionExecutionAsync_WhenExceptionInAction_DoesRollback()
    {
        var options = CreateTransactionalDbOptions();
        using var dbContext = new TestDbContext(options);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var attr = new TransactionalAttribute(typeof(TestDbContext));
        var ctx = CreateActionExecutingContext(serviceProvider);

        var nextResult = new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            null!)
        {
            Exception = new InvalidOperationException("Test exception"),
            ExceptionHandled = false
        };

        await attr.OnActionExecutionAsync(ctx, () => Task.FromResult(nextResult));
    }

    [Test]
    public async Task OnActionExecutionAsync_WhenObjectResultWithErrorStatusCode_DoesRollback()
    {
        var options = CreateTransactionalDbOptions();
        using var dbContext = new TestDbContext(options);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var attr = new TransactionalAttribute(typeof(TestDbContext));
        var ctx = CreateActionExecutingContext(serviceProvider);

        var nextResult = new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            null!)
        {
            Result = new ObjectResult("error") { StatusCode = 400 }
        };

        await attr.OnActionExecutionAsync(ctx, () => Task.FromResult(nextResult));
    }

    [Test]
    public async Task OnActionExecutionAsync_WhenBadRequestResult_DoesRollback()
    {
        var options = CreateTransactionalDbOptions();
        using var dbContext = new TestDbContext(options);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var attr = new TransactionalAttribute(typeof(TestDbContext));
        var ctx = CreateActionExecutingContext(serviceProvider);

        var nextResult = new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            null!)
        {
            Result = new BadRequestResult()
        };

        await attr.OnActionExecutionAsync(ctx, () => Task.FromResult(nextResult));
    }

    [Test]
    public async Task OnActionExecutionAsync_WhenNotFoundResult_DoesRollback()
    {
        var options = CreateTransactionalDbOptions();
        using var dbContext = new TestDbContext(options);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var attr = new TransactionalAttribute(typeof(TestDbContext));
        var ctx = CreateActionExecutingContext(serviceProvider);

        var nextResult = new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            null!)
        {
            Result = new NotFoundResult()
        };

        await attr.OnActionExecutionAsync(ctx, () => Task.FromResult(nextResult));
    }

    [Test]
    public async Task OnActionExecutionAsync_WhenStatusCodeResultWithError_DoesRollback()
    {
        var options = CreateTransactionalDbOptions();
        using var dbContext = new TestDbContext(options);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var attr = new TransactionalAttribute(typeof(TestDbContext));
        var ctx = CreateActionExecutingContext(serviceProvider);

        var nextResult = new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            null!)
        {
            Result = new StatusCodeResult(500)
        };

        await attr.OnActionExecutionAsync(ctx, () => Task.FromResult(nextResult));
    }

    [Test]
    public async Task OnActionExecutionAsync_WhenOkResult_DoesCommit()
    {
        var options = CreateTransactionalDbOptions();
        using var dbContext = new TestDbContext(options);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var attr = new TransactionalAttribute(typeof(TestDbContext));
        var ctx = CreateActionExecutingContext(serviceProvider);

        var nextResult = new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            null!)
        {
            Result = new OkResult()
        };

        await attr.OnActionExecutionAsync(ctx, () => Task.FromResult(nextResult));
    }

    [Test]
    public async Task OnActionExecutionAsync_WhenResultsWithException_ReturnsOriginalException()
    {
        var options = CreateTransactionalDbOptions();
        using var dbContext = new TestDbContext(options);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var attr = new TransactionalAttribute(typeof(TestDbContext));
        var ctx = CreateActionExecutingContext(serviceProvider);

        var expectedException = new InvalidOperationException("something went wrong");
        var nextResult = new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            null!)
        {
            Exception = expectedException
        };

        await attr.OnActionExecutionAsync(ctx, () => Task.FromResult(nextResult));

        nextResult.Exception.Should().Be(expectedException);
    }

    [Test]
    public async Task OnActionExecutionAsync_MultipleContexts_CommitsAllInOrder()
    {
        var options1 = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"MultiCtxTest1_{Guid.NewGuid()}")
            .Options;
        var options2 = new DbContextOptionsBuilder<SecondTestDbContext>()
            .UseInMemoryDatabase($"MultiCtxTest2_{Guid.NewGuid()}")
            .Options;
        using var db1 = new TestDbContext(options1);
        using var db2 = new SecondTestDbContext(options2);

        var services = new ServiceCollection();
        services.AddSingleton(typeof(TestDbContext), db1);
        services.AddSingleton(typeof(SecondTestDbContext), db2);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var attr = new TransactionalAttribute(typeof(TestDbContext), typeof(SecondTestDbContext));
        var ctx = CreateActionExecutingContext(serviceProvider);

        var nextResult = new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            null!)
        {
            Result = new OkResult()
        };

        await attr.OnActionExecutionAsync(ctx, () => Task.FromResult(nextResult));
    }

    private static ActionExecutingContext CreateActionExecutingContext(IServiceProvider serviceProvider)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            null!);
    }

    private static ActionExecutionDelegate CreateActionDelegateWithResult(IActionResult result)
    {
        return () =>
        {
            var ctx = new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                null!)
            {
                Result = result
            };
            return Task.FromResult(ctx);
        };
    }

    private static DbContextOptions<TestDbContext> CreateTransactionalDbOptions()
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"TransactionalTest_{Guid.NewGuid()}")
            .Options;
    }
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    public DbSet<TestEntity> Entities { get; set; } = null!;
}

public class SecondTestDbContext : DbContext
{
    public SecondTestDbContext(DbContextOptions<SecondTestDbContext> options) : base(options) { }
    public DbSet<TestEntity> Entities { get; set; } = null!;
}

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
