using FakeItEasy;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Cache;
using Kentico.Kontent.Delivery.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.Cache
{
    public class DeliveryCacheManagerTests
    {
        private readonly IMemoryCache _memoryCache;
        private readonly DeliveryCacheManager _cacheManager;
        private readonly DeliveryCacheOptions _cacheOptions;
        private object obj;

        public DeliveryCacheManagerTests()
        {
            // Create memory cache with spy
            var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
            _memoryCache = A.Fake<IMemoryCache>();

            A.CallTo(() => _memoryCache.TryGetValue(A<object>.Ignored, out obj))
               .ReturnsLazily(c => {
                   var result = memoryCache.TryGetValue(c.Arguments[0], out var value);
                   return result;

               })
               .AssignsOutAndRefParametersLazily(c => {
                   var result = memoryCache.TryGetValue(c.Arguments[0], out var value);
                   return new object[] { value };
               });



            A.CallTo(() => _memoryCache.CreateEntry(A<object>.Ignored))
                .ReturnsLazily(c => memoryCache.CreateEntry(c.Arguments[0]));

            _cacheOptions = new DeliveryCacheOptions();
            _cacheManager = new DeliveryCacheManager(_memoryCache, Options.Create(_cacheOptions));
        }

        [Fact]
        public async Task GetOrAddAsync_ValueIsCached_ReturnsCachedValue()
        {
            const string key = "key";
            var originalValue = _memoryCache.Set(key, "value");

            var result = await _cacheManager.GetOrAddAsync<string>(key, () => Task.FromResult("value"), null);

            result.Should().Be(originalValue);
        }

        [Fact]
        public async Task GetOrAddAsync_ValueShouldNotBeCached_DoesNotCacheNewValue()
        {
            const string key = "key";
            const string value = "newValue";

            var result = await _cacheManager.GetOrAddAsync(key, () => Task.FromResult(value), _ => false, null);

            result.Should().Be(value);
            _memoryCache.TryGetValue(key, out _).Should().BeFalse();
        }

        [Fact]
        public async Task GetOrAddAsync_ValueIsNotCached_CachesNewValue()
        {
            const string key = "key";
            const string value = "newValue";

            var result = await _cacheManager.GetOrAddAsync(key, () => Task.FromResult(value), null);

            result.Should().Be(value);
            _memoryCache.TryGetValue(key, out string cachedValue).Should().BeTrue();
            cachedValue.Should().Be(value);
        }

        [Fact]
        public async Task GetOrAddAsync_ValueIsNotCached_DependencyIsNotCached_CachesNewDependencies()
        {
            const string key = "key";
            const string value = "newValue";
            var dependencies = new[]
            {
                "dependency_1",
                "dependency_2"
            };

            var result = await _cacheManager.GetOrAddAsync(key, () => Task.FromResult(value), null, _ => dependencies);

            result.Should().Be(value);
            _memoryCache.TryGetValue(key, out string cachedValue).Should().BeTrue();
            cachedValue.Should().Be(value);
            dependencies.Select(x => _memoryCache.TryGetValue(x, out CancellationTokenSource ts) && !ts.IsCancellationRequested)
                .Should().OnlyContain(x => x);
        }

        [Fact]
        public async Task GetOrAddAsync_ValueIsNotCached_DependencyIsCached_DoesNotCacheDependency()
        {
            const string key = "key";
            const string value = "newValue";
            var dependency = "dependency_1";
            var cachedDependencyValue = _memoryCache.Set(dependency, new CancellationTokenSource());

            var result = await _cacheManager.GetOrAddAsync(key, () => Task.FromResult(value), null, _ => new[] { dependency });

            Assert.Equal(value, result);
            Assert.True(_memoryCache.TryGetValue(key, out string cachedValue));
            Assert.Equal(value, cachedValue);
            Assert.True(_memoryCache.TryGetValue(dependency, out CancellationTokenSource tokenSource) && !tokenSource.IsCancellationRequested);
            Assert.Equal(cachedDependencyValue, tokenSource);
        }

        [Fact]
        public async Task GetOrAddAsync_ValueIsNotCached_DependencyIsExpired_CachesNewDependency()
        {
            const string key = "key";
            const string value = "newValue";
            var dependency = "dependency_1";
            var cachedDependencyValue = _memoryCache.Set(dependency, new CancellationTokenSource());
            cachedDependencyValue.Cancel();

            var result = await _cacheManager.GetOrAddAsync(key, () => Task.FromResult(value), null, _ => new[] { dependency });

            Assert.Equal(value, result);
            Assert.True(_memoryCache.TryGetValue(key, out string cachedValue));
            Assert.Equal(value, cachedValue);
            Assert.True(_memoryCache.TryGetValue(dependency, out CancellationTokenSource tokenSource) && !tokenSource.IsCancellationRequested);
            Assert.NotEqual(cachedDependencyValue, tokenSource);
        }

        [Fact]
        public async Task GetOrAddAsync_CachedValueExpiresWhenDependencyInvalidated()
        {
            const string key = "key";
            const string value = "newValue";
            const string dependency = "dependency_1";

            var result = await _cacheManager.GetOrAddAsync(key, () => Task.FromResult(value), null, _ => new[] { dependency });
            await _cacheManager.InvalidateDependencyAsync(dependency);

            Assert.Equal(value, result);
            Assert.False(_memoryCache.TryGetValue(key, out _));
            Assert.True(_memoryCache.TryGetValue(dependency, out CancellationTokenSource tokenSource) && tokenSource.IsCancellationRequested);
        }

        [Fact]
        public async Task GetOrAddAsync_IsNotStaleContent_CachedValueExpiresAfterDefaultTimeout()
        {
            const string key = "key";
            var value = await GetAbstractResponseInstance(false);
            _cacheOptions.DefaultExpiration = TimeSpan.FromMilliseconds(500);

            await _cacheManager.GetOrAddAsync(key, () => Task.FromResult(value), null);
            await Task.Delay(_cacheOptions.DefaultExpiration + TimeSpan.FromMilliseconds(100));

            Assert.False(_memoryCache.TryGetValue(key, out _));
        }

        [Fact]
        public async Task GetOrAddAsync_IsStaleContent_CachedValueExpiresAfterStaleContentTimeout()
        {
            const string key = "key";
            var value = await GetAbstractResponseInstance(true);
            _cacheOptions.StaleContentExpiration = TimeSpan.FromMilliseconds(500);

            await _cacheManager.GetOrAddAsync(key, () => Task.FromResult(value), null);
            await Task.Delay(_cacheOptions.StaleContentExpiration + TimeSpan.FromMilliseconds(100));

            Assert.False(_memoryCache.TryGetValue(key, out _));
        }

        [Fact]
        public async Task GetOrAddAsync_ParallelAccess_ValueFetchedAndCachedOnlyOnce()
        {
            const string key = "key;";
            var counter = 0;
            Task<int> WaitAndIncreaseCounter() => Task.Delay(200).ContinueWith(_ => Interlocked.Increment(ref counter));

            var tasks = Enumerable.Range(0, 100).Select(i => _cacheManager.GetOrAddAsync(key, WaitAndIncreaseCounter, null));
            var results = await Task.WhenAll(tasks);

            Assert.All(results, v => Assert.Equal(1, v));
        }

        [Fact]
        public async Task TryGet_ValueIsNotCached_ReturnsFalse()
        {
            const string key = "key";

            var result = await _cacheManager.TryGetAsync<string>(key, out _);

            Assert.False(result);
        }

        [Fact]
        public async Task TryGet_ValueIsCached_ReturnsTrueAndCachedValue()
        {
            const string key = "key";
            var originalValue = _memoryCache.Set(key, "cachedString");

            var result = await _cacheManager.TryGetAsync<string>(key, out var cachedValue);

            Assert.True(result);
            Assert.Equal(originalValue, cachedValue);
        }

        [Fact]
        public async Task TryGet_KeyIsNull_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cacheManager.TryGetAsync<string>(null, out _));
        }

        [Fact]
        public async Task TryGet_ValueIsExpired_ReturnsFalse()
        {
            const string key = "key";
            var tokenSource = new CancellationTokenSource();
            var cancelToken = new CancellationChangeToken(tokenSource.Token);
            _memoryCache.Set(key, "cachedString", cancelToken);
            tokenSource.Cancel();

            var result = await _cacheManager.TryGetAsync<string>(key, out _);

            Assert.False(result);
        }

        [Fact]
        public async Task InvalidateDependency_KeyIsNull_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cacheManager.InvalidateDependencyAsync(null));
        }

        [Fact]
        public async Task InvalidateDependency_DependencyTokenExists_CancellationRequested()
        {
            const string key = "key";
            var value = _memoryCache.Set(key, new CancellationTokenSource());

            await _cacheManager.InvalidateDependencyAsync(key);

            Assert.True(value.IsCancellationRequested);
        }

        private static async Task<AbstractResponse> GetAbstractResponseInstance(bool shouldBeStaleContent)
        {
            var itemsResponse = new
            {
                items = Enumerable.Empty<object>(),
                modular_content = new Dictionary<string, object>()
            };

            var staleContentHeaderValue = shouldBeStaleContent ? "1" : "0";
            var mockHandler = new MockHttpMessageHandler();
            var responseHeaders = new[] { new KeyValuePair<string, string>("X-Stale-Content", staleContentHeaderValue) };
            mockHandler.When("*").Respond(responseHeaders, "application/json", JsonConvert.SerializeObject(itemsResponse));
            var httpClient = mockHandler.ToHttpClient();
            var client = DeliveryClientBuilder.WithProjectId(Guid.NewGuid()).WithDeliveryHttpClient(new DeliveryHttpClient(httpClient)).Build();
            return await client.GetItemsAsync();
        }
    }
}
