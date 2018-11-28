using KenticoCloud.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KenticoCloud.Delivery.Tests.DependencyInjectionFrameworks
{
    public class AutoFacTests
    {
        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromAutoFacContainer()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .BuildAutoFacServiceProvider();

            var client = (DeliveryClient)provider.GetService<IDeliveryClient>();

            client.AssertDefaultDependencies();
        }
        
        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromCastleWindsorContainer_CustomCodeFirstModelProvider()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .AddScoped<ICodeFirstModelProvider, FakeModelProvider>()
                .RegisterInlineContentItemResolvers()
                .BuildAutoFacServiceProvider();

            var client = (DeliveryClient)provider.GetService<IDeliveryClient>();

            client.AssertDefaultDependenciesWithCodeFirstModelProviderAndInlineContentItemTypeResolvers<FakeModelProvider>();
        }
    }
}
