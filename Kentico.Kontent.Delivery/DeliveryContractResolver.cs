using System;
using Newtonsoft.Json.Serialization;

namespace Kentico.Kontent.Delivery
{
    public class DeliveryContractResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            JsonContract contract = null;
            if (objectType.IsInterface)
            {
                var collection = new DeliveryServiceCollection();
                var serviceProvider = collection.ServiceProvider;
                var service = serviceProvider.GetService(objectType);
                if (service != null)
                {
                    contract = base.CreateObjectContract(service.GetType());
                    contract.DefaultCreator = () => serviceProvider.GetService(objectType);
                }
            }
            return contract ?? base.CreateContract(objectType);
        }
    }
}
