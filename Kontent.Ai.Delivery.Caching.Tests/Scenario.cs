using System.Collections.Generic;
using System.Net.Http;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Builders.DeliveryClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.Caching.Tests
{
    public class Scenario
    {
        private readonly Dictionary<string, int> _requestCounter;
        private readonly IDeliveryCacheManager _cacheManager;
        public IDeliveryClient CachingClient { get; }

        public Scenario(IMemoryCache memoryCache, CacheExpirationType cacheExpirationType, HttpClient httpClient, DeliveryOptions deliveryOptions, Dictionary<string, int> requestCounter)
        {
            _requestCounter = requestCounter;
            _cacheManager = new MemoryCacheManager(memoryCache, Options.Create(new DeliveryCacheOptions { DefaultExpirationType = cacheExpirationType }));
            var baseClient = DeliveryClientBuilder.WithOptions(_ => deliveryOptions).WithDeliveryHttpClient(new DeliveryHttpClient(httpClient)).Build();
            CachingClient = new DeliveryClientCache(_cacheManager, baseClient);
        }

        public Scenario(
            IDistributedCache distributedCache,
            CacheExpirationType cacheExpirationType,
            DistributedCacheResilientPolicy distributedCacheResilientPolicy,
            HttpClient httpClient,
            DeliveryOptions deliveryOptions,
            Dictionary<string, int> requestCounter,
            ILoggerFactory loggerFactory)
        {
            _requestCounter = requestCounter;
            _cacheManager = new DistributedCacheManager(distributedCache, Options.Create(new DeliveryCacheOptions { DefaultExpirationType = cacheExpirationType, DistributedCacheResilientPolicy = distributedCacheResilientPolicy }), loggerFactory);
            var baseClient = DeliveryClientBuilder.WithOptions(_ => deliveryOptions).WithDeliveryHttpClient(new DeliveryHttpClient(httpClient)).Build();
            CachingClient = new DeliveryClientCache(_cacheManager, baseClient);
        }

        public void InvalidateDependency(string dependency) => _cacheManager.InvalidateDependencyAsync(dependency);

        public int GetRequestCount(string url) => _requestCounter.GetValueOrDefault(url);
    }
}
