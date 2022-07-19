using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery
{
    internal sealed class DeliveryJsonSerializer : JsonSerializer
    {
        public DeliveryJsonSerializer()
        {
            ContractResolver = new DeliveryContractResolver(new DeliveryServiceCollection().ServiceProvider);
        }
    }
}
