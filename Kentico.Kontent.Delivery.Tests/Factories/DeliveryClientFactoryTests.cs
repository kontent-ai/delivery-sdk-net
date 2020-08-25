using System;
using FakeItEasy;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.Factories
{
    public class DeliveryClientFactoryTests
    {
        private readonly IOptions<DeliveryOptions> _deliveryOptionsMock;
        private readonly IOptionsMonitor<DeliveryClientFactoryOptions> _deliveryClientFactoryOptionsMock;
        private readonly IServiceProvider _serviceProvider;

        private const string _clientName = "ClientName";

        public DeliveryClientFactoryTests()
        {
            _deliveryOptionsMock = A.Fake<IOptions<DeliveryOptions>>();
            _deliveryClientFactoryOptionsMock = A.Fake<IOptionsMonitor<DeliveryClientFactoryOptions>>();
            _serviceProvider = new ServiceCollection().BuildServiceProvider();
        }

        [Fact]
        public void GetNamedClient_WithCorrectName_GetClient()
        {
            var deliveryClientFactoryOptions = new DeliveryClientFactoryOptions();
            deliveryClientFactoryOptions.DeliveryClientsOptions.Add(() => _deliveryOptionsMock.Value);

            A.CallTo(() => _deliveryClientFactoryOptionsMock.Get(_clientName))
                .Returns(deliveryClientFactoryOptions);

            var deliveryClientFactory = new Delivery.DeliveryClientFactory(_deliveryClientFactoryOptionsMock, _serviceProvider);

            var result = deliveryClientFactory.Get(_clientName);

            result.Should().NotBeNull();
        }

        [Fact]
        public void GetNamedClient_WithWrongName_GetNull()
        {
            var deliveryClientFactoryOptions = new DeliveryClientFactoryOptions();
            deliveryClientFactoryOptions.DeliveryClientsOptions.Add(() => _deliveryOptionsMock.Value);

            A.CallTo(() => _deliveryClientFactoryOptionsMock.Get(_clientName))
                .Returns(deliveryClientFactoryOptions);

            var deliveryClientFactory = new Delivery.DeliveryClientFactory(_deliveryClientFactoryOptionsMock, _serviceProvider);

            var result = deliveryClientFactory.Get("WrongName");

            result.Should().BeNull();
        }
    }
}
