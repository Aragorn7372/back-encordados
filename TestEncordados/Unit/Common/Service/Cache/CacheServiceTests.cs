using System.Text;
using System.Text.Json;
using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.Hybrid;
using BackEncordados.Common.Service.Cache.Memory;
using BackEncordados.Common.Service.Cache.keys;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestEncordados.Unit.Common.Service.Cache;

public class CacheKeysTests
{
    [Test]
    public void TournamentCacheKey_HasCorrectPrefix()
    {
        CacheKeys.TournamentCacheKey.Should().Be("tournaments_");
    }

    [Test]
    public void UserKey_HasCorrectValue()
    {
        CacheKeys.UserKey.Should().Be("user_name_");
    }

    [Test]
    public void UserDataKey_HasCorrectValue()
    {
        CacheKeys.UserDataKey.Should().Be("user_data_");
    }

    [Test]
    public void PurchasedCacheKey_HasCorrectPrefix()
    {
        CacheKeys.PurchasedCacheKey.Should().Be("purchased_");
    }

    [Test]
    public void PasswordChange_HasCorrectValue()
    {
        CacheKeys.PasswordChange.Should().Be("password_");
    }
}

public class MemoryCacheServiceTests
{
    private MemoryCacheService _service = null!;
    private IMemoryCache _realCache = null!;

    [SetUp]
    public void SetUp()
    {
        _realCache = new MemoryCache(new MemoryCacheOptions());
        _service = new MemoryCacheService(_realCache);
    }

    [TearDown]
    public void TearDown()
    {
        if (_realCache is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Test]
    public async Task GetAsync_WhenKeyExists_ReturnsValue()
    {
        var key = "test_key";
        var expectedValue = "test_value";
        
        await _service.SetAsync(key, expectedValue);

        var result = await _service.GetAsync<string>(key);

        result.Should().Be(expectedValue);
    }

    [Test]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsDefault()
    {
        var key = "nonexistent_key";

        var result = await _service.GetAsync<string>(key);

        result.Should().BeNull();
    }

    [Test]
    public async Task GetAsync_ForIntType_ReturnsCorrectValue()
    {
        var key = "int_key";
        var expectedValue = 42;
        
        await _service.SetAsync(key, expectedValue);

        var result = await _service.GetAsync<int>(key);

        result.Should().Be(expectedValue);
    }

    [Test]
    public async Task GetAsync_ForObjectType_ReturnsObject()
    {
        var key = "object_key";
        var expectedValue = new TestObject { Name = "test", Value = 123 };
        
        await _service.SetAsync(key, expectedValue);

        var result = await _service.GetAsync<TestObject>(key);

        result.Should().NotBeNull();
        result!.Name.Should().Be("test");
        result.Value.Should().Be(123);
    }

    [Test]
    public async Task SetAsync_StoresValue()
    {
        var key = "test_key";
        var value = "test_value";
        
        await _service.SetAsync(key, value);

        var result = await _service.GetAsync<string>(key);
        result.Should().Be(value);
    }

    [Test]
    public async Task SetAsync_WithCustomExpiration_StoresValue()
    {
        var key = "test_key";
        var value = "test_value";
        var expiration = TimeSpan.FromMinutes(10);
        
        await _service.SetAsync(key, value, expiration);

        var result = await _service.GetAsync<string>(key);
        result.Should().Be(value);
    }

    [Test]
    public async Task RemoveAsync_RemovesValue()
    {
        var key = "test_key";
        var value = "test_value";
        
        await _service.SetAsync(key, value);
        await _service.RemoveAsync(key);

        var result = await _service.GetAsync<string>(key);
        result.Should().BeNull();
    }

    [Test]
    public async Task RemoveByPatternAsync_ReturnsCompletedTask()
    {
        await _service.RemoveByPatternAsync("pattern_*");
    }

    private class TestObject
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }
}

public class CacheServiceTests
{
    private readonly Dictionary<string, byte[]> _cacheStore = new();
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<ILogger<CacheService>> _mockLogger;
    private readonly CacheService _service;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CacheServiceTests()
    {
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<CacheService>>();
        _service = new CacheService(_mockDistributedCache.Object, _mockLogger.Object);

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) =>
                _cacheStore.TryGetValue(key, out var val) ? val : null);

        _mockDistributedCache
            .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((key, val, _, _) => _cacheStore[key] = val)
            .Returns(Task.CompletedTask);

        _mockDistributedCache
            .Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, _) => _cacheStore.Remove(key))
            .Returns(Task.CompletedTask);
    }

    private void SeedCache<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        _cacheStore[key] = Encoding.UTF8.GetBytes(json);
    }

    [TearDown]
    public void TearDown()
    {
        _cacheStore.Clear();
    }

    [Test]
    public async Task GetAsync_WhenKeyExists_ReturnsDeserializedValue()
    {
        var key = "test_key";
        var expected = "test_value";
        SeedCache(key, expected);

        var result = await _service.GetAsync<string>(key);

        result.Should().Be(expected);
    }

    [Test]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsDefault()
    {
        var result = await _service.GetAsync<string>("nonexistent_key");

        result.Should().BeNull();
    }

    [Test]
    public async Task GetAsync_ForIntType_ReturnsCorrectValue()
    {
        var key = "int_key";
        var expected = 42;
        SeedCache(key, expected);

        var result = await _service.GetAsync<int>(key);

        result.Should().Be(expected);
    }

    [Test]
    public async Task GetAsync_ForComplexObject_ReturnsDeserializedObject()
    {
        var key = "object_key";
        var expected = new TestCacheObject { Name = "test", Value = 123 };
        SeedCache(key, expected);

        var result = await _service.GetAsync<TestCacheObject>(key);

        result.Should().NotBeNull();
        result!.Name.Should().Be("test");
        result.Value.Should().Be(123);
    }

    [Test]
    public async Task GetAsync_WhenExceptionThrown_ReturnsDefaultAndLogsError()
    {
        var key = "error_key";
        var exception = new InvalidOperationException("Redis connection error");
        _mockDistributedCache
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var result = await _service.GetAsync<string>(key);

        result.Should().BeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Error al obtener valor de caché para clave: {key}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task SetAsync_StoresValueSuccessfully()
    {
        var key = "set_key";
        var value = "stored_value";

        await _service.SetAsync(key, value);

        var cached = _cacheStore[key];
        var json = Encoding.UTF8.GetString(cached);
        var deserialized = JsonSerializer.Deserialize<string>(json, JsonOptions);
        deserialized.Should().Be(value);
    }

    [Test]
    public async Task SetAsync_WithCustomExpiration_UsesSpecifiedExpiration()
    {
        var key = "exp_key";
        var value = "exp_value";
        var expiration = TimeSpan.FromMinutes(10);

        await _service.SetAsync(key, value, expiration);

        _mockDistributedCache.Verify(
            x => x.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == expiration),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task SetAsync_WithDefaultExpiration_UsesFiveMinutes()
    {
        var key = "default_exp_key";
        var value = "default_exp_value";

        await _service.SetAsync(key, value);

        _mockDistributedCache.Verify(
            x => x.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(5)),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task SetAsync_WhenExceptionThrown_LogsError()
    {
        var key = "set_error_key";
        var value = "set_error_value";
        var exception = new InvalidOperationException("Redis set error");
        _mockDistributedCache
            .Setup(x => x.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        await _service.SetAsync(key, value);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Error al guardar en caché para clave: {key}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task RemoveAsync_RemovesValue()
    {
        var key = "remove_key";
        SeedCache(key, "to_remove");

        await _service.RemoveAsync(key);

        _cacheStore.ContainsKey(key).Should().BeFalse();
        var result = await _service.GetAsync<string>(key);
        result.Should().BeNull();
    }

    [Test]
    public async Task RemoveAsync_WhenExceptionThrown_LogsError()
    {
        var key = "remove_error_key";
        var exception = new InvalidOperationException("Redis remove error");
        _mockDistributedCache
            .Setup(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        await _service.RemoveAsync(key);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Error al eliminar de caché para clave: {key}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task RemoveByPatternAsync_CompletesSuccessfully()
    {
        await _service.RemoveByPatternAsync("pattern_*");
    }

    [Test]
    public async Task RemoveByPatternAsync_LogsDebugMessage()
    {
        var pattern = "test_*";

        await _service.RemoveByPatternAsync(pattern);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Eliminando entradas de caché que coinciden con patrón: {pattern}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task RemoveByPatternAsync_WhenExceptionThrown_LogsError()
    {
        var pattern = "error_pattern_*";
        var exception = new InvalidOperationException("Logger error in pattern removal");
        _mockLogger
            .Setup(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains($"Eliminando entradas de caché que coinciden con patrón: {pattern}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ))
            .Throws(exception);

        await _service.RemoveByPatternAsync(pattern);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains($"Error al eliminar entradas de caché por patrón: {pattern}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    private class TestCacheObject
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }
}

public class HybridCacheServiceTests
{
    private Mock<ICacheService> _mockL1 = null!;
    private Mock<ICacheService> _mockL2 = null!;
    private HybridCacheService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockL1 = new Mock<ICacheService>();
        _mockL2 = new Mock<ICacheService>();
        _service = new HybridCacheService(_mockL1.Object, _mockL2.Object);
    }

    [Test]
    public async Task GetAsync_WhenL1HasValue_ReturnsFromL1()
    {
        var key = "test_key";
        var expectedValue = "L1_value";
        
        _mockL1.Setup(x => x.GetAsync<string>(key))
            .ReturnsAsync(expectedValue);

        var result = await _service.GetAsync<string>(key);

        result.Should().Be(expectedValue);
        _mockL2.Verify(x => x.GetAsync<string>(key), Times.Never);
    }

    [Test]
    public async Task GetAsync_WhenL1MissesAndL2HasValue_ReturnsFromL2AndCachesInL1()
    {
        var key = "test_key";
        var l2Value = "L2_value";
        
        _mockL1.Setup(x => x.GetAsync<string>(key))
            .ReturnsAsync((string?)null);
        _mockL2.Setup(x => x.GetAsync<string>(key))
            .ReturnsAsync(l2Value);

        var result = await _service.GetAsync<string>(key);

        result.Should().Be(l2Value);
        _mockL1.Verify(x => x.SetAsync(
            key, 
            l2Value, 
            It.Is<TimeSpan>(t => t == TimeSpan.FromMinutes(1))), 
            Times.Once);
    }

    [Test]
    public async Task GetAsync_WhenBothL1AndL2Miss_ReturnsNull()
    {
        var key = "nonexistent_key";
        
        _mockL1.Setup(x => x.GetAsync<string>(key))
            .ReturnsAsync((string?)null);
        _mockL2.Setup(x => x.GetAsync<string>(key))
            .ReturnsAsync((string?)null);

        var result = await _service.GetAsync<string>(key);

        result.Should().BeNull();
    }

    [Test]
    public async Task SetAsync_SetsValueInBothL1AndL2()
    {
        var key = "test_key";
        var value = "test_value";
        
        _mockL1.Setup(x => x.SetAsync(key, value, null));
        _mockL2.Setup(x => x.SetAsync(key, value, null));

        await _service.SetAsync(key, value);

        _mockL1.Verify(x => x.SetAsync(key, value, null), Times.Once);
        _mockL2.Verify(x => x.SetAsync(key, value, null), Times.Once);
    }

    [Test]
    public async Task SetAsync_UsesCustomExpiration_InBothLayers()
    {
        var key = "test_key";
        var value = "test_value";
        var expiration = TimeSpan.FromMinutes(10);
        
        _mockL1.Setup(x => x.SetAsync(key, value, expiration));
        _mockL2.Setup(x => x.SetAsync(key, value, expiration));

        await _service.SetAsync(key, value, expiration);

        _mockL1.Verify(x => x.SetAsync(key, value, expiration), Times.Once);
        _mockL2.Verify(x => x.SetAsync(key, value, expiration), Times.Once);
    }

    [Test]
    public async Task RemoveAsync_RemovesValueFromBothL1AndL2()
    {
        var key = "test_key";
        
        _mockL1.Setup(x => x.RemoveAsync(key));
        _mockL2.Setup(x => x.RemoveAsync(key));

        await _service.RemoveAsync(key);

        _mockL1.Verify(x => x.RemoveAsync(key), Times.Once);
        _mockL2.Verify(x => x.RemoveAsync(key), Times.Once);
    }

    [Test]
    public async Task RemoveByPatternAsync_RemovesFromL2Only()
    {
        var pattern = "test_*";
        
        _mockL2.Setup(x => x.RemoveByPatternAsync(pattern))
            .Returns(Task.CompletedTask);

        await _service.RemoveByPatternAsync(pattern);

        _mockL1.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
        _mockL2.Verify(x => x.RemoveByPatternAsync(pattern), Times.Once);
    }
}