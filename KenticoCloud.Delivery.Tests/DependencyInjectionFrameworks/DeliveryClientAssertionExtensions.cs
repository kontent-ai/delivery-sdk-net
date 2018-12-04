using System.Linq;
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
            => client
                .AssertDefaultDependenciesWithCustomCodeFirstModelProvider<CodeFirstModelProvider>();

        internal static void AssertDefaultDependenciesWithCodeFirstModelProviderAndInlineContentItemTypeResolvers<TCustomCodeFirstModelProvider>(
            this DeliveryClient client)
            where TCustomCodeFirstModelProvider : ICodeFirstModelProvider
            => client
                .AssertInlineContentItemTypesWithResolver()
                .AssertDefaultDependenciesWithCustomCodeFirstModelProvider<TCustomCodeFirstModelProvider>();

        private static void AssertDefaultDependenciesWithCustomCodeFirstModelProvider<TCustomCodeFirstModelProvider>(this DeliveryClient client)
            where TCustomCodeFirstModelProvider : ICodeFirstModelProvider
        {
            Assert.IsType<DeliveryClient>(client);
            Assert.Equal(ProjectId, client.DeliveryOptions?.ProjectId);
            Assert.IsType<CodeFirstPropertyMapper>(client.CodeFirstPropertyMapper);
            Assert.IsType<CodeFirstTypeProvider>(client.CodeFirstTypeProvider);
            Assert.IsType<DefaultContentLinkUrlResolver>(client.ContentLinkUrlResolver);
            Assert.IsType<InlineContentItemsProcessor>(client.InlineContentItemsProcessor);
            Assert.IsType<DefaultResiliencePolicyProvider>(client.ResiliencePolicyProvider);
            Assert.IsType<DeliveryOptions>(client.DeliveryOptions);
            Assert.IsType<TCustomCodeFirstModelProvider>(client.CodeFirstModelProvider);
        }

        internal static DeliveryClient AssertInlineContentItemTypesWithResolver(this DeliveryClient client)
        {
            var expectedInlineContentItemTypesWithResolver = new[]
            {
                typeof(object),
                typeof(UnretrievedContentItem),
                typeof(UnknownContentItem),
                typeof(HostedVideo),
                typeof(Tweet)
            };
            var inlineContentItemsProcessor = client.InlineContentItemsProcessor as InlineContentItemsProcessor;

            var actualInlineContentItemTypesWithResolver = inlineContentItemsProcessor?.ContentItemTypesWithResolver?.ToArray();

            Assert.Equal(expectedInlineContentItemTypesWithResolver, actualInlineContentItemTypesWithResolver);

            return client;
        }
    }
}
