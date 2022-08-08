using System;
using FluentAssertions;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Kontent.Ai.Delivery.Extensions;

namespace Kontent.Ai.Delivery.Tests.Factories
{
    [Obsolete("#312")]
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

            Assert.Throws<NotImplementedException>(() => deliveryClientFactory.Get("clientName"));
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
