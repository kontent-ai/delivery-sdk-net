using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;

namespace Kontent.Ai.Delivery.Caching.Tests
{
    public class ScenarioBuilder
    {
        private readonly string _environmentId = Guid.NewGuid().ToString();
        private readonly string _baseUrl;
        private readonly CacheTypeEnum _cacheType;
        private readonly CacheExpirationType _cacheExpirationType;
        private readonly DistributedCacheResilientPolicy _distributedCacheResilientPolicy;
        private readonly ILoggerFactory _loggerFactory;

        private readonly MemoryCache _memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        private readonly IDistributedCache _distributedCache;
        private readonly Dictionary<string, int> _requestCounter = new Dictionary<string, int>();

        private readonly List<(string key, Action<MockHttpMessageHandler> configure)> _configurations = new List<(string key, Action<MockHttpMessageHandler> configure)>();

        public ScenarioBuilder(
            CacheTypeEnum cacheType = CacheTypeEnum.Memory,
            CacheExpirationType cacheExpirationType = CacheExpirationType.Sliding,
            bool brokenCache = false,
            DistributedCacheResilientPolicy distributedCacheResilientPolicy = DistributedCacheResilientPolicy.Crash,
            ILoggerFactory loggerFactory = null)
        {
            _baseUrl = $"https://deliver.kontent.ai/{_environmentId}/";
            _cacheType = cacheType;
            _cacheExpirationType = cacheExpirationType;
            _distributedCacheResilientPolicy = distributedCacheResilientPolicy;
            _distributedCache = brokenCache ?
                new BrokenDistributedCache(Options.Create(new MemoryDistributedCacheOptions())) :
                new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        }

        public ScenarioBuilder WithResponse(string relativeUrl, object responseObject)
        {
            var url = $"{_baseUrl}{relativeUrl.TrimStart('/')}";

            void ConfigureMock(MockHttpMessageHandler mockHttp) => mockHttp
                .When(url)
                .Respond("application/json", _ => CreateStreamAndCount(relativeUrl, responseObject));

            var existingIndex = _configurations.FindIndex(x => x.key == url);
            if (existingIndex >= 0)
            {
                _configurations[existingIndex] = (url, ConfigureMock);
            }
            else
            {
                _configurations.Add((url, ConfigureMock));
            }

            return this;
        }

        public ScenarioBuilder WithResponse(string relativeUrl, IEnumerable<KeyValuePair<string, string>> requestHeaders, object responseObject, IEnumerable<KeyValuePair<string, string>> responseHeaders)
        {
            var url = $"{_baseUrl}{relativeUrl.TrimStart('/')}";
            var key = url + (requestHeaders == null
                ? ""
                : $"|{string.Join(";", requestHeaders.Select(p => $"{p.Key}:{p.Value}"))}");

            void ConfigureMock(MockHttpMessageHandler mockHttp) => mockHttp
                .When(url)
                .WithHeaders(requestHeaders ?? new List<KeyValuePair<string, string>>())
                .Respond(responseHeaders ?? new List<KeyValuePair<string, string>>(), "application/json", _ => CreateStreamAndCount(relativeUrl, responseObject));

            var existingIndex = _configurations.FindIndex(x => x.key == key);
            if (existingIndex >= 0)
            {
                _configurations[existingIndex] = (key, ConfigureMock);
            }
            else
            {
                _configurations.Add((key, ConfigureMock));
            }

            return this;
        }

        private Stream CreateStreamAndCount(string url, object responseObject)
        {
            _requestCounter[url] = _requestCounter.GetValueOrDefault(url) + 1;
            var bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(responseObject));
            return new MemoryStream(bytes);
        }

        public Scenario Build()
        {
            var mockHttp = new MockHttpMessageHandler();
            foreach (var (_, configure) in _configurations)
            {
                configure?.Invoke(mockHttp);
            }
            if (_cacheType == CacheTypeEnum.Memory)
            {
                return new Scenario(_memoryCache, _cacheExpirationType, mockHttp.ToHttpClient(), new DeliveryOptions { EnvironmentId = _environmentId }, _requestCounter);
            }
            else
            {
                return new Scenario(_distributedCache, _cacheExpirationType, _distributedCacheResilientPolicy, mockHttp.ToHttpClient(), new DeliveryOptions { EnvironmentId = _environmentId }, _requestCounter, _loggerFactory);
            }
        }
    }
}
