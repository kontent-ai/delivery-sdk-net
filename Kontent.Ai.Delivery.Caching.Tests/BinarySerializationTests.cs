﻿using System;
using System.IO;
using System.Linq;
using Kontent.Ai.Delivery.Builders.DeliveryClient;
using Kontent.Ai.Delivery.Caching.Extensions;
using Kontent.Ai.Delivery.Caching.Tests.ContentTypes;
using Kontent.Ai.Delivery.ContentItems;
using RichardSzalay.MockHttp;
using FluentAssertions;
using Xunit;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Caching.Tests
{
    public class BinarySerializationTests
    {
        private readonly Guid _environmentId;
        private readonly string _baseUrl;
        private readonly MockHttpMessageHandler _mockHttp;

        public BinarySerializationTests()
        {
            _environmentId = Guid.NewGuid();
            var environmentId = _environmentId.ToString();
            _baseUrl = $"https://deliver.kontent.ai/{environmentId}";
            _mockHttp = new MockHttpMessageHandler();
        }

        [Fact]
        public async Task GetItemAsync_SerializeAndDeserialize()
        {
            // Arrange
            string url = $"{_baseUrl}/items/brazil_natural_barra_grande";

            _mockHttp
                .When(url)
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}brazil_natural_barra_grande.json")));

            var client = DeliveryClientBuilder.WithEnvironmentId(_environmentId).WithTypeProvider(new CustomTypeProvider()).WithDeliveryHttpClient(new DeliveryHttpClient(_mockHttp.ToHttpClient())).Build();

            // Act
            var response = await client.GetItemAsync<Coffee>("brazil_natural_barra_grande");
            var serializedResponse = response.ToBson();
            var deserializedResponse = serializedResponse.FromBson<DeliveryItemResponse<Coffee>>();

            // Assert item equality (apply precision correction for DateTime when deserializing)
            response.Should().BeEquivalentTo(deserializedResponse, o => o.DateTimesBsonCorrection());

            // Check that collections are ok
            Assert.NotEmpty(deserializedResponse.Item.Image);
            Assert.NotEmpty(deserializedResponse.Item.Processing);
        }

        [Fact]
        public async Task GetItemsAsync_SerializeAndDeserialize()
        {
            // Arrange
            string url = $"{_baseUrl}/items";

            _mockHttp
                .When(url)
                .WithQueryString("system.type=article")
                .Respond("application/json",
                    await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}full_articles.json")));

            var client = DeliveryClientBuilder.WithEnvironmentId(_environmentId).WithTypeProvider(new CustomTypeProvider()).WithDeliveryHttpClient(new DeliveryHttpClient(_mockHttp.ToHttpClient())).Build();

            // Act
            var response = await client.GetItemsAsync<Article>();

            var serializedResponse = response.ToBson();
            var deserializedResponse = serializedResponse.FromBson<DeliveryItemListingResponse<Article>>();

            // Assert item equality
            response.Should().BeEquivalentTo(deserializedResponse, o => o.IgnoringCyclicReferences().DateTimesBsonCorrection());

            // Assert the first item - check collections and DateTimes
            var firstItem = response.Items.FirstOrDefault();
            var firstDeserializedItem = deserializedResponse.Items.FirstOrDefault();
            Assert.NotEmpty(firstDeserializedItem.TeaserImage);
            Assert.NotEmpty(firstDeserializedItem.Personas);
            Assert.Equal(firstItem.PostDate, firstDeserializedItem.PostDate);
            Assert.Equal(firstItem.PostDate.Value.Kind, firstDeserializedItem.PostDate.Value.Kind);
            Assert.Equal(firstItem.PostDateContent.DisplayTimezone, firstDeserializedItem.PostDateContent.DisplayTimezone);
            Assert.Equal(firstItem.PostDateContent.Value, firstDeserializedItem.PostDateContent.Value);
            Assert.Equal(firstItem.PostDateContent.Value.Value.Kind, firstDeserializedItem.PostDateContent.Value.Value.Kind);
        }
    }
}
