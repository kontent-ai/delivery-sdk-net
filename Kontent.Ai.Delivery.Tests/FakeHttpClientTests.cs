﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FakeItEasy;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Builders.DeliveryClient;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests
{
    /// <summary>
    /// Sample test mocking HTTP client
    /// </summary>
    public class FakeHttpClientTests
    {
        [Fact]
        public async Task GetItemAsync()
        {
            // Arrange
            const string testUrl = "https://tests.fake.url";
            var deliveryOptions = MockDeliveryOptions(testUrl);
            var deliveryHttpClient = new DeliveryHttpClient(MockHttpClient(testUrl));
            var deliveryClient = MockDeliveryClient(deliveryOptions, deliveryHttpClient);

            // Act
            var contentItem = await deliveryClient.GetItemAsync<Home>("test");

            // Assert
            Assert.Equal("1bd6ba00-4bf2-4a2b-8334-917faa686f66", contentItem.Item.System.Id);
        }

        private static HttpClient MockHttpClient(string baseUrl)
        {
            var responseJsonPath = Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}home.json");
            var responseJson = File.ReadAllText(responseJsonPath);

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{baseUrl}/*").Respond("application/json", responseJson);

            return mockHttp.ToHttpClient();
        }

        private static DeliveryOptions MockDeliveryOptions(string baseUrl)
            => DeliveryOptionsBuilder
                .CreateInstance()
                .WithEnvironmentId(Guid.NewGuid())
                .UseProductionApi()
                .WithCustomEndpoint($"{baseUrl}/{{0}}")
                .Build();

        private static IDeliveryClient MockDeliveryClient(DeliveryOptions deliveryOptions, IDeliveryHttpClient deliveryHttpClient)
        {
            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var retryPolicy = A.Fake<IRetryPolicy>();
            var retryPolicyProvider = A.Fake<IRetryPolicyProvider>();
          
            A.CallTo(() => retryPolicyProvider.GetRetryPolicy())
                .Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            var client = DeliveryClientBuilder
                .WithOptions(_ => deliveryOptions)
                .WithDeliveryHttpClient(deliveryHttpClient)
                .WithContentLinkUrlResolver(contentLinkUrlResolver)
                .WithRetryPolicyProvider(retryPolicyProvider)
                .WithTypeProvider(new CustomTypeProvider())
                .Build();

            return client;
        }
    }
}
