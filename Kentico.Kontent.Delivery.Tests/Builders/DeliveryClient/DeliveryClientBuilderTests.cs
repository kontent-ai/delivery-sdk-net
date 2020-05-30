using System;
using System.Collections.Generic;
using FakeItEasy;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentLinks;
using Kentico.Kontent.Delivery.Abstractions.InlineContentItems;
using Kentico.Kontent.Delivery.Abstractions.RetryPolicy;
using Kentico.Kontent.Delivery.Abstractions.StrongTyping;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Kentico.Kontent.Delivery.InlineContentItems;
using Kentico.Kontent.Delivery.StrongTyping;
using Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using Kentico.Kontent.Delivery.Tests.Models;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.Builders.DeliveryClient
{
    public class DeliveryClientBuilderTests
    {
        private const string ProjectId = "e5629811-ddaa-4c2b-80d2-fa91e16bb264";
        private const string PreviewEndpoint = "https://preview-deliver.test.com/{0}";
        private const string PreviewApiKey =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJqdGkiOiI3YjJlM2FiOTBjOGM0ODVmYjdmZTczNWY0ZGM1NDIyMCIsImlhdCI6IjE1Mjg4ODc2MzQiLCJleHAiOiIxODc0NDg3NjM0IiwicHJvamVjd" +
            "F9pZCI6IjEyMzQ1Njc5OGFiY2RibGFibGEiLCJ2ZXIiOiIxLjAuMCIsImF1ZCI6InByZXZpZXcuZGVsaXZlci5rZW50aWNvY2xvdWQuY29tIn0.wtSzbNDpbEHR2Bj4LUTGsdgesg4b693TFuhRCRsDyoc";

        private readonly Guid _guid = new Guid(ProjectId);

        [Fact]
        public void BuildWithProjectId_ReturnsDeliveryClientWithProjectIdSet()
        {
            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder.WithProjectId(ProjectId).Build();

            Assert.Equal(ProjectId, deliveryClient.DeliveryOptions.CurrentValue.ProjectId);
        }

        [Fact]
        public void BuildWithDeliveryOptions_ReturnsDeliveryClientWithDeliveryOptions()
        {
            var guid = new Guid(ProjectId);

            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithProjectId(guid)
                    .UsePreviewApi(PreviewApiKey)
                    .WithCustomEndpoint(PreviewEndpoint)
                    .Build()
                ).Build();

            Assert.Equal(ProjectId, deliveryClient.DeliveryOptions.CurrentValue.ProjectId);
            Assert.True(deliveryClient.DeliveryOptions.CurrentValue.UsePreviewApi);
            Assert.Equal(PreviewEndpoint, deliveryClient.DeliveryOptions.CurrentValue.PreviewEndpoint);
        }

        [Fact]
        public void BuildWithOptionalSteps_ReturnsDeliveryClientWithSetInstances()
        {
            var mockModelProvider = A.Fake<IModelProvider>();
            var mockRetryPolicyProvider = A.Fake<IRetryPolicyProvider>();
            var mockPropertyMapper = A.Fake<IPropertyMapper>();
            var mockContentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var mockInlineContentItemsProcessor = A.Fake<IInlineContentItemsProcessor>();
            var mockDefaultInlineContentItemsResolver = A.Fake<IInlineContentItemsResolver<object>>();
            var mockUnretrievedInlineContentItemsResolver = A.Fake<IInlineContentItemsResolver<UnretrievedContentItem>>();
            var mockAnContentItemsResolver = A.Fake<IInlineContentItemsResolver<CompleteContentItemModel>>();
            var mockTypeProvider = A.Fake<ITypeProvider>();
            var mockDeliveryHttpClient = new DeliveryHttpClient(new MockHttpMessageHandler().ToHttpClient());

            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder
                .WithProjectId(ProjectId)
                .WithDeliveryHttpClient(mockDeliveryHttpClient)
                .WithContentLinkUrlResolver(mockContentLinkUrlResolver)
                .WithInlineContentItemsProcessor(mockInlineContentItemsProcessor)
                .WithInlineContentItemsResolver(mockDefaultInlineContentItemsResolver)
                .WithInlineContentItemsResolver(mockUnretrievedInlineContentItemsResolver)
                .WithInlineContentItemsResolver(mockAnContentItemsResolver)
                .WithModelProvider(mockModelProvider)
                .WithPropertyMapper(mockPropertyMapper)
                .WithRetryPolicyProvider(mockRetryPolicyProvider)
                .WithTypeProvider(mockTypeProvider)
                .Build();

            Assert.Equal(ProjectId, deliveryClient.DeliveryOptions.CurrentValue.ProjectId);
            Assert.Equal(mockModelProvider, deliveryClient.ModelProvider);
            Assert.Equal(mockRetryPolicyProvider, deliveryClient.RetryPolicyProvider);
            Assert.Equal(mockTypeProvider, deliveryClient.TypeProvider);
            Assert.Equal(mockDeliveryHttpClient, deliveryClient.DeliveryHttpClient);
        }

        [Fact]
        public void BuildWithOptionalStepsWithCustomResolvers_ReturnsDeliveryClientWithSetInstances()
        {
            var mockDefaultInlineContentItemsResolver = A.Fake<IInlineContentItemsResolver<object>>();
            var mockUnretrievedInlineContentItemsResolver = A.Fake<IInlineContentItemsResolver<UnretrievedContentItem>>();
            var mockCompleteContentItemsResolver = A.Fake<IInlineContentItemsResolver<CompleteContentItemModel>>();
            var expectedResolvableInlineContentItemsTypes = new[]
            {
                typeof(object),
                typeof(UnretrievedContentItem),
                typeof(CompleteContentItemModel),
                typeof(UnknownContentItem)
            };

            var deliveryClient = (Delivery.DeliveryClient)DeliveryClientBuilder
                .WithProjectId(ProjectId)
                .WithInlineContentItemsResolver(mockDefaultInlineContentItemsResolver)
                .WithInlineContentItemsResolver(mockUnretrievedInlineContentItemsResolver)
                .WithInlineContentItemsResolver(mockCompleteContentItemsResolver)
                .Build();
            var actualResolvableInlineContentItemTypes = GetResolvableInlineContentItemTypes(deliveryClient);

            Assert.Equal(ProjectId, deliveryClient.DeliveryOptions.CurrentValue.ProjectId);
            Assert.Equal(expectedResolvableInlineContentItemsTypes, actualResolvableInlineContentItemTypes);
        }

        [Fact]
        public void BuildWithOptionalStepsAndCustomProvider_ReturnsDeliveryClientWithSetInstances()
        {
            var modelProvider = new FakeModelProvider();

            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder
                .WithProjectId(ProjectId)
                .WithModelProvider(modelProvider)
                .Build();

            Assert.Equal(modelProvider, deliveryClient.ModelProvider);
        }

        [Fact]
        public void BuildWithoutOptionalSteps_ReturnsDeliveryClientWithDefaultImplementations()
        {
            var expectedResolvableInlineContentItemsTypes = new[]
            {
                typeof(object),
                typeof(UnretrievedContentItem),
                typeof(UnknownContentItem)
            };

            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder
                .WithProjectId(_guid)
                .Build();
            var actualResolvableInlineContentItemTypes = GetResolvableInlineContentItemTypes(deliveryClient);

            Assert.NotNull(deliveryClient.ModelProvider);
            Assert.NotNull(deliveryClient.TypeProvider);
            Assert.NotNull(deliveryClient.DeliveryHttpClient);
            Assert.NotNull(deliveryClient.RetryPolicyProvider);
            Assert.Equal(expectedResolvableInlineContentItemsTypes, actualResolvableInlineContentItemTypes);
        }

        [Fact]
        public void BuildWithOptionsAndNullHttpClient_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithDeliveryHttpClient(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullContentLinUrlResolver_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithContentLinkUrlResolver(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullModelProvider_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithModelProvider(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullInlineContentItemsResolver_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithInlineContentItemsResolver<object>(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullInlineContentItemsProcessor_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithInlineContentItemsProcessor(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullTypeProvider_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithTypeProvider(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullResiliencePolicyProvider_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithRetryPolicyProvider(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullPropertyMapper_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithPropertyMapper(null));
        }

        private static IEnumerable<Type> GetResolvableInlineContentItemTypes(Delivery.DeliveryClient deliveryClient)
            => (((ModelProvider)deliveryClient.ModelProvider).InlineContentItemsProcessor as InlineContentItemsProcessor).ContentItemResolvers.Keys;
    }
}
