using FakeItEasy;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.Tests
{
    public partial class ServiceCollectionExtensionsTests
    {
        public class NamedClient
        {
            private readonly IServiceCollection _serviceCollection;
            private readonly INamedServiceProvider _namedServiceProvider;
            private readonly IConfiguration _configuration;
            private readonly DeliveryOptions _deliveryOptions = new DeliveryOptions { ProjectId = Guid.NewGuid().ToString() };
            private readonly string _correctName = "correctName";
            private readonly string _wrongName = "wrongName";

            public NamedClient()
            {
                _namedServiceProvider = A.Fake<INamedServiceProvider>();
                _serviceCollection = new ServiceCollection()
                   .AddSingleton(_namedServiceProvider);
                _configuration = A.Fake<IConfiguration>();
            }

            [Fact]
            public void AddDeliveryClient_Options_GetClient_ReturnsClient()
            {
                _serviceCollection.AddDeliveryClient(_correctName, _deliveryOptions);
                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_correctName);

                client.Should().NotBeNull();
            }

            [Fact]
            public void AddDeliveryClient_NullOptions_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClient(_correctName, deliveryOptions: null));
            }

            [Fact]
            public void AddDeliveryClient_Options_GetClientWithWrongName_ReturnsNull()
            {
                _serviceCollection.AddDeliveryClient(_correctName, _deliveryOptions);
                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_wrongName);

                client.Should().BeNull();
            }

            [Fact]
            public void AddDeliveryClient_Options_GetClientWithNoName_ReturnsNull()
            {
                _serviceCollection.AddDeliveryClient(_correctName, _deliveryOptions);
                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get();

                client.Should().BeNull();
            }

            [Fact]
            public void AddDeliveryClient_Configuration_GetClient_ReturnsClient()
            {
                _serviceCollection.AddDeliveryClient(_correctName, _configuration);
                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_correctName);

                client.Should().NotBeNull();
            }

            [Fact]
            public void AddDeliveryClient_Configuration_GetClientWithWrongName_ReturnsNull()
            {
                _serviceCollection.AddDeliveryClient(_correctName, _configuration);
                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_wrongName);

                client.Should().BeNull();
            }

            [Fact]
            public void AddDeliveryClient_Configuration_GetClientWithNoName_ReturnsNull()
            {
                _serviceCollection.AddDeliveryClient(_correctName, _configuration);
                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get();

                client.Should().BeNull();
            }

            [Fact]
            public void AddDeliveryClient_OptionsBuilder_GetClient_ReturnsClient()
            {
                _serviceCollection.AddDeliveryClient(_correctName, (deliveryOptionsBuilder) => _deliveryOptions);
                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_correctName);

                client.Should().NotBeNull();
            }

            [Fact]
            public void AddDeliveryClient_NullOptionsBuilder_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClient(_correctName, buildDeliveryOptions: null));
            }

            [Fact]
            public void AddDeliveryClient_OptionsBuilder_GetClientWithWrongName_ReturnsNull()
            {
                _serviceCollection.AddDeliveryClient(_correctName, (deliveryOptionsBuilder) => _deliveryOptions);
                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_wrongName);

                client.Should().BeNull();
            }

            [Fact]
            public void AddDeliveryClient_OptionsBuilder_GetClientWithNoName_ReturnsNull()
            {
                _serviceCollection.AddDeliveryClient(_correctName, (deliveryOptionsBuilder) => _deliveryOptions);
                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get();

                client.Should().BeNull();
            }


            [Fact]
            public void AddDeliveryClient_Options_WithNamedProvider_GetClient_ReturnsNamedProvider()
            {
                A.CallTo(() => _namedServiceProvider.GetService<ITypeProvider>(_correctName))
                    .Returns(new FakeNamedTypeProvider());
                _serviceCollection.AddDeliveryClient(_correctName, _deliveryOptions);
                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();
                var client = factory.Get(_correctName);

                var typeProviderType = ((DeliveryClient)client).TypeProvider.GetType();

                typeProviderType.Should().Be<FakeNamedTypeProvider>();
            }

            [Fact]
            public void AddDeliveryClient_Options_WithNoNamedProvider_GetClient_ReturnsNoNamedProvider()
            {
                A.CallTo(() => _namedServiceProvider.GetService<ITypeProvider>(_correctName))
                    .Returns(null);
                _serviceCollection.AddSingleton<ITypeProvider, FakeTypeProvider>();
                _serviceCollection.AddDeliveryClient(_correctName, _deliveryOptions);
                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();
                var client = factory.Get(_correctName);

                var typeProviderType = ((DeliveryClient)client).TypeProvider.GetType();

                typeProviderType.Should().Be<FakeTypeProvider>();
            }

            [Fact]
            public void AddDeliveryClient_Options_WithNoNamedAndNamedProvider_GetClient_ReturnsNamedProvider()
            {
                A.CallTo(() => _namedServiceProvider.GetService<ITypeProvider>(_correctName))
                .Returns(new FakeNamedTypeProvider());
                _serviceCollection.AddSingleton<ITypeProvider, FakeTypeProvider>();
                _serviceCollection.AddDeliveryClient(_correctName, _deliveryOptions);
                var sp = _serviceCollection.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDeliveryClientFactory>();
                var client = factory.Get(_correctName);

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
}
