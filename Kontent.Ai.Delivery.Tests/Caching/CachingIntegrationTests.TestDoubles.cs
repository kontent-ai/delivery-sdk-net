using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace Kontent.Ai.Delivery.Tests.Caching;

public partial class CachingIntegrationTests
{
    private class TestCacheManager : IDeliveryCacheManager
    {
        public List<CachedItem> CachedItems { get; } = [];

        public async Task<T?> GetOrSetAsync<T>(
            string cacheKey,
            Func<CancellationToken, Task<CacheEntry<T>?>> factory,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var existing = CachedItems.FirstOrDefault(i => i.Key == cacheKey);
            if (existing?.Value is T cached)
                return cached;

            var entry = await factory(cancellationToken);
            if (entry is null)
                return null;

            CachedItems.Add(new CachedItem
            {
                Key = cacheKey,
                Value = entry.Value,
                Dependencies = [.. entry.Dependencies]
            });
            return entry.Value;
        }

        public Task<bool> InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
            => Task.FromResult(true);

        public class CachedItem
        {
            public string Key { get; set; } = "";
            public object? Value { get; set; }
            public List<string> Dependencies { get; set; } = [];
        }
    }

    private class MockDistributedCache : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, byte[]> _cache = new();

        public byte[]? Get(string key) => _cache.TryGetValue(key, out var value) ? value : null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _cache[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => _cache.TryRemove(key, out _);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class DelayedJsonResponseHandler(string jsonResponse, TimeSpan delay) : HttpMessageHandler
    {
        private readonly string _jsonResponse = jsonResponse;
        private readonly TimeSpan _delay = delay;
        private readonly TaskCompletionSource<bool> _firstRequestStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _requestCount;

        public int RequestCount => Volatile.Read(ref _requestCount);
        public Task WaitForFirstRequestAsync() => _firstRequestStarted.Task;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestNumber = Interlocked.Increment(ref _requestCount);
            if (requestNumber == 1)
            {
                _firstRequestStarted.TrySetResult(true);
            }

            if (_delay > TimeSpan.Zero)
            {
                using var timer = new PeriodicTimer(_delay);
                await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_jsonResponse, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class PrimedSuccessThenErrorHandler(
        string successJson,
        string errorJson,
        TimeSpan failureDelay = default) : HttpMessageHandler
    {
        private readonly string _successJson = successJson;
        private readonly string _errorJson = errorJson;
        private readonly TimeSpan _failureDelay = failureDelay;
        private int _requestCount;

        public int RequestCount => Volatile.Read(ref _requestCount);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestNumber = Interlocked.Increment(ref _requestCount);
            if (requestNumber == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_successJson, Encoding.UTF8, "application/json")
                };
            }

            if (_failureDelay > TimeSpan.Zero)
            {
                using var timer = new PeriodicTimer(_failureDelay);
                await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(_errorJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
