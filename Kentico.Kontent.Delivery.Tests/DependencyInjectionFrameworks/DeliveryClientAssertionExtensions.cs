using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.StrongTyping;
using Kentico.Kontent.Delivery.ContentLinks;
using Kentico.Kontent.Delivery.InlineContentItems;
using Kentico.Kontent.Delivery.RetryPolicy;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks
{
    internal static class DeliveryClientAssertionExtensions
    {
        private const string ProjectId = "00a21be4-8fef-4dd9-9380-f4cbb82e260d";

        internal static void AssertDefaultDependencies(this DeliveryClient client)
            => client
                .AssertDefaultDependenciesWithCustomModelProvider<ModelProvider>();

        internal static void AssertDefaultDependenciesWithModelProviderAndInlineContentItemTypeResolvers<TCustomModelProvider>(
            this DeliveryClient client)
            where TCustomModelProvider : IModelProvider
            => client
                .AssertInlineContentItemTypesWithResolver()
                .AssertDefaultDependenciesWithCustomModelProvider<TCustomModelProvider>();

        private static void AssertDefaultDependenciesWithCustomModelProvider<TCustomModelProvider>(this DeliveryClient client)
            where TCustomModelProvider : IModelProvider
        {
            Assert.IsType<DeliveryClient>(client);
            Assert.Equal(ProjectId, client.DeliveryOptions?.ProjectId);
            Assert.IsType<PropertyMapper>(client.PropertyMapper);
            Assert.IsType<TypeProvider>(client.TypeProvider);
            Assert.IsType<DefaultContentLinkUrlResolver>(client.ContentLinkUrlResolver);
            Assert.IsType<InlineContentItemsProcessor>(client.InlineContentItemsProcessor);
            Assert.IsType<DefaultRetryPolicyProvider>(client.RetryPolicyProvider);
            Assert.IsType<DeliveryOptions>(client.DeliveryOptions);
            Assert.IsType<TCustomModelProvider>(client.ModelProvider);
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

            var actualInlineContentItemTypesWithResolver = inlineContentItemsProcessor?.ContentItemResolvers?.Keys.ToArray();

            Assert.Equal(expectedInlineContentItemTypesWithResolver, actualInlineContentItemTypesWithResolver);

            return client;
        }
    }
}
