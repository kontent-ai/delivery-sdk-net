using System;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.Configuration;
using Kentico.Kontent.Delivery.Caching.Extensions;
using Kentico.Kontent.Delivery.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using Xunit;

namespace Kentico.Kontent.Delivery.Caching.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        private readonly ServiceCollection _serviceCollection;

        public ServiceCollectionExtensionsTests()
        {
            _serviceCollection = new ServiceCollection();
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void AddDeliveryClientCacheWithDeliveryCacheOptions_ThrowsMissingTypeRegistrationException(CacheTypeEnum cacheType)
        {
            Assert.Throws<MissingTypeRegistrationException>(() => _serviceCollection.AddDeliveryClientCache(new DeliveryCacheOptions() { CacheType = cacheType }));
        }

        [Fact]
        public void AddDeliveryClientCacheWithNullDeliveryCacheOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClientCache(null));
        }

        [Fact]
        public void AddDeliveryClientCacheWitNoPreviousRegistrationDeliveryClient_ThrowsMissingTypeRegistrationException()
        {
            Assert.Throws<MissingTypeRegistrationException>(() => _serviceCollection.AddDeliveryClientCache(new DeliveryCacheOptions()));
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void AddDeliveryClient_WithNoCache_GetClient(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddDeliveryClient(new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            _serviceCollection.AddDeliveryClientCache(new DeliveryCacheOptions()
            {
                CacheType = cacheType
            });

            var sp = _serviceCollection.BuildServiceProvider();
            var factory = sp.GetRequiredService<IDeliveryClientFactory>();

            var client = factory.Get();

            client.Should().NotBeNull();
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void AddDeliveryClient_CacheWithDeliveryCacheOptions_ThrowsNotImeplementException(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddDeliveryClient(new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            _serviceCollection.AddDeliveryClientCache(new DeliveryCacheOptions()
            {
                CacheType = cacheType
            });

            var sp = _serviceCollection.BuildServiceProvider();
            var factory = sp.GetRequiredService<IDeliveryClientFactory>();

            Assert.Throws<NotImplementedException>(() => factory.Get("WrongName"));
        }
    }
}
