using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Caching.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using System;
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
