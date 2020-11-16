using System;
using FakeItEasy;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.Factories
{
    public class DeliveryClientFactoryTests
    {
        private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptionsMock;
        private readonly IServiceProvider _serviceProvider;

        private const string _clientName = "ClientName";

        public DeliveryClientFactoryTests()
        {
            _deliveryOptionsMock = A.Fake<IOptionsMonitor<DeliveryOptions>>();
            _serviceProvider = new ServiceCollection().BuildServiceProvider();
        }

        [Fact]
        public void GetNamedClient_WithCorrectName_GetClient()
        {
            var deliveryOptions = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
            A.CallTo(() => _deliveryOptionsMock.Get(_clientName))
                .Returns(deliveryOptions);

            var deliveryClientFactory = new Delivery.DeliveryClientFactory(_deliveryOptionsMock, _serviceProvider);

            var result = deliveryClientFactory.Get(_clientName);

            result.Should().NotBeNull();
        }

        [Fact]
        public void GetNamedClient_WithWrongName_GetNull()
        {
            var deliveryOptions = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
            A.CallTo(() => _deliveryOptionsMock.Get(_clientName))
                .Returns(deliveryOptions);

            var deliveryClientFactory = new Delivery.DeliveryClientFactory(_deliveryOptionsMock, _serviceProvider);

            var result = deliveryClientFactory.Get("WrongName");

            result.Should().BeNull();
        }
    }
}
