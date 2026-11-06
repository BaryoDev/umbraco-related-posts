using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace BaryoDev.Umbraco.RelatedPosts;

/// <summary>Package-owned bounded memory cache with one shared token. Cancelling it drops every entry.</summary>
public sealed class RelatedPostsCache
{
    private const int MaxEntries = 2000;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = MaxEntries });
    private readonly object _gate = new();
    private CancellationTokenSource _cts = new();

    public T GetOrCreate<T>(string key, TimeSpan ttl, Func<T> create)
    {
        if (_cache.TryGetValue(key, out T? hit) && hit is not null) return hit;
        var value = create();
        CancellationTokenSource cts;
        lock (_gate) cts = _cts;
        var options = new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = ttl };
        options.AddExpirationToken(new CancellationChangeToken(cts.Token));
        _cache.Set(key, value, options);
        return value;
    }

    public void Clear()
    {
        CancellationTokenSource old;
        lock (_gate)
        {
            old = _cts;
            _cts = new CancellationTokenSource();
        }
        old.Cancel();
        old.Dispose();
    }
}
