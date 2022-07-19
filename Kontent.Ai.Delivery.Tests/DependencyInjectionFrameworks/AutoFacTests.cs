using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.DependencyInjectionFrameworks
{
    [Collection("DI Tests")]
    public class AutoFacTests
    {
        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromAutoFacContainer()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .RegisterInlineContentItemResolvers()
                .BuildAutoFacServiceProvider();

            var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

            client.AssertDefaultDependencies();
        }
        
        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromCastleWindsorContainer_CustomModelProvider()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .AddSingleton<IModelProvider, FakeModelProvider>()
                .BuildAutoFacServiceProvider();

            var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

            client.AssertDefaultDependenciesWithModelProviderAndInlineContentItemTypeResolvers<FakeModelProvider>();
        }
    }
}
