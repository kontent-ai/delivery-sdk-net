using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Caching;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;
using FluentAssertions;
using FakeItEasy;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly INamedServiceProvider _namedServiceProvider;

        public ServiceCollectionExtensionsTests()
        {
            _serviceCollection = new ServiceCollection()
                .AddMemoryCache()
                .AddDistributedMemoryCache();
            _namedServiceProvider = A.Fake<INamedServiceProvider>();
        }

        [Theory]
        [InlineData(CacheTypeEnum.Memory)]
        [InlineData(CacheTypeEnum.Distributed)]
        public void AddDeliveryNamedClient_CacheWithDeliveryCacheOptions_GetNamedClient(CacheTypeEnum cacheType)
        {
            _serviceCollection.AddSingleton(_namedServiceProvider);
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
            _serviceCollection.AddSingleton(_namedServiceProvider);
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
            _serviceCollection.AddSingleton(_namedServiceProvider);
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
            _serviceCollection.AddSingleton(_namedServiceProvider);
            _serviceCollection.AddDeliveryClient("named", new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            var sp = _serviceCollection.BuildServiceProvider();

            var factory = sp.GetRequiredService<IDeliveryClientFactory>();
            var client = factory.Get("named");

            client.Should().NotBeNull();
        }

        [Fact]
        public void AddDeliveryNamedClient_DeliveryOptions_GetNull()
        {
            _serviceCollection.AddSingleton(_namedServiceProvider);
            _serviceCollection.AddDeliveryClient("named", new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            var sp = _serviceCollection.BuildServiceProvider();

            var factory = sp.GetRequiredService<IDeliveryClientFactory>();
            var client = factory.Get("WrongName");

            client.Should().BeNull();
        }

        [Fact]
        public void AddDeliveryNamedClient_DeliveryOptions_GetNoNamedClientNull()
        {
            _serviceCollection.AddSingleton(_namedServiceProvider);
            _serviceCollection.AddDeliveryClient("named", new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            var sp = _serviceCollection.BuildServiceProvider();

            var factory = sp.GetRequiredService<IDeliveryClientFactory>();
            var client = factory.Get();

            client.Should().BeNull();
        }

        [Fact]
        public void AddDeliveryNamedClient_WithNamedTypeProvider_GetNamedTypeProvider()
        {
            A.CallTo(() => _namedServiceProvider.GetService<ITypeProvider>("named"))
                .Returns(new FakeNamedTypeProvider());
            _serviceCollection.AddSingleton(_namedServiceProvider);
            _serviceCollection.AddDeliveryClient("named", new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            var sp = _serviceCollection.BuildServiceProvider();

            var factory = sp.GetRequiredService<IDeliveryClientFactory>();
            var client = factory.Get("named");

            var typeProviderType = ((DeliveryClient)client).TypeProvider.GetType();

            typeProviderType.Should().Be<FakeNamedTypeProvider>();
        }

        [Fact]
        public void AddDeliveryNamedClient_WithTypeProvider_GetTypeProvider()
        {
            A.CallTo(() => _namedServiceProvider.GetService<ITypeProvider>("named"))
                .Returns(null);
            _serviceCollection.AddSingleton<ITypeProvider, FakeTypeProvider>();
            _serviceCollection.AddSingleton(_namedServiceProvider);
            _serviceCollection.AddDeliveryClient("named", new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            var sp = _serviceCollection.BuildServiceProvider();

            var factory = sp.GetRequiredService<IDeliveryClientFactory>();
            var client = factory.Get("named");

            var typeProviderType = ((DeliveryClient)client).TypeProvider.GetType();

            typeProviderType.Should().Be<FakeTypeProvider>();
        }

        [Fact]
        public void AddDeliveryNamedClient_WithNamedAndNoNamedTypeProvider_GetNamedTypePovider()
        {
            A.CallTo(() => _namedServiceProvider.GetService<ITypeProvider>("named"))
                .Returns(new FakeNamedTypeProvider());
            _serviceCollection.AddSingleton<ITypeProvider, FakeTypeProvider>();
            _serviceCollection.AddSingleton(_namedServiceProvider);
            _serviceCollection.AddDeliveryClient("named", new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() });
            var sp = _serviceCollection.BuildServiceProvider();

            var factory = sp.GetRequiredService<IDeliveryClientFactory>();
            var client = factory.Get("named");

            var typeProviderType = ((DeliveryClient)client).TypeProvider.GetType();

            typeProviderType.Should().Be<FakeNamedTypeProvider>();
        }

        private class FakeTypeProvider : ITypeProvider
        {
            public string GetCodename(Type contentType)
            {
                throw new NotImplementedException();
            }

            public Type GetType(string contentType)
            {
                throw new NotImplementedException();
            }
        }

        private class FakeNamedTypeProvider : ITypeProvider
        {
            public string GetCodename(Type contentType)
            {
                throw new NotImplementedException();
            }

            public Type GetType(string contentType)
            {
                throw new NotImplementedException();
            }
        }
    }
}
