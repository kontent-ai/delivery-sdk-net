using System;
using FakeItEasy;
using KenticoKontent.Delivery.InlineContentItems;
using KenticoKontent.Delivery.ResiliencePolicy;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace KenticoKontent.Delivery.Tests.Factories
{
    internal static class DeliveryClientFactory
    {
        private static readonly MockHttpMessageHandler MockHttp = new MockHttpMessageHandler();
        private static IModelProvider _mockModelProvider = A.Fake<IModelProvider>();
        private static IPropertyMapper _mockPropertyMapper = A.Fake<IPropertyMapper>();
        private static IResiliencePolicyProvider _mockResiliencePolicyProvider = A.Fake<IResiliencePolicyProvider>();
        private static ITypeProvider _mockTypeProvider = A.Fake<ITypeProvider>();
        private static IContentLinkUrlResolver _mockContentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
        private static IInlineContentItemsProcessor _mockInlineContentItemsProcessor = A.Fake<IInlineContentItemsProcessor>();

        internal static DeliveryClient GetMockedDeliveryClientWithProjectId(
            Guid projectId,
            MockHttpMessageHandler httpMessageHandler = null,
            IModelProvider modelProvider = null,
            IPropertyMapper propertyMapper = null,
            IResiliencePolicyProvider resiliencePolicyProvider = null,
            ITypeProvider typeProvider = null,
            IContentLinkUrlResolver contentLinkUrlResolver = null,
            IInlineContentItemsProcessor inlineContentItemsProcessor = null
        )
        {
            if (modelProvider != null) _mockModelProvider = modelProvider;
            if (propertyMapper != null) _mockPropertyMapper = propertyMapper;
            if (resiliencePolicyProvider != null) _mockResiliencePolicyProvider = resiliencePolicyProvider;
            if (typeProvider != null) _mockTypeProvider = typeProvider;
            if (contentLinkUrlResolver != null) _mockContentLinkUrlResolver = contentLinkUrlResolver;
            if (inlineContentItemsProcessor != null) _mockInlineContentItemsProcessor = inlineContentItemsProcessor;
            var httpClient = httpMessageHandler != null ? httpMessageHandler.ToHttpClient() : MockHttp.ToHttpClient();

            var client = new DeliveryClient(
                Options.Create(new DeliveryOptions { ProjectId = projectId.ToString() }),
                httpClient,
                _mockContentLinkUrlResolver,
                _mockInlineContentItemsProcessor,
                _mockModelProvider,
                _mockResiliencePolicyProvider,
                _mockTypeProvider,
                _mockPropertyMapper
            );

            return client;
        }

        internal static DeliveryClient GetMockedDeliveryClientWithOptions(DeliveryOptions options, MockHttpMessageHandler httpMessageHandler = null)
        {
            var httpClient = httpMessageHandler != null ? httpMessageHandler.ToHttpClient() : MockHttp.ToHttpClient();
            var client = new DeliveryClient(
                Options.Create(options),
                httpClient,
                _mockContentLinkUrlResolver,
                _mockInlineContentItemsProcessor,
                _mockModelProvider,
                _mockResiliencePolicyProvider,
                _mockTypeProvider,
                _mockPropertyMapper
            );

            return client;
        }
    }
}
