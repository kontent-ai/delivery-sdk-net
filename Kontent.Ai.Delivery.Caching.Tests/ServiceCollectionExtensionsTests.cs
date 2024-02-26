﻿using System;
using FluentAssertions;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching.Extensions;
using Kontent.Ai.Delivery.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using Xunit;

namespace Kontent.Ai.Delivery.Caching.Tests
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
        public void AddDeliveryClientCacheWithDeliveryCacheOptions_ThrowsDecorationException(CacheTypeEnum cacheType)
        {
            Assert.Throws<DecorationException>(() => _serviceCollection.AddDeliveryClientCache(new DeliveryCacheOptions() { CacheType = cacheType }));
        }

        [Fact]
        public void AddDeliveryClientCacheWithNullDeliveryCacheOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClientCache(null));
        }

        [Fact]
        public void AddDeliveryClientCacheWitNoPreviousRegistrationDeliveryClient_ThrowsDecorationException()
        {
            Assert.Throws<DecorationException>(() => _serviceCollection.AddDeliveryClientCache(new DeliveryCacheOptions()));
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void AddDeliveryClient_WithNoCache_GetClient(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddDeliveryClient(new DeliveryOptions() { EnvironmentId = Guid.NewGuid().ToString() });
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
            _serviceCollection.AddDeliveryClient(new DeliveryOptions() { EnvironmentId = Guid.NewGuid().ToString() });
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
