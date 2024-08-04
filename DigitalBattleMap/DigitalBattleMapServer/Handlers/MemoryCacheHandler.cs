using Microsoft.Extensions.Caching.Memory;

namespace DigitalBattleMapServer.Handlers;

public class MemoryCacheHandler : IMemoryCacheHandler
{
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheHandler(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public byte[] Get(string key)
    {
        ValidateKey(key);
        _memoryCache.TryGetValue(key, out byte[] value);
        return value ?? Array.Empty<byte>();
    }

    public void Set(string key, byte[] value)
    {
        ValidateKey(key);
        _memoryCache.Set(key, value);
    }

    public void Delete(string key)
    {
        ValidateKey(key);
        _memoryCache.Remove(key);
    }

    private void ValidateKey(string key)
    {
        // Make sure the key is of a valid size otherwise attackers could inject giant keys and crash the server!
        if (string.IsNullOrEmpty(key) || key.Length > 32)
            throw new ArgumentException("Key cannot be greater than 32");
    }
}