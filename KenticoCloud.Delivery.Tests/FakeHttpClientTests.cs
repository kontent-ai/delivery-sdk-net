using System;
using System.IO;
using FakeItEasy;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Xunit;

namespace KenticoCloud.Delivery.Tests
{
    public class FakeHttpClientTests
    {
        [Fact]
        public async void GetItemAsync()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("https://deliver.kenticocloud.com/*").Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Fixtures\\home.json")));
            var httpClient = mockHttp.ToHttpClient();
            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var codeFirstModelProvider = A.Fake<ICodeFirstModelProvider>();
            var deliveryOptions = new OptionsWrapper<DeliveryOptions>(new DeliveryOptions { ProjectId = Guid.NewGuid().ToString() });
            var client = new DeliveryClient(
                deliveryOptions,
                contentLinkUrlResolver,
                null,
                codeFirstModelProvider
            )
            {
                HttpClient = httpClient,
            };

            // Act
            var contentItem = await client.GetItemAsync("test");

            // Assert
            Assert.Equal("1bd6ba00-4bf2-4a2b-8334-917faa686f66", contentItem.Item.System.Id);
        }
    }
}
