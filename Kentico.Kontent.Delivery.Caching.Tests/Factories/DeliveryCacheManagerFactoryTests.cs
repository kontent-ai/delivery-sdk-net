using FakeItEasy;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Caching.Extensions;
using Kentico.Kontent.Delivery.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using Xunit;

namespace Kentico.Kontent.Delivery.Caching.Tests.Factories
{
    public class DeliveryCacheManagerFactoryTests
    {
        private readonly ServiceCollection _serviceCollection;

        private const string _clientName = "ClientName";

        public DeliveryCacheManagerFactoryTests()
        {
            _serviceCollection = new ServiceCollection();
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void GetNamedDeliveryCacheManager_WithCorrectName_GetDeliveryCacheManager(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddDeliveryClient(_clientName, new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            _serviceCollection.AddDeliveryClientCache(_clientName, new DeliveryCacheOptions()
            {
                CacheType = cacheType
            });

            var sp = _serviceCollection.BuildServiceProvider();
            var factory = sp.GetRequiredService<IDeliveryClientFactory>();

            var result = factory.Get(_clientName);

            result.Should().NotBeNull();
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void GetNamedDeliveryCacheManager_WithWrongName_GetNull(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddDeliveryClient(new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            _serviceCollection.AddDeliveryClientCache(new DeliveryCacheOptions()
            {
                CacheType = cacheType
            });

            var sp = _serviceCollection.BuildServiceProvider();
            var factory = sp.GetRequiredService<IDeliveryClientFactory>();

            var result = factory.Get("WrongName");

            result.Should().BeNull();
        }
    }
}
