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
