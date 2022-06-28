using FakeItEasy;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Caching;
using Kentico.Kontent.Delivery.Caching.Factories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using Xunit;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.Tests
{
    public partial class ServiceCollectionExtensionsTests
    {
        public class DeliveryClientFactory
        {
            private readonly IServiceCollection _serviceCollection;
            private readonly IConfiguration _configuration;
            private readonly DeliveryOptions _deliveryOptions = new DeliveryOptions { ProjectId = Guid.NewGuid().ToString() };
            private readonly string _correctName = "correctName";
            private readonly string _wrongName = "wrongName";

            public DeliveryClientFactory()
            {
                _serviceCollection = new ServiceCollection();
                _configuration = A.Fake<IConfiguration>();
            }

            [Fact]
            public void AddDeliveryClientFactory_RegisterCorrectType()
            {
                _serviceCollection.AddDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                factory.Should().BeOfType<DependencyInjection.DeliveryClientFactory>();
            }

            [Fact]
            public void AddDeliveryClientFactory_NullConfig_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClientFactory(null));
            }

            [Fact]
            public void AddDeliveryClientFactory_GetClient_ReturnsClient()
            {
                _serviceCollection.AddDeliveryClientFactory(
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
            public void AddDeliveryClientFactory_AddDeliveryClient_ReturnsCorrectTypeOfClient()
            {
                _serviceCollection.AddDeliveryClientFactory(
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
            public void AddDeliveryClientFactory_AddDeliveryClientDistributedCache_ReturnsCorrectTypeOfClient()
            {
                var clientName = "MemoryDistributedCache";
                _serviceCollection.AddDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.AddDeliveryClientCache(
                        clientName,
                        deliveryOptionBuilder => _deliveryOptions,
                        CacheManagerFactory.Create(
                            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
                            Options.Create(new DeliveryCacheOptions
                            {
                                CacheType = CacheTypeEnum.Distributed
                            })
                        )
                    ).Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(clientName);
                client.Should().BeOfType<DeliveryClientCache>();
            }

            [Fact]
            public void AddDeliveryClientFactory_AddDeliveryClientMemoryCache_ReturnsCorrectTypeOfClient()
            {
                var clientName = "MemoryCache";
                _serviceCollection.AddDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.AddDeliveryClientCache(
                        clientName,
                        deliveryOptionBuilder => _deliveryOptions,
                        CacheManagerFactory.Create(
                        new MemoryCache(new MemoryCacheOptions()),
                        Options.Create(
                            new DeliveryCacheOptions
                            {
                                CacheType = CacheTypeEnum.Memory
                            })
                        )
                    ).Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                var client = factory.Get(clientName);
                client.Should().BeOfType<DeliveryClientCache>();
            }

            [Fact]
            public void AddDeliveryClientFactory_NullOptions_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClientFactory(factoryBuilder => factoryBuilder.AddDeliveryClient(_correctName, null).Build()));
            }

            [Fact]
            public void AddDeliveryClientFactory_NullName_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClientFactory(factoryBuilder => factoryBuilder.AddDeliveryClient(null, _ => _deliveryOptions).Build()));
            }


            [Fact]
            public void AddDeliveryClientFactory_GetClientWithWrongName_ThrowsArgumentException()
            {
                _serviceCollection.AddDeliveryClientFactory(
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
            public void AddDeliveryClientFactory_GetClientWithNoName_ThrowsNotImplementedException()
            {
                _serviceCollection.AddDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                Assert.Throws<NotImplementedException>(() => factory.Get());
            }

            [Fact]
            public void AddDeliveryClientFactory_GetClientWithNullName_ThrowsArgumentNullException()
            {
                _serviceCollection.AddDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                Assert.Throws<ArgumentNullException>(() => factory.Get(null));
            }

            [Fact]
            public void AddDeliveryClientFactory_GetClientWithEmptyName_ThrowsArgumentException()
            {
                _serviceCollection.AddDeliveryClientFactory(
                    factoryBuilder => factoryBuilder.Build()
                );

                var services = _serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IDeliveryClientFactory>();

                Assert.Throws<ArgumentException>(() => factory.Get(string.Empty));
            }

            [Fact]
            public void AddDeliveryClientFactory_WithCustomTypeProvider_ServicesCorrectlyRegistered()
            {
                var typeProvider = A.Fake<ITypeProvider>();

                _serviceCollection.AddDeliveryClientFactory(
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
            public void AddDeliveryClientFactory_WithTwoClientsWithCustomTypeProvider_ServicesCorrectlyRegistered()
            {
                var typeProviderA = A.Fake<ITypeProvider>();
                var typeProviderB = A.Fake<ITypeProvider>();
                var clientAName = "Marketing";
                var clientBName = "Finance";
                var projectAID = "923850ac-5869-4743-8414-eb278e7beb69";
                var projectBID = "88d518c5-db60-432d-918a-14dba79c63ac";

                _serviceCollection.AddDeliveryClientFactory(
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
