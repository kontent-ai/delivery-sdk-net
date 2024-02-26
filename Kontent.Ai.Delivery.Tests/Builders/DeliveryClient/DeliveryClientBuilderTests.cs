using System;
using System.Collections.Generic;
using FakeItEasy;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Builders.DeliveryClient;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.InlineContentItems;
using Kontent.Ai.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using Kontent.Ai.Delivery.Tests.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Builders.DeliveryClient
{
    public class DeliveryClientBuilderTests
    {
        private const string EnvironmentId = "e5629811-ddaa-4c2b-80d2-fa91e16bb264";
        private const string PreviewEndpoint = "https://preview-deliver.test.com/{0}";
        private const string PreviewApiKey =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJqdGkiOiI3YjJlM2FiOTBjOGM0ODVmYjdmZTczNWY0ZGM1NDIyMCIsImlhdCI6IjE1Mjg4ODc2MzQiLCJleHAiOiIxODc0NDg3NjM0IiwicHJvamVjd" +
            "F9pZCI6IjEyMzQ1Njc5OGFiY2RibGFibGEiLCJ2ZXIiOiIxLjAuMCIsImF1ZCI6InByZXZpZXcuZGVsaXZlci5rZW50aWNvY2xvdWQuY29tIn0.wtSzbNDpbEHR2Bj4LUTGsdgesg4b693TFuhRCRsDyoc";

        private readonly Guid _guid = new Guid(EnvironmentId);

        [Fact]
        public void BuildWithEnvironmentId_ReturnsDeliveryClientWithEnvironmentIdSet()
        {
            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder.WithEnvironmentId(EnvironmentId).Build();

            Assert.Equal(EnvironmentId, deliveryClient.DeliveryOptions.CurrentValue.EnvironmentId);
        }

        [Fact]
        public void BuildWithDeliveryOptions_ReturnsDeliveryClientWithDeliveryOptions()
        {
            var guid = new Guid(EnvironmentId);

            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(guid)
                    .UsePreviewApi(PreviewApiKey)
                    .WithCustomEndpoint(PreviewEndpoint)
                    .Build()
                ).Build();

            Assert.Equal(EnvironmentId, deliveryClient.DeliveryOptions.CurrentValue.EnvironmentId);
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
            var mockLoggerFactory = A.Fake<ILoggerFactory>();

            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder
                .WithEnvironmentId(EnvironmentId)
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
                .WithLoggerFactory(mockLoggerFactory)
                .Build();

            Assert.Equal(EnvironmentId, deliveryClient.DeliveryOptions.CurrentValue.EnvironmentId);
            Assert.Equal(mockModelProvider, deliveryClient.ModelProvider);
            Assert.Equal(mockRetryPolicyProvider, deliveryClient.RetryPolicyProvider);
            Assert.Equal(mockTypeProvider, deliveryClient.TypeProvider);
            Assert.Equal(mockDeliveryHttpClient, deliveryClient.DeliveryHttpClient);
            Assert.Equal(mockLoggerFactory, deliveryClient.LoggerFactory);
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
                .WithEnvironmentId(EnvironmentId)
                .WithInlineContentItemsResolver(mockDefaultInlineContentItemsResolver)
                .WithInlineContentItemsResolver(mockUnretrievedInlineContentItemsResolver)
                .WithInlineContentItemsResolver(mockCompleteContentItemsResolver)
                .Build();
            var actualResolvableInlineContentItemTypes = GetResolvableInlineContentItemTypes(deliveryClient);

            Assert.Equal(EnvironmentId, deliveryClient.DeliveryOptions.CurrentValue.EnvironmentId);
            Assert.Equal(expectedResolvableInlineContentItemsTypes, actualResolvableInlineContentItemTypes);
        }

        [Fact]
        public void BuildWithOptionalStepsAndCustomProvider_ReturnsDeliveryClientWithSetInstances()
        {
            var modelProvider = new FakeModelProvider();

            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder
                .WithEnvironmentId(EnvironmentId)
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
                .WithEnvironmentId(_guid)
                .Build();
            var actualResolvableInlineContentItemTypes = GetResolvableInlineContentItemTypes(deliveryClient);

            Assert.NotNull(deliveryClient.ModelProvider);
            Assert.NotNull(deliveryClient.TypeProvider);
            Assert.NotNull(deliveryClient.DeliveryHttpClient);
            Assert.NotNull(deliveryClient.RetryPolicyProvider);
            Assert.Equal(expectedResolvableInlineContentItemsTypes, actualResolvableInlineContentItemTypes);
            Assert.Equal(NullLoggerFactory.Instance, deliveryClient.LoggerFactory);
        }

        [Fact]
        public void BuildWithOptionsAndNullHttpClient_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithEnvironmentId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithDeliveryHttpClient(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullContentLinUrlResolver_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithEnvironmentId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithContentLinkUrlResolver(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullModelProvider_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithEnvironmentId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithModelProvider(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullInlineContentItemsResolver_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithEnvironmentId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithInlineContentItemsResolver<object>(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullInlineContentItemsProcessor_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithEnvironmentId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithInlineContentItemsProcessor(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullTypeProvider_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithEnvironmentId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithTypeProvider(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullResiliencePolicyProvider_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithEnvironmentId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithRetryPolicyProvider(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullPropertyMapper_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithEnvironmentId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithPropertyMapper(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullLoggerFactory_TrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithEnvironmentId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithLoggerFactory(null));
        }

        private static IEnumerable<Type> GetResolvableInlineContentItemTypes(Delivery.DeliveryClient deliveryClient)
            => (((ModelProvider)deliveryClient.ModelProvider).InlineContentItemsProcessor as InlineContentItemsProcessor).ContentItemResolvers.Keys;
    }
}
