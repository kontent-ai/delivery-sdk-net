using System.Collections.Generic;
using System.Net.Http;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Kentico.Kontent.Delivery.Configuration.DeliveryOptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Kentico.Kontent.Delivery.Caching.Tests
{
    public class Scenario
    {
        private readonly Dictionary<string, int> _requestCounter;
        private readonly IDeliveryCacheManager _cacheManager;
        public IDeliveryClient CachingClient { get; }

        public Scenario(IMemoryCache memoryCache, HttpClient httpClient, DeliveryOptions deliveryOptions, Dictionary<string, int> requestCounter)
        {
            _requestCounter = requestCounter;
            _cacheManager = new DeliveryCacheManager(memoryCache, Options.Create(new DeliveryCacheOptions()));
            var baseClient = DeliveryClientBuilder.WithOptions(_ => deliveryOptions).WithDeliveryHttpClient(new DeliveryHttpClient(httpClient)).Build();
            CachingClient = new DeliveryClientCache(_cacheManager, baseClient);
        }

        public void InvalidateDependency(string dependency) => _cacheManager.InvalidateDependencyAsync(dependency);

        public int GetRequestCount(string url) => _requestCounter.GetValueOrDefault(url);
    }
}
