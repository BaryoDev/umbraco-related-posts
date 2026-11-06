using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace BaryoDev.Umbraco.RelatedPosts;

/// <summary>Memory cache with one shared token. Cancelling it drops every entry.</summary>
public sealed class RelatedPostsCache
{
    private readonly IMemoryCache _cache;
    private readonly object _gate = new();
    private CancellationTokenSource _cts = new();

    public RelatedPostsCache(IMemoryCache cache) => _cache = cache;

    public T GetOrCreate<T>(string key, TimeSpan ttl, Func<T> create)
    {
        return _cache.GetOrCreate(key, entry =>
        {
            CancellationTokenSource cts;
            lock (_gate) cts = _cts;
            entry.AbsoluteExpirationRelativeToNow = ttl;
            entry.AddExpirationToken(new CancellationChangeToken(cts.Token));
            return create();
        })!;
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
