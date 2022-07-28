using FakeItEasy;
using FluentAssertions;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Kontent.Ai.Delivery.Extensions.DependencyInjection.Tests
{
    public partial class ServiceCollectionExtensionsTests
    {
        public class NamedCacheClient
        {
            private readonly IServiceCollection _serviceCollection;
            private readonly DeliveryOptions _deliveryOptions = new DeliveryOptions { ProjectId = Guid.NewGuid().ToString() };
            private readonly string _correctName = "correctName";
            private readonly string _wrongName = "wrongName";

            public NamedCacheClient()
            {
                _serviceCollection = new ServiceCollection()
                    .AddSingleton(A.Fake<INamedServiceProvider>());
            }

            [Theory]
            [InlineData(CacheTypeEnum.Memory)]
            [InlineData(CacheTypeEnum.Distributed)]
            public void AddDeliveryClient_CacheOptions_GetClient_ReturnsClient(CacheTypeEnum cacheType)
            {
                _serviceCollection.AddDeliveryClient(_correctName, _deliveryOptions);
                _serviceCollection.AddDeliveryClientCache(_correctName, new DeliveryCacheOptions()
                {
                    CacheType = cacheType
                });

                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_correctName);

                client.Should().BeOfType(typeof(DeliveryClientCache));
                factory.Should().BeOfType(typeof(NamedDeliveryClientCacheFactory));
                client.Should().NotBeNull();
            }

            [Fact]
            public void AddDeliveryClient_NullCacheOption_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClientCache(_correctName, null));
            }

            [Theory]
            [InlineData(CacheTypeEnum.Memory)]
            [InlineData(CacheTypeEnum.Distributed)]
            public void AddDeliveryClient_CacheOptions_GetClientWithWrongName_ReturnsNull(CacheTypeEnum cacheType)
            {
                _serviceCollection.AddDeliveryClient(_correctName, _deliveryOptions);
                _serviceCollection.AddDeliveryClientCache(_correctName, new DeliveryCacheOptions()
                {
                    CacheType = cacheType
                });

                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_wrongName);

                factory.Should().BeOfType(typeof(NamedDeliveryClientCacheFactory));
                client.Should().BeNull();
            }

            [Theory]
            [InlineData(CacheTypeEnum.Memory)]
            [InlineData(CacheTypeEnum.Distributed)]
            public void AddDeliveryClient_CacheOptions_GetClientWithNoName_ReturnsNull(CacheTypeEnum cacheType)
            {
                _serviceCollection.AddDeliveryClient(_correctName, _deliveryOptions);
                _serviceCollection.AddDeliveryClientCache(_correctName, new DeliveryCacheOptions()
                {
                    CacheType = cacheType
                });

                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get();

                factory.Should().BeOfType(typeof(NamedDeliveryClientCacheFactory));
                client.Should().BeNull();
            }

            [Theory]
            [InlineData(CacheTypeEnum.Memory)]
            [InlineData(CacheTypeEnum.Distributed)]
            public void AddDeliveryClient_CacheOptions_WithWrongName_GetClient_ReturnsNull(CacheTypeEnum cacheType)
            {
                _serviceCollection.AddDeliveryClient(_correctName, _deliveryOptions);
                _serviceCollection.AddDeliveryClientCache(_wrongName, new DeliveryCacheOptions()
                {
                    CacheType = cacheType
                });

                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_wrongName);

                factory.Should().BeOfType(typeof(NamedDeliveryClientCacheFactory));
                client.Should().BeNull();
            }
        }
    }
}
