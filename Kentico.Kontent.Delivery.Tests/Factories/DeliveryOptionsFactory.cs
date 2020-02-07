using FakeItEasy;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kentico.Kontent.Delivery.Tests.Factories
{
    public static class DeliveryOptionsFactory
    { 
        public static IOptionsSnapshot<DeliveryOptions> Create(DeliveryOptions options)
        {
            var mock = A.Fake<IOptionsSnapshot<DeliveryOptions>>();
            A.CallTo(() => mock.Value).Returns(options);
            return mock;
        }
    }
}
