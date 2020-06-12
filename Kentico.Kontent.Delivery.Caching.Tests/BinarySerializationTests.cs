using System;
using System.IO;
using System.Linq;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Kentico.Kontent.Delivery.Caching.Extensions;
using Kentico.Kontent.Delivery.Caching.Tests.ContentTypes;
using Kentico.Kontent.Delivery.ContentItems;
using RichardSzalay.MockHttp;
using FluentAssertions;
using Xunit;

namespace Kentico.Kontent.Delivery.Caching.Tests
{
    public class BinarySerializationTests
    {
        private readonly Guid _projectId;
        private readonly string _baseUrl;
        private readonly MockHttpMessageHandler _mockHttp;

        public BinarySerializationTests()
        {
            _projectId = Guid.NewGuid();
            var projectId = _projectId.ToString();
            _baseUrl = $"https://deliver.kontent.ai/{projectId}";
            _mockHttp = new MockHttpMessageHandler();
        }

        [Fact]
        public async void GetItemAsync_SerializeAndDeserialize()
        {
            // Arrange
            string url = $"{_baseUrl}/items/brazil_natural_barra_grande";

            _mockHttp
                .When(url)
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}brazil_natural_barra_grande.json")));

            var client = DeliveryClientBuilder.WithProjectId(_projectId).WithTypeProvider(new CustomTypeProvider()).WithDeliveryHttpClient(new DeliveryHttpClient(_mockHttp.ToHttpClient())).Build();

            // Act
            var response = await client.GetItemAsync<Coffee>("brazil_natural_barra_grande");
            var serializedResponse = response.ToBson();
            var deserializedResponse = serializedResponse.FromBson<DeliveryItemResponse<Coffee>>();

            // Assert item equality
            // Assert all except DateTime (precision is lost when using BSON)
            response.Should().BeEquivalentTo(deserializedResponse, o => o.Excluding(p => p.SelectedMemberInfo.MemberType == typeof(DateTime)));

            // Check DateTime separately
            Assert.Equal(response.Item.System.LastModified.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"), deserializedResponse.Item.System.LastModified.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"));

            // Check that collections are ok
            Assert.NotEmpty(deserializedResponse.Item.Image);
            Assert.NotEmpty(deserializedResponse.Item.Processing);
        }

        [Fact]
        public async void GetItemsAsync_SerializeAndDeserialize()
        {
            // Arrange
            string url = $"{_baseUrl}/items";

            _mockHttp
                .When(url)
                .WithQueryString("system.type=article")
                .Respond("application/json", 
                    await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}full_articles.json")));

            var client = DeliveryClientBuilder.WithProjectId(_projectId).WithTypeProvider(new CustomTypeProvider()).WithDeliveryHttpClient(new DeliveryHttpClient(_mockHttp.ToHttpClient())).Build();

            // Act
            var response = await client.GetItemsAsync<Article>();

            var serializedResponse = response.ToBson();
            var deserializedResponse = serializedResponse.FromBson<DeliveryItemListingResponse<Article>>();

            // Assert item equality
            //response.Should().BeEquivalentTo(deserializedResponse, o => o.ExcludingNestedObjects().Excluding(p => p.SelectedMemberInfo.MemberType == typeof(DateTime) || p.SelectedMemberInfo.MemberType == typeof(DateTime?)));


            // Assert item
            //Assert.NotEmpty(deserializedResponse.Item.Image);
            //Assert.Equal(response.Items.Image.First().Url, deserializedResponse.Item.Image.First().Url);
            //Assert.NotEmpty(deserializedResponse.Item.Processing);
            //Assert.Equal(response.Item.Processing.First().Codename, deserializedResponse.Item.Processing.First().Codename);
            //Assert.Equal(response.Item.ProductName, deserializedResponse.Item.ProductName);
            //Assert.Equal(response.Item.Altitude, deserializedResponse.Item.Altitude);

            //// Assert system data
            //Assert.Equal(response.Item.System.LastModified.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"), deserializedResponse.Item.System.LastModified.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"));
            //Assert.Equal(response.Item.System.Type, deserializedResponse.Item.System.Type);
        }
    }
}
