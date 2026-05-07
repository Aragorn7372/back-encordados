using Microsoft.Extensions.Caching.Memory;

namespace BackEncordados.Common.Service.Cache.Memory;

public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        // Intentamos obtener el valor de forma sincrónica
        if (cache.TryGetValue(key, out T? value))
        {
            // Devolvemos una tarea ya completada con el resultado
            return await Task.FromResult(value);
        }
        
        return await Task.FromResult(default(T));
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        cache.Set(key, value, expiration ?? TimeSpan.FromMinutes(5));
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(string key) { cache.Remove(key); await Task.CompletedTask; }
    public Task RemoveByPatternAsync(string pattern) => Task.CompletedTask; 
}