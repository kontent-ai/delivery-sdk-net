using Newtonsoft.Json;

namespace Kontent.Ai.Delivery
{
    internal sealed class DeliveryJsonSerializer : JsonSerializer
    {
        public DeliveryJsonSerializer()
        {
            ContractResolver = new DeliveryContractResolver(new DeliveryServiceCollection().ServiceProvider);
        }
    }
}
