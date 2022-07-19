using FakeItEasy;
using FluentAssertions;
using Kentico.Kontent.Delivery.Caching.Factories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kentico.Kontent.Delivery.Caching.Tests
{
    public class CacheManagerFactoryTests
    {
        private IOptions<DeliveryCacheOptions> _options;
        private IDistributedCache _distributedCache;
        private IMemoryCache _memoryCache;

        public CacheManagerFactoryTests()
        {
            _options = A.Fake<IOptions<DeliveryCacheOptions>>();
            _distributedCache = A.Fake<IDistributedCache>();
            _memoryCache = A.Fake<IMemoryCache>();
        }

        [Fact]
        public void Create_DistributedCache()
        {
            var deliveryCacheManager = CacheManagerFactory.Create(_distributedCache, _options);

            deliveryCacheManager.Should().NotBeNull();
        }

        [Fact]
        public void Create_MemoryCache()
        {
            var deliveryCacheManager = CacheManagerFactory.Create(_memoryCache, _options);

            deliveryCacheManager.Should().NotBeNull();
        }
    }
}
