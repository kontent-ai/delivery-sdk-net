using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Kontent.Delivery
{
    public class DeliveryServiceCollection
    {
        public ServiceProvider ServiceProvider;

        public DeliveryServiceCollection()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => {
                
                scan.FromAssemblyOf<DeliveryClient>().AddClasses(false).AsImplementedInterfaces();

            });

            ServiceProvider = collection.BuildServiceProvider();
        }
    }
}
