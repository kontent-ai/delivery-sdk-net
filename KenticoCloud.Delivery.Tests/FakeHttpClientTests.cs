using System;
using System.IO;
using System.Net.Http;
using FakeItEasy;
using KenticoCloud.Delivery.ResiliencePolicy;
using Polly;
using RichardSzalay.MockHttp;
using Xunit;

namespace KenticoCloud.Delivery.Tests
{
    // Sample test mocking HTTP client
    public class FakeHttpClientTests
    {
        [Fact]
        public async void GetItemAsync()
        {
            // Arrange
            const string testUrl = "https://tests.fake.url";
            var httpClient = MockHttpClient(testUrl);
            var deliveryOptions = MockDeliveryOptions(testUrl);
            var deliveryClient = MockDeliveryClient(deliveryOptions, httpClient);

            // Act
            var contentItem = await deliveryClient.GetItemAsync("test");

            // Assert
            Assert.Equal("1bd6ba00-4bf2-4a2b-8334-917faa686f66", contentItem.Item.System.Id);
        }

        private static HttpClient MockHttpClient(string baseUrl)
        {
            var responseJsonPath = Path.Combine(Environment.CurrentDirectory, "Fixtures\\home.json");
            var responseJson = File.ReadAllText(responseJsonPath);

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{baseUrl}/*").Respond("application/json", responseJson);

            return mockHttp.ToHttpClient();
        }

        private static DeliveryOptions MockDeliveryOptions(string baseUrl)
            => DeliveryOptionsBuilder
                .CreateInstance()
                .WithProjectId(Guid.NewGuid())
                .UseProductionApi
                .WithCustomEndpoint($"{baseUrl}/{{0}}")
                .Build();

        private static IDeliveryClient MockDeliveryClient(DeliveryOptions deliveryOptions, HttpClient httpClient)
        {
            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var codeFirstModelProvider = A.Fake<ICodeFirstModelProvider>();
            var resiliencePolicyProvider = A.Fake<IResiliencePolicyProvider>();
            A.CallTo(() => resiliencePolicyProvider.Policy)
                .Returns(Policy
                    .HandleResult<HttpResponseMessage>(result => true)
                    .RetryAsync(deliveryOptions.MaxRetryAttempts));

            var client = DeliveryClientBuilder
                .WithOptions(_ => deliveryOptions)
                .WithHttpClient(httpClient)
                .WithContentLinkUrlResolver(contentLinkUrlResolver)
                .WithCodeFirstModelProvider(codeFirstModelProvider)
                .WithResiliencePolicyProvider(resiliencePolicyProvider)
                .Build();

            return client;
        }
    }
}
