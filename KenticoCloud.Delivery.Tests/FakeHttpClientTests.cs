using System;
using System.IO;
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
            mockHttp.When("https://deliver.kenticocloud.com/*").Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\home.json")));
            var httpClient = mockHttp.ToHttpClient();
            DeliveryClient client = new DeliveryClient(Guid.NewGuid().ToString()) { HttpClient = httpClient };

            // Act
            var contentItem = await client.GetItemAsync("test");

            // Assert
            Assert.Equal("1bd6ba00-4bf2-4a2b-8334-917faa686f66", contentItem.Item.System.Id);
        }
    }
}
