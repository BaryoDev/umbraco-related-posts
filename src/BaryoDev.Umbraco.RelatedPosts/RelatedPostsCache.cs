using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace BaryoDev.Umbraco.RelatedPosts;

/// <summary>Package-owned bounded memory cache with one shared token. Cancelling it drops every entry.</summary>
public sealed class RelatedPostsCache
{
    private const int MaxEntries = 10000;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = MaxEntries });
    private readonly object _gate = new();
    private CancellationTokenSource _cts = new();

    /// <summary>Single flight: concurrent misses for one key share one compute.</summary>
    public T GetOrCreate<T>(string key, TimeSpan ttl, Func<T> create)
    {
        if (!_cache.TryGetValue(key, out Lazy<T>? lazy) || lazy is null)
        {
            CancellationTokenSource cts;
            lock (_gate)
            {
                cts = _cts;
                if (!_cache.TryGetValue(key, out lazy) || lazy is null)
                {
                    lazy = new Lazy<T>(create, LazyThreadSafetyMode.ExecutionAndPublication);
                    var options = new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = ttl };
                    options.AddExpirationToken(new CancellationChangeToken(cts.Token));
                    _cache.Set(key, lazy, options);
                }
            }
        }
        try
        {
            return lazy.Value;
        }
        catch
        {
            _cache.Remove(key);
            throw;
        }
    }

    public void Clear()
    {
        CancellationTokenSource old;
        lock (_gate)
        {
            old = _cts;
            _cts = new CancellationTokenSource();
        }
        // Not disposed: a reader may still hold its token.
        old.Cancel();
    }
}
