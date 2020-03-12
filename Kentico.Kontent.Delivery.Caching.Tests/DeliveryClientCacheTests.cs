using FakeItEasy;
using Kentico.Kontent.Delivery.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kentico.Kontent.Delivery.Caching.Tests
{
    public class DeliveryClientCacheTests
    {
        public DeliveryClientCacheTests()
        {
            A.Fake<IDeliveryClient>();
            A.Fake<IDeliveryCacheManager>();
        }
    }
}
