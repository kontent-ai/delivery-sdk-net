using KenticoCloud.Delivery.CodeFirst;
using KenticoCloud.Delivery.ContentLinks;
using KenticoCloud.Delivery.InlineContentItems;
using KenticoCloud.Delivery.ResiliencePolicy;
using Xunit;

namespace KenticoCloud.Delivery.Tests.DependencyInjectionFrameworks
{
    internal static class DeliveryClientAssertionExtensions
    {
        private const string ProjectId = "00a21be4-8fef-4dd9-9380-f4cbb82e260d";

        internal static void AssertDefaultDependencies(this DeliveryClient client)
            => client.AssertDefaultDependenciesWithCustomCodeFirstModelProvider<CodeFirstModelProvider>();

        internal static void AssertDefaultDependenciesWithCustomCodeFirstModelProvider<TCustomCodeFirstModelProvider>(this DeliveryClient client)
            where TCustomCodeFirstModelProvider : ICodeFirstModelProvider
        {
            Assert.IsType<DeliveryClient>(client);
            Assert.IsType<CodeFirstPropertyMapper>(client.CodeFirstPropertyMapper);
            Assert.IsType<DefaultTypeProvider>(client.CodeFirstTypeProvider);
            Assert.IsType<DefaultContentLinkUrlResolver>(client.ContentLinkUrlResolver);
            Assert.IsType<InlineContentItemsProcessor>(client.InlineContentItemsProcessor);
            Assert.IsType<DefaultResiliencePolicyProvider>(client.ResiliencePolicyProvider);
            Assert.IsType<DeliveryOptions>(client.DeliveryOptions);
            Assert.Equal(ProjectId, client.DeliveryOptions.ProjectId);
            Assert.IsType<TCustomCodeFirstModelProvider>(client.CodeFirstModelProvider);
        }
    }
}
