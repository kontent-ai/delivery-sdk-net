using System;
using FakeItEasy;
using Kentico.Kontent.Delivery.Abstractions.RetryPolicy;
using Kentico.Kontent.Delivery.Abstractions.StrongTyping;
using Kentico.Kontent.Delivery.Configuration;
using RichardSzalay.MockHttp;

namespace Kentico.Kontent.Delivery.Tests.Factories
{
    internal static class DeliveryClientFactory
    {
        private static readonly MockHttpMessageHandler MockHttp = new MockHttpMessageHandler();
        private static IModelProvider _mockModelProvider = A.Fake<IModelProvider>();
        private static IRetryPolicyProvider _mockResiliencePolicyProvider = A.Fake<IRetryPolicyProvider>();
        private static ITypeProvider _mockTypeProvider = A.Fake<ITypeProvider>();

        internal static DeliveryClient GetMockedDeliveryClientWithProjectId(
            Guid projectId,
            MockHttpMessageHandler httpMessageHandler = null,
            IModelProvider modelProvider = null,
            IRetryPolicyProvider resiliencePolicyProvider = null,
            ITypeProvider typeProvider = null)
        {
            if (modelProvider != null) _mockModelProvider = modelProvider;
            if (resiliencePolicyProvider != null) _mockResiliencePolicyProvider = resiliencePolicyProvider;
            if (typeProvider != null) _mockTypeProvider = typeProvider;
            var httpClient = httpMessageHandler != null ? httpMessageHandler.ToHttpClient() : MockHttp.ToHttpClient();

            var client = new DeliveryClient(
                DeliveryOptionsFactory.CreateMonitor(new DeliveryOptions { ProjectId = projectId.ToString() }),
                _mockModelProvider,
                _mockResiliencePolicyProvider,
                _mockTypeProvider,
                new DeliveryHttpClient(httpClient)
            );

            return client;
        }

        internal static DeliveryClient GetMockedDeliveryClientWithOptions(DeliveryOptions options, MockHttpMessageHandler httpMessageHandler = null)
        {
            var deliveryHttpClient = new DeliveryHttpClient(httpMessageHandler != null ? httpMessageHandler.ToHttpClient() : MockHttp.ToHttpClient());
            var client = new DeliveryClient(
                DeliveryOptionsFactory.CreateMonitor(options),
                _mockModelProvider,
                _mockResiliencePolicyProvider,
                _mockTypeProvider,
                deliveryHttpClient
            );

            return client;
        }
    }
}
