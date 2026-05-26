using BackEncordados.Common.Service.Cache;
using Moq;

namespace TestEncordados.Unit.Fixtures;

public static class CacheServiceBuilder
{
    private static readonly Dictionary<string, object> Store = new();

    public static Mock<ICacheService> Create()
    {
        var mock = new Mock<ICacheService>();
        Store.Clear();

        mock.Setup(c => c.GetAsync<object>(It.IsAny<string>()))
            .Returns((string key) =>
            {
                if (Store.TryGetValue(key, out var value))
                    return Task.FromResult<object?>(value);
                return Task.FromResult<object?>(null);
            });

        mock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>()))
            .Callback<string, object, TimeSpan?>((key, value, _) => Store[key] = value!)
            .Returns(Task.CompletedTask);

        mock.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Callback<string>((key) => Store.Remove(key))
            .Returns(Task.CompletedTask);

        return mock;
    }

    public static void Clear() => Store.Clear();
}