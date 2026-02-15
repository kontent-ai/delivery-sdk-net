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

        public Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
            where T : class
        {
            var item = CachedItems.FirstOrDefault(i => i.Key == cacheKey);
            return Task.FromResult(item?.Value as T);
        }

        public Task SetAsync<T>(
            string cacheKey,
            T value,
            IEnumerable<string> dependencies,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            CachedItems.Add(new CachedItem
            {
                Key = cacheKey,
                Value = value,
                Dependencies = [.. dependencies]
            });
            return Task.CompletedTask;
        }

        public Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
            => Task.CompletedTask;

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
}
