using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.StrongTyping;
using Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks
{
    [Collection("DI Tests")]
    public class CastleWindsorTests
    {
        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromCastleWindsorContainer()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .RegisterInlineContentItemResolvers()
                .BuildWindsorCastleServiceProvider();

            var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

            client.AssertDefaultDependencies();
        }

        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromCastleWindsorContainer_CustomModelProvider()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .AddSingleton<IModelProvider, FakeModelProvider>()
                .BuildWindsorCastleServiceProvider();

            var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

            client.AssertDefaultDependenciesWithModelProviderAndInlineContentItemTypeResolvers<FakeModelProvider>();
        }
    }
}
