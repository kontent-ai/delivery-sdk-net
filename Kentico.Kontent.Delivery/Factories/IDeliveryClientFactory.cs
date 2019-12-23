using System;
using System.Collections.Generic;
using System.Text;

namespace Kentico.Kontent.Delivery.Factories
{
    public interface IDeliveryClientFactory
    {
        IDeliveryClient CreateDeliveryClient(string name);
    }
}
