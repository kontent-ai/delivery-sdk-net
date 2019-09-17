using System;
using System.Collections.Generic;
using FakeItEasy;
using KenticoKontent.Delivery.InlineContentItems;
using KenticoKontent.Delivery.ResiliencePolicy;
using KenticoKontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using RichardSzalay.MockHttp;
using Xunit;

namespace KenticoKontent.Delivery.Tests.Builders.DeliveryClient
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

            Assert.Equal(ProjectId, deliveryClient.DeliveryOptions.ProjectId);
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

            Assert.Equal(ProjectId, deliveryClient.DeliveryOptions.ProjectId);
            Assert.True(deliveryClient.DeliveryOptions.UsePreviewApi);
            Assert.Equal(PreviewEndpoint, deliveryClient.DeliveryOptions.PreviewEndpoint);
        }

        [Fact]
        public void BuildWithOptionalSteps_ReturnsDeliveryClientWithSetInstances()
        {
            var mockModelProvider = A.Fake<IModelProvider>();
            var mockResiliencePolicyProvider = A.Fake<IResiliencePolicyProvider>();
            var mockPropertyMapper = A.Fake<IPropertyMapper>();
            var mockContentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var mockInlineContentItemsProcessor = A.Fake<IInlineContentItemsProcessor>();
            var mockDefaultInlineContentItemsResolver = A.Fake<IInlineContentItemsResolver<object>>();
            var mockUnretrievedInlineContentItemsResolver = A.Fake<IInlineContentItemsResolver<UnretrievedContentItem>>();
            var mockAnContentItemsResolver = A.Fake<IInlineContentItemsResolver<CompleteContentItemModel>>();
            var mockTypeProvider = A.Fake<ITypeProvider>();
            var mockHttp = new MockHttpMessageHandler().ToHttpClient();            

            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder
                .WithProjectId(ProjectId)
                .WithHttpClient(mockHttp)
                .WithContentLinkUrlResolver(mockContentLinkUrlResolver)
                .WithInlineContentItemsProcessor(mockInlineContentItemsProcessor)
                .WithInlineContentItemsResolver(mockDefaultInlineContentItemsResolver)
                .WithInlineContentItemsResolver(mockUnretrievedInlineContentItemsResolver)
                .WithInlineContentItemsResolver(mockAnContentItemsResolver)
                .WithModelProvider(mockModelProvider)
                .WithPropertyMapper(mockPropertyMapper)
                .WithResiliencePolicyProvider(mockResiliencePolicyProvider)
                .WithTypeProvider(mockTypeProvider)
                .Build();

            Assert.Equal(ProjectId, deliveryClient.DeliveryOptions.ProjectId);
            Assert.Equal(mockContentLinkUrlResolver, deliveryClient.ContentLinkUrlResolver);            
            Assert.Equal(mockInlineContentItemsProcessor, deliveryClient.InlineContentItemsProcessor);
            Assert.Equal(mockModelProvider, deliveryClient.ModelProvider);
            Assert.Equal(mockPropertyMapper, deliveryClient.PropertyMapper);
            Assert.Equal(mockResiliencePolicyProvider, deliveryClient.ResiliencePolicyProvider);
            Assert.Equal(mockTypeProvider, deliveryClient.TypeProvider);
            Assert.Equal(mockHttp, deliveryClient.HttpClient);
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

            Assert.Equal(ProjectId, deliveryClient.DeliveryOptions.ProjectId);
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
        public void BuildWithoutOptionalStepts_ReturnsDeliveryClientWithDefaultImplementations()
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
            Assert.NotNull(deliveryClient.PropertyMapper);
            Assert.NotNull(deliveryClient.TypeProvider);
            Assert.NotNull(deliveryClient.ContentLinkUrlResolver);
            Assert.NotNull(deliveryClient.HttpClient);
            Assert.NotNull(deliveryClient.InlineContentItemsProcessor);
            Assert.NotNull(deliveryClient.ResiliencePolicyProvider);
            Assert.Equal(expectedResolvableInlineContentItemsTypes, actualResolvableInlineContentItemTypes);
        }

        [Fact]
        public void BuildWithOptionsAndNullHttpClient_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithHttpClient(null));
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

            Assert.Throws<ArgumentNullException>(() => builderStep.WithResiliencePolicyProvider(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullPropertyMapper_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithPropertyMapper(null));
        }

        private static IEnumerable<Type> GetResolvableInlineContentItemTypes(Delivery.DeliveryClient deliveryClient)
            => (deliveryClient.InlineContentItemsProcessor as InlineContentItemsProcessor)?.ContentItemResolvers.Keys;
    }
}
