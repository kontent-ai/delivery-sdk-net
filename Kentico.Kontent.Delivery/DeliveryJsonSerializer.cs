using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery
{
    internal class DeliveryJsonSerializer : JsonSerializer
    {
        public DeliveryJsonSerializer()
        {
            ContractResolver = new DeliveryContractResolver(new DeliveryServiceCollection().ServiceProvider);
        }
    }
}
