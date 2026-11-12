using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

namespace BaryoDev.Umbraco.RelatedPosts.Tests;

public class CacheTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task Concurrent_misses_compute_once()
    {
        const int callers = 20;
        var cache = new RelatedPostsCache();
        using var barrier = new Barrier(callers);
        using var gate = new ManualResetEventSlim();
        var computed = 0;
        var arrived = 0;

        // Dedicated threads released together; create stays blocked until all of them are in the call.
        var workers = Enumerable.Range(0, callers)
            .Select(_ => Task.Factory.StartNew(
                () =>
                {
                    Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(30)));
                    Interlocked.Increment(ref arrived);
                    return cache.GetOrCreate("k", Ttl, () =>
                    {
                        Interlocked.Increment(ref computed);
                        Assert.True(gate.Wait(TimeSpan.FromSeconds(30)));
                        return 42;
                    });
                },
                TaskCreationOptions.LongRunning))
            .ToArray();

        Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref arrived) == callers, TimeSpan.FromSeconds(30)));
        Thread.Sleep(100);
        gate.Set();
        var results = await Task.WhenAll(workers);

        Assert.Equal(1, computed);
        Assert.All(results, r => Assert.Equal(42, r));
    }

    [Fact]
    public async Task Clear_during_compute_is_not_cached()
    {
        var cache = new RelatedPostsCache();
        using var entered = new ManualResetEventSlim();
        using var gate = new ManualResetEventSlim();

        var slow = Task.Factory.StartNew(
            () => cache.GetOrCreate("k", Ttl, () =>
            {
                entered.Set();
                Assert.True(gate.Wait(TimeSpan.FromSeconds(30)));
                return "stale";
            }),
            TestContext.Current.CancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        Assert.True(entered.Wait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken));
        cache.Clear();
        gate.Set();

        Assert.Equal("stale", await slow);
        Assert.Equal("fresh", cache.GetOrCreate("k", Ttl, () => "fresh"));
    }

    [Fact]
    public void A_capacity_eviction_is_reported_once_and_the_cache_keeps_serving()
    {
        var reports = new List<int>();
        var cache = new RelatedPostsCache(10, n => { lock (reports) { reports.Add(n); } });
        var calls = 0;
        int Create() => ++calls;
        cache.GetOrCreate("a", Ttl, Create);
        cache.GetOrCreate("b", Ttl, Create);

        cache.Compact(1.0);
        cache.Compact(1.0);

        // Eviction callbacks run on the thread pool, so wait for the first one only.
        Assert.True(SpinWait.SpinUntil(() => { lock (reports) { return reports.Count > 0; } }, TimeSpan.FromSeconds(5)));
        Thread.Sleep(50);
        lock (reports)
        {
            Assert.Equal([10], reports);
        }

        // Evicted keys are computed again, then served from the cache.
        Assert.Equal(3, cache.GetOrCreate("a", Ttl, Create));
        Assert.Equal(3, cache.GetOrCreate("a", Ttl, Create));
        Assert.Equal(3, calls);
    }

    [Fact]
    public void The_default_limit_is_ten_thousand_and_a_limit_below_one_is_raised()
    {
        Assert.Equal(10000, new RelatedPostsCache().MaxEntries);
        Assert.Equal(1, new RelatedPostsCache(0, null).MaxEntries);
    }

    [Fact]
    public void Content_type_and_language_changes_clear_the_cache()
    {
        foreach (var raise in new Action<InvalidateOnPublishHandler>[]
        {
            h => h.Handle(new ContentTypeCacheRefresherNotification(Array.Empty<object>(), MessageType.RefreshAll)),
            h => h.Handle(new LanguageCacheRefresherNotification(Array.Empty<object>(), MessageType.RefreshAll)),
        })
        {
            var cache = new RelatedPostsCache();
            var h = new InvalidateOnPublishHandler(cache);
            var calls = 0;
            int Create() => ++calls;
            Assert.Equal(1, cache.GetOrCreate("k", Ttl, Create));

            raise(h);

            Assert.Equal(2, cache.GetOrCreate("k", Ttl, Create));
        }
    }

    [Fact]
    public void Value_is_reused_until_cleared()
    {
        var cache = new RelatedPostsCache();
        var calls = 0;
        int Create() => ++calls;

        Assert.Equal(1, cache.GetOrCreate("k", Ttl, Create));
        Assert.Equal(1, cache.GetOrCreate("k", Ttl, Create));
        cache.Clear();
        Assert.Equal(2, cache.GetOrCreate("k", Ttl, Create));
    }
}
