using System;
using Autofac;
using FakeItEasy;
using FluentAssertions;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kontent.Ai.Delivery.Extensions.DependencyInjection.Tests
{
    [Obsolete("#312")]
    public class DeliveryClientCacheFactoryTests
    {
        private readonly IOptionsMonitor<DeliveryCacheOptions> _deliveryCacheOptionsMock;
        private readonly IDeliveryClientFactory _innerDeliveryClientFactoryMock;
        private readonly INamedServiceProvider _autofacServiceProvider;
        private readonly IServiceCollection _serviceCollection;

        private const string _clientName = "ClientName";

        public DeliveryClientCacheFactoryTests()
        {
            _deliveryCacheOptionsMock = A.Fake<IOptionsMonitor<DeliveryCacheOptions>>();
            _innerDeliveryClientFactoryMock = A.Fake<IDeliveryClientFactory>();
            _autofacServiceProvider = A.Fake<INamedServiceProvider>();
            _serviceCollection = new ServiceCollection()
                .AddMemoryCache();
        }

        [Fact]
        public void GetNamedCacheClient_WithCorrectName_GetClient()
        {
            var deliveryCacheOptions = new DeliveryCacheOptions();
            A.CallTo(() => _deliveryCacheOptionsMock.Get(_clientName))
                .Returns(deliveryCacheOptions);

            var deliveryClientFactory = new NamedDeliveryClientCacheFactory(_innerDeliveryClientFactoryMock, _deliveryCacheOptionsMock, _serviceCollection.BuildServiceProvider(), _autofacServiceProvider);

            var result = deliveryClientFactory.Get(_clientName);

            result.Should().NotBeNull();
        }

        [Fact]
        public void GetNamedCacheClient_WithWrongName_GetNull()
        {
            var deliveryCacheOptions = new DeliveryCacheOptions();
            A.CallTo(() => _deliveryCacheOptionsMock.Get(_clientName))
                .Returns(deliveryCacheOptions);

            var deliveryClientFactory = new NamedDeliveryClientCacheFactory(_innerDeliveryClientFactoryMock, _deliveryCacheOptionsMock, _serviceCollection.BuildServiceProvider(), _autofacServiceProvider);

            var result = deliveryClientFactory.Get("WrongName");

            result.Should().NotBeNull();
        }
    }
}
