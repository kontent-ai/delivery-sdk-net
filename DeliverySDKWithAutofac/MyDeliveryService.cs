using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;

namespace DeliverySDKWithAutofac
{
    public class MyDeliveryService : IMyDeliveryServiceProject1, IMyDeliveryServiceProject2
    {
        public IDeliveryClient client { get; }

        public MyDeliveryService(string projectId, ITypeProvider typeProvider)
        {
            this.client = DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithProjectId(projectId)
                    .UseProductionApi()
                .   Build()
                )
                .WithTypeProvider(typeProvider)
                .Build();
        }
    }
}
