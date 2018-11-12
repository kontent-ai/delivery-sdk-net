using KenticoCloud.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KenticoCloud.Delivery.Tests.DependencyInjectionFrameworks
{
    public class SimpleInjectorTests
    {
        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromSimpleInjectorContainer()
        {
            var container = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .BuildSimpleInjectorServiceProvider();

            var client = (DeliveryClient) container.GetInstance<IDeliveryClient>();

            client.AssertDefaultDependencies();
        }

        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromSimpleInjectorContainer_CustomCodeFirstModelProvider()
        {
            var container = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .AddScoped<ICodeFirstModelProvider, FakeModelProvider>()
                .BuildSimpleInjectorServiceProvider();

            var client = (DeliveryClient) container.GetInstance<IDeliveryClient>();

            client.AssertDefaultDependenciesWithCustomCodeFirstModelProvider<FakeModelProvider>();
        }

        [Fact]
        public void FakeModelProviderIsSuccessfullyResolvedAfterCrossWireWithServiceCollection()
        {
            var container = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .BuildSimpleInjectorServiceProvider();
            container.Register<ICodeFirstModelProvider, FakeModelProvider>();

            var resolvedService = container.GetService<ICodeFirstModelProvider>();

            Assert.IsType<FakeModelProvider>(resolvedService);
        }
    }
}
