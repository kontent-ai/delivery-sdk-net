using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;

namespace DeliverySDKWithAutofac
{
    public interface IMyDeliveryServiceProject1
    {
        public IDeliveryClient client { get; }
    }
}
