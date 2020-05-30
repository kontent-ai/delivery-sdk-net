using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.StrongTyping;
using Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks
{
    [Collection("DI Tests")]
    public class SimpleInjectorTests
    {
        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromSimpleInjectorContainer()
        {
            var container = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .RegisterInlineContentItemResolvers()
                .BuildSimpleInjectorServiceProvider();

            var client = (DeliveryClient) container.GetInstance<IDeliveryClient>();

            client.AssertDefaultDependencies();
        }

        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromSimpleInjectorContainer_CustomModelProvider()
        {
            var container = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .AddSingleton<IModelProvider, FakeModelProvider>()
                .BuildSimpleInjectorServiceProvider();

            var client = (DeliveryClient) container.GetInstance<IDeliveryClient>();

            client.AssertDefaultDependenciesWithModelProviderAndInlineContentItemTypeResolvers<FakeModelProvider>();
        }

        [Fact]
        public void FakeModelProviderIsSuccessfullyResolvedAfterCrossWireWithServiceCollection()
        {
            var container = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .BuildSimpleInjectorServiceProvider();
            container.Register<IModelProvider, FakeModelProvider>();

            var resolvedService = container.GetRequiredService<IModelProvider>();

            Assert.IsType<FakeModelProvider>(resolvedService);
        }
    }
}
