using FakeItEasy;
using Kentico.Kontent.Delivery.Abstractions;

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
