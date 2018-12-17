using KenticoCloud.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KenticoCloud.Delivery.Tests.DependencyInjectionFrameworks
{
    [Collection("DI Tests")]
    public class UnityTests
    {
        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromUnityContainer()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .BuildUnityServiceProvider();

            var client = (DeliveryClient)provider.GetService<IDeliveryClient>();

            client.AssertDefaultDependencies();
        }

        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromUnityContainer_CustomCodeFirstModelProvider()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .RegisterInlineContentItemResolvers()
                .AddScoped<ICodeFirstModelProvider, FakeModelProvider>()
                .BuildUnityServiceProvider();

            var client = (DeliveryClient)provider.GetService<IDeliveryClient>();

            client.AssertDefaultDependenciesWithCodeFirstModelProviderAndInlineContentItemTypeResolvers<FakeModelProvider>();
        }
    }
}
