using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace BaryoDev.Umbraco.RelatedPosts;

/// <summary>Package-owned bounded memory cache with one shared token. Cancelling it drops every entry.</summary>
public sealed class RelatedPostsCache
{
    public const int DefaultMaxEntries = 10000;
    private readonly MemoryCache _cache;
    private readonly Action<int>? _onCapacity;
    private readonly object _gate = new();
    private CancellationTokenSource _cts = new();
    private int _warned;

    public RelatedPostsCache() : this(DefaultMaxEntries, null)
    {
    }

    /// <param name="maxEntries">Entry limit. At the limit the memory cache evicts some entries to make room.</param>
    /// <param name="onCapacity">Called once, the first time an entry is evicted for lack of room.</param>
    public RelatedPostsCache(int maxEntries, Action<int>? onCapacity)
    {
        MaxEntries = Math.Max(1, maxEntries);
        _onCapacity = onCapacity;
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = MaxEntries });
    }

    public int MaxEntries { get; }

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
                    options.RegisterPostEvictionCallback(OnEvicted);
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

    /// <summary>Evicts a share of the entries now, the way a full cache does in the background. For tests.</summary>
    internal void Compact(double percentage) => _cache.Compact(percentage);

    private void OnEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        if (reason == EvictionReason.Capacity && Interlocked.Exchange(ref _warned, 1) == 0)
        {
            _onCapacity?.Invoke(MaxEntries);
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
