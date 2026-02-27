using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.UsedIn;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.UsedIn;

public class DeliveryUsedInResponseTests
{
    [Fact]
    public void ExplicitInterface_Items_ReturnsSameList()
    {
        var sut = new DeliveryUsedInResponse
        {
            Items = []
        };

        IDeliveryUsedInResponse iface = sut;

        Assert.Empty(iface.Items);
    }
}
