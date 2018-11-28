using KenticoCloud.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KenticoCloud.Delivery.Tests.DependencyInjectionFrameworks
{
    public class CastleWindsorTests
    {
        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromCastleWindsorContainer()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .BuildWindsorCastleServiceProvider();

            var client = (DeliveryClient)provider.GetService<IDeliveryClient>();

            client.AssertDefaultDependencies();
        }

        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromCastleWindsorContainer_CustomCodeFirstModelProvider()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .RegisterInlineContentItemResolvers()
                .AddScoped<ICodeFirstModelProvider, FakeModelProvider>()
                .BuildWindsorCastleServiceProvider();

            var client = (DeliveryClient)provider.GetService<IDeliveryClient>();

            client.AssertDefaultDependenciesWithCodeFirstModelProviderAndInlineContentItemTypeResolvers<FakeModelProvider>();
        }
    }
}
