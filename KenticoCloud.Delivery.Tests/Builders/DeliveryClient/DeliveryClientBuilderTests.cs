using System;
using FakeItEasy;
using KenticoCloud.Delivery.InlineContentItems;
using KenticoCloud.Delivery.ResiliencePolicy;
using KenticoCloud.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using RichardSzalay.MockHttp;
using Xunit;

namespace KenticoCloud.Delivery.Tests.Builders.DeliveryClient
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
            var mockCodeFirstModelProvider = A.Fake<ICodeFirstModelProvider>();
            var mockResiliencePolicyProvider = A.Fake<IResiliencePolicyProvider>();
            var mockCodeFirstPropertyMapper = A.Fake<ICodeFirstPropertyMapper>();
            var mockContentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var mockInlineContentItemsProcessor = A.Fake<IInlineContentItemsProcessor>();
            var mockInlineContentItemsResolver = A.Fake<IInlineContentItemsResolver<object>>();
            var mockCodeFirstTypeProvider = A.Fake<ICodeFirstTypeProvider>();
            var mockHttp = new MockHttpMessageHandler().ToHttpClient();
            A.CallTo(() => mockInlineContentItemsProcessor.DefaultResolver).Returns(mockInlineContentItemsResolver);

            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder
                .WithProjectId(ProjectId)
                .WithHttpClient(mockHttp)
                .WithContentLinkUrlResolver(mockContentLinkUrlResolver)
                .WithInlineContentItemsProcessor(mockInlineContentItemsProcessor)
                .WithInlineContentItemsResolver(mockInlineContentItemsResolver)
                .WithCodeFirstModelProvider(mockCodeFirstModelProvider)
                .WithCodeFirstPropertyMapper(mockCodeFirstPropertyMapper)
                .WithResiliencePolicyProvider(mockResiliencePolicyProvider)
                .WithCodeFirstTypeProvider(mockCodeFirstTypeProvider)
                .Build();

            Assert.Equal(ProjectId, deliveryClient.DeliveryOptions.ProjectId);
            Assert.Equal(mockContentLinkUrlResolver, deliveryClient.ContentLinkUrlResolver);
            Assert.Equal(mockInlineContentItemsResolver, deliveryClient.InlineContentItemsProcessor.DefaultResolver);
            Assert.Equal(mockInlineContentItemsProcessor, deliveryClient.InlineContentItemsProcessor);
            Assert.Equal(mockCodeFirstModelProvider, deliveryClient.CodeFirstModelProvider);
            Assert.Equal(mockCodeFirstPropertyMapper, deliveryClient.CodeFirstPropertyMapper);
            Assert.Equal(mockResiliencePolicyProvider, deliveryClient.ResiliencePolicyProvider);
            Assert.Equal(mockCodeFirstTypeProvider, deliveryClient.CodeFirstTypeProvider);
            Assert.Equal(mockHttp, deliveryClient.HttpClient);
        }

        [Fact]
        public void BuildWithOptionalStepsAndCustomProvider_ReturnsDeliveryClientWithSetInstances()
        {
            var codeFirstModelProvider = new FakeModelProvider();

            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder
                .WithProjectId(ProjectId)
                .WithCodeFirstModelProvider(codeFirstModelProvider)
                .Build();

            Assert.Equal(codeFirstModelProvider, deliveryClient.CodeFirstModelProvider);
        }

        [Fact]
        public void BuildWithoutOptionalStepts_ReturnsDeliveryClientWithDefaultImplementations()
        {
            var deliveryClient = (Delivery.DeliveryClient) DeliveryClientBuilder
                .WithProjectId(_guid)
                .Build();

            Assert.NotNull(deliveryClient.CodeFirstModelProvider);
            Assert.NotNull(deliveryClient.CodeFirstPropertyMapper);
            Assert.NotNull(deliveryClient.CodeFirstTypeProvider);
            Assert.NotNull(deliveryClient.ContentLinkUrlResolver);
            Assert.NotNull(deliveryClient.HttpClient);
            Assert.NotNull(deliveryClient.InlineContentItemsProcessor);
            Assert.NotNull(deliveryClient.ResiliencePolicyProvider);
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
        public void BuildWithOptionsAndNullCodeFirstModelProvider_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithCodeFirstModelProvider(null));
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
        public void BuildWithOptionsAndNullCodeFirstTypeProvider_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithCodeFirstTypeProvider(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullResiliencePolicyProvider_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithResiliencePolicyProvider(null));
        }

        [Fact]
        public void BuildWithOptionsAndNullCodeFirstPropertyMapper_ThrowsArgumentNullException()
        {
            var builderStep = DeliveryClientBuilder.WithProjectId(_guid);

            Assert.Throws<ArgumentNullException>(() => builderStep.WithCodeFirstPropertyMapper(null));
        }
    }
}
