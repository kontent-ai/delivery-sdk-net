﻿using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.Configuration;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;
using Kentico.Kontent.Delivery.ContentItems;
using Kentico.Kontent.Delivery.ContentItems.ContentLinks;
using Kentico.Kontent.Delivery.ContentItems.InlineContentItems;
using Kentico.Kontent.Delivery.RetryPolicy;
using Kentico.Kontent.Delivery.Tests.Models.ContentTypes;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks
{
    internal static class DeliveryClientAssertionExtensions
    {
        private const string ProjectId = "00a21be4-8fef-4dd9-9380-f4cbb82e260d";

        internal static void AssertDefaultDependencies(this DeliveryClient client)
        {
            client.AssertInlineContentItemTypesWithResolver().AssertDefaultDependenciesWithCustomModelProvider<ModelProvider>();
            Assert.IsType<PropertyMapper>(((ModelProvider)client.ModelProvider).PropertyMapper);
            Assert.IsType<DefaultContentLinkUrlResolver>(((ModelProvider)client.ModelProvider).ContentLinkUrlResolver);
            Assert.IsType<InlineContentItemsProcessor>(((ModelProvider)client.ModelProvider).InlineContentItemsProcessor);
        }

        internal static void AssertDefaultDependenciesWithModelProviderAndInlineContentItemTypeResolvers<TCustomModelProvider>(
            this DeliveryClient client)
            where TCustomModelProvider : IModelProvider
            => client
                .AssertDefaultDependenciesWithCustomModelProvider<TCustomModelProvider>();

        private static void AssertDefaultDependenciesWithCustomModelProvider<TCustomModelProvider>(this DeliveryClient client)
            where TCustomModelProvider : IModelProvider
        {
            Assert.IsType<DeliveryClient>(client);
            Assert.Equal(ProjectId, client.DeliveryOptions.CurrentValue.ProjectId);
            Assert.IsType<TypeProvider>(client.TypeProvider);
            Assert.IsType<DefaultRetryPolicyProvider>(client.RetryPolicyProvider);
            Assert.IsType<DeliveryOptions>(client.DeliveryOptions.CurrentValue);
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
            var inlineContentItemsProcessor = ((ModelProvider)client.ModelProvider).InlineContentItemsProcessor as InlineContentItemsProcessor;

            var actualInlineContentItemTypesWithResolver = inlineContentItemsProcessor?.ContentItemResolvers?.Keys.ToArray();

            Assert.Equal(expectedInlineContentItemTypesWithResolver, actualInlineContentItemTypesWithResolver);

            return client;
        }
    }
}
