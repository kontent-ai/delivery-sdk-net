using Autofac;
using FakeItEasy;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.Tests
{
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
