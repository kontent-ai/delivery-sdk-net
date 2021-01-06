using System;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Kentico.Kontent.Delivery.Extensions;

namespace Kentico.Kontent.Delivery.Tests.Factories
{
    public class DeliveryClientFactoryTests
    {
        private readonly ServiceCollection _serviceCollection;

        public DeliveryClientFactoryTests()
        {
            _serviceCollection = new ServiceCollection();
        }

        [Fact]
        public void GetNamedClient_GetNull()
        {
            var deliveryClientFactory = new Delivery.DeliveryClientFactory(_serviceCollection.BuildServiceProvider());

            var result = deliveryClientFactory.Get("clientName");

            result.Should().BeNull();
        }

        [Fact]
        public void GetClient_GetClient()
        {
            _serviceCollection.AddDeliveryClient(new DeliveryOptions
            {
                ProjectId = Guid.NewGuid().ToString()
            });
            var deliveryClientFactory = new Delivery.DeliveryClientFactory(_serviceCollection.BuildServiceProvider());

            var result = deliveryClientFactory.Get();

            result.Should().NotBeNull();
        }
    }
}
