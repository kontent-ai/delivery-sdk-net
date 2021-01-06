using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Caching;
using Microsoft.Extensions.DependencyInjection;
using Kentico.Kontent.Delivery.Extensions.Autofac.DependencyInjection.Extensions;
using System;
using Xunit;
using FluentAssertions;
using Autofac;
using FakeItEasy;

namespace Kentico.Kontent.Delivery.Extensions.Autofac.DependencyInjection.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly IComponentContext _container;

        public ServiceCollectionExtensionsTests()
        {
            _serviceCollection = new ServiceCollection()
                .AddMemoryCache()
                .AddDistributedMemoryCache();
            _container = A.Fake<IComponentContext>();
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void AddDeliveryNamedClient_CacheWithDeliveryCacheOptions_GetNamedClient(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddSingleton(_container);
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
            _serviceCollection.AddSingleton(_container);
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
        public void AddDeliveryNamedClient_CacheWithDeliveryCacheOptions_GetNoNamedClientNull(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddSingleton(_container);
            _serviceCollection.AddDeliveryClient("named", new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            _serviceCollection.AddDeliveryClientCache("named", new DeliveryCacheOptions()
            {
                CacheType = cacheType
            });

            var sp = _serviceCollection.BuildServiceProvider();
            var factory = sp.GetRequiredService<IDeliveryClientFactory>();

            var client = factory.Get();

            client.Should().BeNull();
        }

        [Fact]
        public void AddDeliveryNamedClient_DeliveryOptions_GetNamedClient()
        {
            _serviceCollection.AddSingleton(_container);
            _serviceCollection.AddDeliveryClient("named", new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            var sp = _serviceCollection.BuildServiceProvider();

            var factory = sp.GetRequiredService<IDeliveryClientFactory>();
            var client = factory.Get("named");

            client.Should().NotBeNull();
        }

        [Fact]
        public void AddDeliveryNamedClient_DeliveryOptions_GetNull()
        {
            _serviceCollection.AddSingleton(_container);
            _serviceCollection.AddDeliveryClient("named", new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            var sp = _serviceCollection.BuildServiceProvider();

            var factory = sp.GetRequiredService<IDeliveryClientFactory>();
            var client = factory.Get("WrongName");

            client.Should().BeNull();
        }

        [Fact]
        public void AddDeliveryNamedClient_DeliveryOptions_GetNoNamedClientNull()
        {
            _serviceCollection.AddSingleton(_container);
            _serviceCollection.AddDeliveryClient("named", new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            var sp = _serviceCollection.BuildServiceProvider();

            var factory = sp.GetRequiredService<IDeliveryClientFactory>();
            var client = factory.Get();

            client.Should().BeNull();
        }

    }
}
