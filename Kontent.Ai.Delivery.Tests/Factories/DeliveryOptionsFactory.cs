using System;
using FakeItEasy;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.Tests.Factories
{
    public static class DeliveryOptionsFactory
    { 
        public static IOptionsMonitor<DeliveryOptions> CreateMonitor(DeliveryOptions options)
        {
            var mock = A.Fake<IOptionsMonitor<DeliveryOptions>>();
            A.CallTo(() => mock.CurrentValue).Returns(options);
            return mock;
        }
        
        public static IOptionsMonitor<DeliveryOptions> CreateMonitor(Guid projectId)
        {
            return CreateMonitor(new DeliveryOptions { ProjectId = projectId.ToString() });
        }

        public static IOptions<DeliveryOptions> Create(DeliveryOptions options)
        {
            var mock = A.Fake<IOptions<DeliveryOptions>>();
            A.CallTo(() => mock.Value).Returns(options);
            return mock;
        }
    }
}
