using System;
using FakeItEasy;
using KenticoCloud.Delivery.InlineContentItems;
using KenticoCloud.Delivery.ResiliencePolicy;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace KenticoCloud.Delivery.Tests.Factories
{
    internal static class DeliveryClientFactory
    {
        private static readonly MockHttpMessageHandler MockHttp = new MockHttpMessageHandler();
        private static ICodeFirstModelProvider _mockCodeFirstModelProvider = A.Fake<ICodeFirstModelProvider>();
        private static ICodeFirstPropertyMapper _mockCodeFirstPropertyMapper = A.Fake<ICodeFirstPropertyMapper>();
        private static IResiliencePolicyProvider _mockResiliencePolicyProvider = A.Fake<IResiliencePolicyProvider>();
        private static ICodeFirstTypeProvider _mockCodeFirstTypeProvider = A.Fake<ICodeFirstTypeProvider>();
        private static IContentLinkUrlResolver _mockContentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
        private static IInlineContentItemsProcessor _mockInlineContentItemsProcessor = A.Fake<IInlineContentItemsProcessor>();

        internal static DeliveryClient GetMockedDeliveryClientWithProjectId(
            Guid projectId,
            MockHttpMessageHandler httpMessageHandler = null,
            ICodeFirstModelProvider codeFirstModelProvider = null,
            ICodeFirstPropertyMapper codeFirstPropertyMapper = null,
            IResiliencePolicyProvider resiliencePolicyProvider = null,
            ICodeFirstTypeProvider codeFirstTypeProvider = null,
            IContentLinkUrlResolver contentLinkUrlResolver = null,
            IInlineContentItemsProcessor inlineContentItemsProcessor = null
        )
        {
            if (codeFirstModelProvider != null) _mockCodeFirstModelProvider = codeFirstModelProvider;
            if (codeFirstPropertyMapper != null) _mockCodeFirstPropertyMapper = codeFirstPropertyMapper;
            if (resiliencePolicyProvider != null) _mockResiliencePolicyProvider = resiliencePolicyProvider;
            if (codeFirstTypeProvider != null) _mockCodeFirstTypeProvider = codeFirstTypeProvider;
            if (contentLinkUrlResolver != null) _mockContentLinkUrlResolver = contentLinkUrlResolver;
            if (inlineContentItemsProcessor != null) _mockInlineContentItemsProcessor = inlineContentItemsProcessor;
            var httpClient = httpMessageHandler != null ? httpMessageHandler.ToHttpClient() : MockHttp.ToHttpClient();

            var client = new DeliveryClient(
                Options.Create(new DeliveryOptions { ProjectId = projectId.ToString() }),
                httpClient,
                _mockContentLinkUrlResolver,
                _mockInlineContentItemsProcessor,
                _mockCodeFirstModelProvider,
                _mockResiliencePolicyProvider,
                _mockCodeFirstTypeProvider,
                _mockCodeFirstPropertyMapper
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
                _mockCodeFirstModelProvider,
                _mockResiliencePolicyProvider,
                _mockCodeFirstTypeProvider,
                _mockCodeFirstPropertyMapper
            );

            return client;
        }
    }
}
