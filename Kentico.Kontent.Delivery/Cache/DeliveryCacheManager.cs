using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.Cache
{
    public sealed class DeliveryCacheManager : IDeliveryCacheManager
    {
        private readonly IMemoryCache _memoryCache;
        private readonly DeliveryCacheOptions _cacheOptions;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _createLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly ConcurrentDictionary<string, object> _dependencyLocks = new ConcurrentDictionary<string, object>();


        public DeliveryCacheManager(IMemoryCache memoryCache, IOptions<DeliveryCacheOptions> cacheOptions)
        {
            _memoryCache = memoryCache;
            _cacheOptions = cacheOptions.Value ?? new DeliveryCacheOptions();
        }

        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> valueFactory, Func<T, bool> shouldCache = null, Func<T, IEnumerable<string>> dependenciesFactory = null)
        {
            if (await TryGetAsync(key, out T entry))
            {
                return entry;
            }

            var entryLock = _createLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            try
            {
                await entryLock.WaitAsync();

                if (await TryGetAsync(key, out entry))
                {
                    return entry;
                }

                var value = await valueFactory();

                // Decide if the value should be cached based on the response
                if (shouldCache != null && !shouldCache(value))
                {
                    return value;
                }

                // Set different timeout for stale content
                var valueCacheOptions = new MemoryCacheEntryOptions();
                if (value is AbstractResponse ar && ar.HasStaleContent)
                {
                    valueCacheOptions.SetAbsoluteExpiration(_cacheOptions.StaleContentExpiration);
                }
                else
                {
                    valueCacheOptions.SetSlidingExpiration(_cacheOptions.DefaultExpiration);
                }

                var dependencies = dependenciesFactory?.Invoke(value) ?? new List<string>();
                var dependencyCacheOptions = new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove };
                foreach (var dependency in dependencies)
                {
                    var dependencyKey = dependency;
                    var dependencyLock = _dependencyLocks.GetOrAdd(dependencyKey, _ => new object());

                    if (!_memoryCache.TryGetValue(dependencyKey, out CancellationTokenSource tokenSource) || tokenSource.IsCancellationRequested)
                    {
                        lock (dependencyLock)
                        {
                            if (!_memoryCache.TryGetValue(dependencyKey, out tokenSource) || tokenSource.IsCancellationRequested)
                            {
                                tokenSource = _memoryCache.Set(dependencyKey, new CancellationTokenSource(), dependencyCacheOptions);
                            }
                        }
                    }

                    if (tokenSource != null)
                    {
                        valueCacheOptions.AddExpirationToken(new CancellationChangeToken(tokenSource.Token));
                    }
                }

                return _memoryCache.Set(key, value, valueCacheOptions);
            }
            finally
            {
                entryLock.Release();
            }
        }

        public Task<bool> TryGetAsync<T>(string key, out T value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return Task.FromResult(_memoryCache.TryGetValue(key, out value));
        }

        public async Task InvalidateDependencyAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (await TryGetAsync(key, out CancellationTokenSource tokenSource))
            {
                tokenSource.Cancel();
            }

        }

        public void Dispose()
        {
            _memoryCache?.Dispose();
        }

        public async Task ClearAsync()
        {
            foreach (var key in _createLocks.Keys)
            {
                _memoryCache.Remove(key);
            }
        }
    }
}
