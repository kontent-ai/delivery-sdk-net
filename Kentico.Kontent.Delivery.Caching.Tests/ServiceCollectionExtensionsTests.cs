using System;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
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
        public void AddDeliveryClient_CacheWithDeliveryCacheOptions_GetNull(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddDeliveryClient(new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            _serviceCollection.AddDeliveryClientCache(new DeliveryCacheOptions()
            {
                CacheType = cacheType
            });

            var sp = _serviceCollection.BuildServiceProvider();
            var factory = sp.GetRequiredService<IDeliveryClientFactory>();

            var client = factory.Get("WrongName");

            client.Should().BeNull();
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void AddDeliveryNamedClient_CacheWithDeliveryCacheOptions_GetNamedClient(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddDeliveryClient("named", new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            _serviceCollection.AddDeliveryClientCache("named", new DeliveryCacheOptions()
            {
                CacheType = cacheType
            });

            var sp = _serviceCollection.BuildServiceProvider();
            var factory = sp.GetRequiredService<IDeliveryClientFactory>();

            var client = factory.Get("named");

            client.Should().NotBeNull();
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void AddDeliveryNamedClient_CacheWithDeliveryCacheOptions_GetNull(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddDeliveryClient("named", new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            _serviceCollection.AddDeliveryClientCache("named", new DeliveryCacheOptions()
            {
                CacheType = cacheType
            });

            var sp = _serviceCollection.BuildServiceProvider();
            var factory = sp.GetRequiredService<IDeliveryClientFactory>();

            var client = factory.Get("WrongName");

            client.Should().BeNull();
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void AddDeliveryClientCacheNamedWithDeliveryCacheOptions_ThrowsInvalidOperationException(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddDeliveryClientCache("named", new DeliveryCacheOptions() { CacheType = cacheType });
            var sp = _serviceCollection.BuildServiceProvider();

            Assert.Throws<InvalidOperationException>(() => sp.GetRequiredService<IDeliveryClientFactory>());
        }

        [Fact]
        public void AddDeliveryClientCacheWithNullDeliveryCacheOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClientCache(null));
        }

        [Fact]
        public void AddDeliveryClientCacheNamedWithNullDeliveryCacheOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClientCache("named", null));
        }

        [Fact]
        public void AddDeliveryClientCacheWitNoPreviousRegistrationDeliveryClient_ThrowsMissingTypeRegistrationException()
        {
            Assert.Throws<MissingTypeRegistrationException>(() => _serviceCollection.AddDeliveryClientCache(new DeliveryCacheOptions()));
        }

        [Fact]
        public void AddDeliveryClientNamedCacheWitNoPreviousRegistrationDeliveryClient_ThrowsInvalidOperationException()
        {
            _serviceCollection.AddDeliveryClientCache("named", new DeliveryCacheOptions());
            var sp = _serviceCollection.BuildServiceProvider();

            Assert.Throws<InvalidOperationException>(() => sp.GetRequiredService<IDeliveryClientFactory>());
        }
    }
}
