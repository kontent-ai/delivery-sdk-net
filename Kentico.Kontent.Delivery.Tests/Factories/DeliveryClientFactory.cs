﻿using System;
using System.Net.Http;
using FakeItEasy;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.Configuration;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;
using Kentico.Kontent.Delivery.Abstractions.RetryPolicy;
using RichardSzalay.MockHttp;

namespace Kentico.Kontent.Delivery.Tests.Factories
{
    internal static class DeliveryClientFactory
    {
        private static readonly MockHttpMessageHandler MockHttp = new MockHttpMessageHandler();
        private static readonly DeliveryJsonSerializer Serializer = new DeliveryJsonSerializer();

        internal static DeliveryClient GetMockedDeliveryClientWithProjectId(
            Guid projectId,
            MockHttpMessageHandler httpMessageHandler = null,
            IModelProvider modelProvider = null,
            IRetryPolicyProvider resiliencePolicyProvider = null,
            ITypeProvider typeProvider = null)
        {
            var httpClient = GetHttpClient(httpMessageHandler);

            var client = new DeliveryClient(
                DeliveryOptionsFactory.CreateMonitor(new DeliveryOptions { ProjectId = projectId.ToString() }),
                modelProvider ?? A.Fake<IModelProvider>(),
                resiliencePolicyProvider ?? A.Fake<IRetryPolicyProvider>(),
                typeProvider ?? A.Fake<ITypeProvider>(),
                new DeliveryHttpClient(httpClient),
                Serializer
            );

            return client;
        }

        internal static DeliveryClient GetMockedDeliveryClientWithOptions(DeliveryOptions options, MockHttpMessageHandler httpMessageHandler = null)
        {
            var httpClient = GetHttpClient(httpMessageHandler);
            var deliveryHttpClient = new DeliveryHttpClient(httpClient);
            
            var client = new DeliveryClient(
                DeliveryOptionsFactory.CreateMonitor(options),
                A.Fake<IModelProvider>(),
                A.Fake<IRetryPolicyProvider>(),
                A.Fake<ITypeProvider>(),
                deliveryHttpClient,
                Serializer
            );

            return client;
        }

        private static HttpClient GetHttpClient(MockHttpMessageHandler mockHttpMessageHandler)
        {
            return mockHttpMessageHandler != null ? mockHttpMessageHandler.ToHttpClient() : MockHttp.ToHttpClient();
        }
    }
}
