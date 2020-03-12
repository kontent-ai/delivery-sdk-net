using FakeItEasy;
using Microsoft.Extensions.Options;

namespace Kentico.Kontent.Delivery.Tests.Factories
{
    public static class DeliveryOptionsFactory
    { 
        public static IOptionsMonitor<DeliveryOptions> CreateMonitor(DeliveryOptions options)
        {
            var mock = A.Fake<IOptionsMonitor<DeliveryOptions>>();
            A.CallTo(() => mock.CurrentValue).Returns(options);
            return mock;
        }

        public static IOptions<DeliveryOptions> Create(DeliveryOptions options)
        {
            var mock = A.Fake<IOptions<DeliveryOptions>>();
            A.CallTo(() => mock.Value).Returns(options);
            return mock;
        }
    }
}
