using System;
using System.Net.Http;
using FakeItEasy;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Kontent.Ai.Delivery.Extensions;

namespace Kontent.Ai.Delivery.Tests.Factories
{
    internal static class DeliveryClientFactory
    {
        private static readonly MockHttpMessageHandler MockHttp = new MockHttpMessageHandler();

        internal static DeliveryClient GetMockedDeliveryClientWithEnvironmentId(
            Guid environmentId,
            MockHttpMessageHandler httpMessageHandler = null,
            IModelProvider modelProvider = null,
            IRetryPolicyProvider resiliencePolicyProvider = null,
            ITypeProvider typeProvider = null)
        {
            var services = new ServiceCollection();

            if (typeProvider != null)
            {
                services.AddSingleton(typeProvider);
            }

            if (modelProvider != null)
            {
                services.AddSingleton(modelProvider);
            }

            var options = new DeliveryOptions { EnvironmentId = environmentId.ToString() };

            services.AddDeliveryClient(
                options,
                configureRefit: null,
                configureHttpClient: builder =>
                {
                    if (httpMessageHandler != null)
                    {
                        builder.ConfigurePrimaryHttpMessageHandler(() => httpMessageHandler);
                    }
                });

            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IDeliveryClient>();
            return (DeliveryClient)client;
        }

        internal static DeliveryClient GetMockedDeliveryClientWithOptions(DeliveryOptions options, MockHttpMessageHandler httpMessageHandler = null)
        {
            var services = new ServiceCollection();

            services.AddDeliveryClient(
                options,
                configureRefit: null,
                configureHttpClient: builder =>
                {
                    if (httpMessageHandler != null)
                    {
                        builder.ConfigurePrimaryHttpMessageHandler(() => httpMessageHandler);
                    }
                });

            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IDeliveryClient>();
            return (DeliveryClient)client;
        }
    }
}
