using System;
using Newtonsoft.Json.Serialization;

namespace Kentico.Kontent.Delivery
{
    public class DeliveryContractResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = base.CreateContract(objectType);
            DeliveryServiceCollection sc = new DeliveryServiceCollection();
            var sp = sc.ServiceProvider;
            var s = sp.GetService(objectType);
            if (s != null)
            {
                contract = base.CreateObjectContract(s.GetType());
                contract.DefaultCreator = () => sp.GetService(objectType);
            }

            return contract;
        }
    }
}
