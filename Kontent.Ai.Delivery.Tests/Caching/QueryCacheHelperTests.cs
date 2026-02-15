using System.Collections.Concurrent;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Caching;

public class QueryCacheHelperTests
{
    [Fact]
    public async Task GetOrFetchAsync_ConcurrentMissesForSameKey_InvokesFactoryOnce()
    {
        var cacheManager = new TestCacheManager();
        var fetchCount = 0;

        async Task<(TestValue? Value, IEnumerable<string> Dependencies)> Fetch(CancellationToken ct)
        {
            Interlocked.Increment(ref fetchCount);
            await Task.Delay(100, ct).ConfigureAwait(false);
            return (new TestValue("value"), []);
        }

        var results = await Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ => QueryCacheHelper.GetOrFetchAsync(
                    cacheManager,
                    "same-key",
                    Fetch,
                    expiration: null,
                    logger: null,
                    cancellationToken: CancellationToken.None)));

        Assert.Equal(1, fetchCount);
        Assert.Single(results, r => !r.IsCacheHit);
        Assert.All(results, r => Assert.Equal("value", r.Value?.Value));
    }

    [Fact]
    public async Task GetOrFetchAsync_SameKeyAcrossDifferentManagers_DoesNotCoalesceAcrossManagers()
    {
        var managerA = new TestCacheManager();
        var managerB = new TestCacheManager();

        var startedFetches = 0;
        var managerAFetchCount = 0;
        var managerBFetchCount = 0;
        var bothOwnersStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task WaitForBothOwnersAsync(CancellationToken ct)
        {
            if (Interlocked.Increment(ref startedFetches) == 2)
            {
                bothOwnersStarted.TrySetResult();
            }

            await bothOwnersStarted.Task.WaitAsync(ct).ConfigureAwait(false);
            await Task.Delay(50, ct).ConfigureAwait(false);
        }

        async Task<(TestValue? Value, IEnumerable<string> Dependencies)> FetchA(CancellationToken ct)
        {
            Interlocked.Increment(ref managerAFetchCount);
            await WaitForBothOwnersAsync(ct).ConfigureAwait(false);
            return (new TestValue("A"), []);
        }

        async Task<(TestValue? Value, IEnumerable<string> Dependencies)> FetchB(CancellationToken ct)
        {
            Interlocked.Increment(ref managerBFetchCount);
            await WaitForBothOwnersAsync(ct).ConfigureAwait(false);
            return (new TestValue("B"), []);
        }

        var tasks = new[]
        {
            QueryCacheHelper.GetOrFetchAsync(managerA, "shared-key", FetchA, null, null, CancellationToken.None),
            QueryCacheHelper.GetOrFetchAsync(managerA, "shared-key", FetchA, null, null, CancellationToken.None),
            QueryCacheHelper.GetOrFetchAsync(managerB, "shared-key", FetchB, null, null, CancellationToken.None),
            QueryCacheHelper.GetOrFetchAsync(managerB, "shared-key", FetchB, null, null, CancellationToken.None)
        };

        var results = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, managerAFetchCount);
        Assert.Equal(1, managerBFetchCount);
        Assert.Equal(2, startedFetches);

        Assert.Equal("A", results[0].Value?.Value);
        Assert.Equal("A", results[1].Value?.Value);
        Assert.Equal("B", results[2].Value?.Value);
        Assert.Equal("B", results[3].Value?.Value);
    }

    [Fact]
    public async Task GetOrFetchAsync_WaiterCancellation_CancelsWaiterOnlyAndAllowsCachePopulation()
    {
        var cacheManager = new TestCacheManager();
        var fetchStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFetch = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fetchCount = 0;

        async Task<(TestValue? Value, IEnumerable<string> Dependencies)> Fetch(CancellationToken ct)
        {
            Interlocked.Increment(ref fetchCount);
            fetchStarted.TrySetResult();
            await releaseFetch.Task.ConfigureAwait(false);
            return (new TestValue("cached"), []);
        }

        var ownerTask = QueryCacheHelper.GetOrFetchAsync(
            cacheManager,
            "waiter-cancel",
            Fetch,
            expiration: null,
            logger: null,
            cancellationToken: CancellationToken.None);

        await fetchStarted.Task;

        using var waiterCts = new CancellationTokenSource();
        waiterCts.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            QueryCacheHelper.GetOrFetchAsync(
                cacheManager,
                "waiter-cancel",
                Fetch,
                expiration: null,
                logger: null,
                cancellationToken: waiterCts.Token));

        releaseFetch.TrySetResult();

        var ownerResult = await ownerTask;
        Assert.False(ownerResult.IsCacheHit);

        var cachedResult = await QueryCacheHelper.GetOrFetchAsync(
            cacheManager,
            "waiter-cancel",
            Fetch,
            expiration: null,
            logger: null,
            cancellationToken: CancellationToken.None);

        Assert.True(cachedResult.IsCacheHit);
        Assert.Equal("cached", cachedResult.Value?.Value);
        Assert.Equal(1, fetchCount);
    }

    [Fact]
    public async Task GetOrFetchAsync_OwnerThrows_AllWaitersObserveSameFailureInBurst()
    {
        var cacheManager = new TestCacheManager();
        var fetchCount = 0;

        async Task<(TestValue? Value, IEnumerable<string> Dependencies)> Fetch(CancellationToken ct)
        {
            Interlocked.Increment(ref fetchCount);
            await Task.Delay(100, ct).ConfigureAwait(false);
            throw new InvalidOperationException("boom");
        }

        var tasks = Enumerable.Range(0, 6)
            .Select(_ => QueryCacheHelper.GetOrFetchAsync(
                cacheManager,
                "owner-throws",
                Fetch,
                expiration: null,
                logger: null,
                cancellationToken: CancellationToken.None))
            .ToArray();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.WhenAll(tasks));

        Assert.Equal("boom", ex.Message);
        Assert.Equal(1, fetchCount);
        Assert.All(tasks, t => Assert.True(t.IsFaulted));
    }

    [Fact]
    public async Task GetOrFetchWithRehydrationAsync_NoCachePayload_DoesNotReturnFalseCacheHitsForWaiters()
    {
        var cacheManager = new TestCacheManager();
        var fetchCount = 0;

        async Task<(TestPayload? Payload, TestValue? ProcessedResult, IEnumerable<string> Dependencies)> Fetch(CancellationToken ct)
        {
            var call = Interlocked.Increment(ref fetchCount);
            await Task.Delay(50, ct).ConfigureAwait(false);
            return (null, new TestValue($"result-{call}"), []);
        }

        var tasks = new[]
        {
            QueryCacheHelper.GetOrFetchWithRehydrationAsync<TestPayload, TestValue>(
                cacheManager,
                "null-payload",
                Fetch,
                (payload, _) => Task.FromResult(new TestValue(payload.Value)),
                expiration: null,
                logger: null,
                cancellationToken: CancellationToken.None),
            QueryCacheHelper.GetOrFetchWithRehydrationAsync<TestPayload, TestValue>(
                cacheManager,
                "null-payload",
                Fetch,
                (payload, _) => Task.FromResult(new TestValue(payload.Value)),
                expiration: null,
                logger: null,
                cancellationToken: CancellationToken.None)
        };

        var results = await Task.WhenAll(tasks);

        Assert.Equal(2, fetchCount);
        Assert.All(results, result => Assert.False(result.IsCacheHit));
        Assert.All(results, result => Assert.NotNull(result.Value));
    }

    private sealed class TestCacheManager : IDeliveryCacheManager
    {
        private readonly ConcurrentDictionary<string, object> _store = new(StringComparer.Ordinal);

        public Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_store.TryGetValue(cacheKey, out var value) ? value as T : null);
        }

        public Task SetAsync<T>(
            string cacheKey,
            T value,
            IEnumerable<string> dependencies,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            _store[cacheKey] = value;
            return Task.CompletedTask;
        }

        public Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed record TestValue(string Value);

    private sealed record TestPayload(string Value);
}
