using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Kentico.Kontent.Delivery.ContentItems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.Tests.Integration.MultipleProjectRegistration
{
    public class DeliveryClientFactoryTests
    {
        [Fact]
        public void GetNamedClient_ProperTypeProviderIsSet_ReturnProperlyTypedModels()
        {
            var ClientAName = "ClientA";
            var ClientAProjectId = "4f8de8d2-4361-4ad9-99a4-28a7f493ca89";

            var ClientBName = "ClientB";
            var ClientBProjectId = "af0a9b6d-3934-4806-9b29-5898db3e0279";


            var host = Host.CreateDefaultBuilder(new string[] { })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    // Notes
                    // https://stackoverflow.com/questions/26126873/autofac-sub-dependencies-chain-registration

                    builder.RegisterType<ProjectAProvider>().Named<ITypeProvider>(ClientAName);
                    builder.RegisterType<ModelProvider>().Named<IModelProvider>(ClientAName)
                        .WithParameter(Autofac.Core.ResolvedParameter.ForNamed<ITypeProvider>(ClientAName));
                    builder.RegisterType<ProjectBProvider>().Named<ITypeProvider>(ClientBName);
                })
                .ConfigureServices((_, services) =>
                {
                    services.AddAutofac();

                    services.AddDeliveryClient(
                        ClientAName, builder =>
                        builder.WithProjectId(ClientAProjectId)
                            .UseProductionApi()
                            .Build(),
                        NamedServiceProviderType.Autofac);

                    services.AddDeliveryClient(
                        ClientBName, builder =>
                            builder.WithProjectId(ClientBProjectId)
                                .UseProductionApi()
                                .Build(),
                        NamedServiceProviderType.Autofac);
                })
                .Build();

            var deliveryClientFactory = host.Services.GetRequiredService<IDeliveryClientFactory>();

            // clientA does have set custom type provider under ModelProvider
            var clientA = deliveryClientFactory.Get(ClientAName);

            // direct client does have set custom type provider under ModelProvider
            var directClient = DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithProjectId(ClientAProjectId)
                    .UseProductionApi()
                    .Build())
            .WithTypeProvider(new ProjectAProvider())
            .Build();

        }
    }
}