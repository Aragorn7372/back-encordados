namespace BackEncordados.Common.Service.Cache.Hybrid;

public class HybridCacheService(
    [FromKeyedServices("L1")] ICacheService l1, 
    [FromKeyedServices("L2")] ICacheService l2) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        // Intentar RAM local
        var value = await l1.GetAsync<T>(key);
        if (value != null) return value;

        // Intentar Redis
        value = await l2.GetAsync<T>(key);
        if (value != null)
        {
            //  tiempo corto para no saturar
            await l1.SetAsync(key, value, TimeSpan.FromMinutes(1));
        }
        return value;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        await l1.SetAsync(key, value, expiration);
        await l2.SetAsync(key, value, expiration);
    }

    public async Task RemoveAsync(string key)
    {
        await l1.RemoveAsync(key);
        await l2.RemoveAsync(key);
    }
    
    public async Task RemoveByPatternAsync(string pattern) => await l2.RemoveByPatternAsync(pattern);
}