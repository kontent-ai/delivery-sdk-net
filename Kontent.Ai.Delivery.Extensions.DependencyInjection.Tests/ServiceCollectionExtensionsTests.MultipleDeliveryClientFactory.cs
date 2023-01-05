using FakeItEasy;
using FluentAssertions;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Caching.Factories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using Xunit;

namespace Kontent.Ai.Delivery.Extensions.DependencyInjection.Tests
{
    public partial class ServiceCollectionExtensionsTests
    {
        public class MultipleDeliveryClientFactory
        {
            private readonly IServiceCollection _serviceCollection;
            private readonly DeliveryOptions _deliveryOptions = new DeliveryOptions { ProjectId = Guid.NewGuid().ToString() };
            private readonly string _correctName = "correctName";
            private readonly string _wrongName = "wrongName";

            private readonly IDeliveryCacheManager _cacheManager = CacheManagerFactory.Create(
                    new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
                    Options.Create(new DeliveryCacheOptions
                    {
                        CacheType = CacheTypeEnum.Distributed
                    })
            );

            public MultipleDeliveryClientFactory()
            {
                _serviceCollection = new ServiceCollection();
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_RegisterCorrectType()
            {
                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                factory.Should().BeOfType<DependencyInjection.MultipleDeliveryClientFactory>();
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_NullConfig_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddMultipleDeliveryClientFactory(null));
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_GetClient_ReturnsClient()
            {
                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.AddDeliveryClient(
                        _correctName,
                        _ => _deliveryOptions
                        ).Build()
                    );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_correctName);

                client.Should().NotBeNull();
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_AddDeliveryClient_ReturnsCorrectTypeOfClient()
            {
                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.AddDeliveryClient(
                        _correctName,
                        _ => _deliveryOptions
                        ).Build()
                    );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_correctName);

                client.Should().BeOfType<DeliveryClient>();
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_AddDeliveryClientDistributedCache_ReturnsCorrectTypeOfClient()
            {
                var clientName = "MemoryDistributedCache";
                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.AddDeliveryClientCache(
                        clientName,
                        deliveryOptionBuilder => _deliveryOptions,
                        _cacheManager
                    ).Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(clientName);
                client.Should().BeOfType<DeliveryClientCache>();
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_AddDeliveryClientMemoryCache_ReturnsCorrectTypeOfClient()
            {
                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder
                        .AddDeliveryClientCache(_correctName, deliveryOptionBuilder => _deliveryOptions, _cacheManager)
                        .Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_correctName);
                client.Should().BeOfType<DeliveryClientCache>();
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_AddDeliveryClient_NullOptions_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddMultipleDeliveryClientFactory(factoryBuilder => factoryBuilder.AddDeliveryClient(_correctName, _ => null).Build()));
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_AddDeliveryClientCache_NullOptions_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() =>
                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder
                        .AddDeliveryClientCache(_correctName, _ => null, _cacheManager)
                        .Build()
                ));
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_AddDeliveryClient_NullName_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddMultipleDeliveryClientFactory(factoryBuilder => factoryBuilder.AddDeliveryClient(null, _ => _deliveryOptions).Build()));
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_AddDeliveryClientCache_NullName_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() =>
                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder
                        .AddDeliveryClientCache(null, _ => _deliveryOptions, _cacheManager)
                        .Build()
                ));
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_AddDeliveryClientCache_NullCacheManager_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() =>
                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder
                        .AddDeliveryClientCache(_correctName, _ => _deliveryOptions, null)
                        .Build()
                ));
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_GetClientWithWrongName_ThrowsArgumentException()
            {
                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.AddDeliveryClient(
                        _correctName,
                        _ => _deliveryOptions
                        ).Build()
                    );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                Assert.Throws<ArgumentException>(() => factory.Get(_wrongName));
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_GetClientWithNoName_ThrowsNotImplementedException()
            {
                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                Assert.Throws<NotImplementedException>(() => factory.Get());
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_GetClientWithNullName_ThrowsArgumentNullException()
            {
                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                Assert.Throws<ArgumentNullException>(() => factory.Get(null));
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_GetClientWithEmptyName_ThrowsArgumentException()
            {
                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                Assert.Throws<ArgumentException>(() => factory.Get(string.Empty));
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_WithCustomTypeProvider_ServicesCorrectlyRegistered()
            {
                var typeProvider = A.Fake<ITypeProvider>();

                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder
                    .AddDeliveryClient(
                        _correctName,
                        _ => _deliveryOptions,
                        optionalConfig => optionalConfig
                            .WithTypeProvider(typeProvider)
                        )
                    .Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(_correctName);
                var bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var registeredTypeProvider = client.GetType()
                    .GetField("TypeProvider", bindingFlags)
                    .GetValue(client);
                var registeredModelProvider = client.GetType()
                    .GetField("ModelProvider", bindingFlags)
                    .GetValue(client);
                var innerRegisteredTypeProvider = registeredModelProvider.GetType()
                    .GetProperty("TypeProvider", bindingFlags)
                    .GetValue(registeredModelProvider);


                registeredTypeProvider.Should().Be(typeProvider);
                innerRegisteredTypeProvider.Should().Be(typeProvider);
            }

            [Fact]
            public void AddMultipleDeliveryClientFactory_WithTwoClientsWithCustomTypeProvider_ServicesCorrectlyRegistered()
            {
                var typeProviderA = A.Fake<ITypeProvider>();
                var typeProviderB = A.Fake<ITypeProvider>();
                var clientAName = "Marketing";
                var clientBName = "Finance";
                var projectAID = "923850ac-5869-4743-8414-eb278e7beb69";
                var projectBID = "88d518c5-db60-432d-918a-14dba79c63ac";

                _serviceCollection.AddMultipleDeliveryClientFactory(
                    factoryBuilder => factoryBuilder
                    .AddDeliveryClient(
                        clientAName,
                        builder => builder
                            .WithProjectId(projectAID)
                            .UseProductionApi()
                            .Build(),
                        optionalConfig => optionalConfig
                            .WithTypeProvider(typeProviderA)
                    )
                    .AddDeliveryClient(
                        clientBName,
                        builder => builder
                            .WithProjectId(projectBID)
                            .UseProductionApi()
                            .Build(),
                        optionalConfig => optionalConfig
                            .WithTypeProvider(typeProviderB)
                        )
                    .Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                var bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var clientA = factory.Get(clientAName);
                var registeredTypeProviderA = clientA.GetType()
                    .GetField("TypeProvider", bindingFlags)
                    .GetValue(clientA);
                var registeredModelProviderA = clientA.GetType()
                    .GetField("ModelProvider", bindingFlags)
                    .GetValue(clientA);
                var innerRegisteredTypeProviderA = registeredModelProviderA.GetType()
                    .GetProperty("TypeProvider", bindingFlags)
                    .GetValue(registeredModelProviderA);
                var deliveryOptionsA = (IOptionsMonitor<DeliveryOptions>)clientA.GetType()
                   .GetField("DeliveryOptions", bindingFlags)
                   .GetValue(clientA);
                var clientB = factory.Get(clientBName);
                var registeredTypeProviderB = clientB.GetType()
                    .GetField("TypeProvider", bindingFlags)
                    .GetValue(clientB);
                var registeredModelProviderB = clientB.GetType()
                    .GetField("ModelProvider", bindingFlags)
                    .GetValue(clientB);
                var innerRegisteredTypeProviderB = registeredModelProviderB.GetType()
                    .GetProperty("TypeProvider", bindingFlags)
                    .GetValue(registeredModelProviderB);
                var deliveryOptionsB = (IOptionsMonitor<DeliveryOptions>)clientB.GetType()
                   .GetField("DeliveryOptions", bindingFlags)
                   .GetValue(clientB);

                deliveryOptionsA.CurrentValue.ProjectId.Should().Be(projectAID);
                registeredTypeProviderA.Should().Be(typeProviderA);
                innerRegisteredTypeProviderA.Should().Be(typeProviderA);

                deliveryOptionsB.CurrentValue.ProjectId.Should().Be(projectBID);
                registeredTypeProviderB.Should().Be(typeProviderB);
                innerRegisteredTypeProviderB.Should().Be(typeProviderB);
            }
        }
    }
}