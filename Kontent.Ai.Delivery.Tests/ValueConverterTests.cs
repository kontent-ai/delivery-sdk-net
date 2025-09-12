using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TestGreeterValueConverterAttribute : Attribute, IPropertyValueConverter<string, object>
    {
        public Task<object?> GetPropertyValueAsync<TElement>(PropertyInfo property, TElement element, ResolvingContext context) where TElement : IContentElementValue<string>
            => Task.FromResult<object?>($"Hello {element.Value}!");
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TestLinkedItemCodenamesValueConverterAttribute : Attribute, IPropertyValueConverter<List<string>, object>
    {
        public Task<object?> GetPropertyValueAsync<TElement>(PropertyInfo property, TElement element, ResolvingContext context) where TElement : IContentElementValue<List<string>>
            => Task.FromResult<object?>(element.Value);
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class NodaTimeValueConverterAttribute : Attribute, IPropertyValueConverter<DateTime?, ZonedDateTime>
    {
        public Task<ZonedDateTime> GetPropertyValueAsync<TElement>(PropertyInfo property, TElement element, ResolvingContext context) where TElement : IContentElementValue<DateTime?>
        {
            if (!element.Value.HasValue) return Task.FromResult<ZonedDateTime>(default);
            var udt = DateTime.SpecifyKind(element.Value.Value, DateTimeKind.Utc);
            return Task.FromResult(ZonedDateTime.FromDateTimeOffset(udt));
        }
    }

    public class ValueConverterTests
    {
        private readonly string _guid;
        private readonly string _baseUrl;

        public ValueConverterTests()
        {
            _guid = Guid.NewGuid().ToString();
            _baseUrl = $"https://deliver.kontent.ai/{_guid}";
        }

        [Fact]
        public async Task LinkedItemCodenamesValueConverter()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{_baseUrl}/items/on_roasts")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}on_roasts.json")));
            var client = CreateClient(mockHttp);
            var result = await client.GetItem<Article>("on_roasts").ExecuteAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(new[] { "coffee_processing_techniques", "origins_of_arabica_bourbon" }, result.Value.Elements.RelatedArticleCodenames);
        }

        [Fact]
        public async Task GreeterPropertyValueConverter()
        {
            var mockHttp = new MockHttpMessageHandler();
            string url = $"{_baseUrl}/items/on_roasts";
            mockHttp.When(url).
                Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}on_roasts.json")));
            var client = CreateClient(mockHttp);
            var result = await client.GetItem<Article>("on_roasts").ExecuteAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal("Hello On Roasts!", result.Value.Elements.TitleConverted);
        }

        [Fact]
        public async Task NodaTimePropertyValueConverter()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{_baseUrl}/items/on_roasts")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}on_roasts.json")));
            var client = CreateClient(mockHttp);
            var result = await client.GetItem<Article>("on_roasts").ExecuteAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(new ZonedDateTime(Instant.FromUtc(2014, 11, 7, 0, 0), DateTimeZone.Utc), result.Value.Elements.PostDateNodaTime);
        }

        [Fact]
        public async Task RichTextViaValueConverter()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{_baseUrl}/items/coffee_beverages_explained")
                .WithQueryString("depth=15")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));
            var client = CreateClient(mockHttp);
            var result = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();

            Assert.True(result.IsSuccess);
            var hostedVideo = result.Value.Elements.BodyCopyRichText.FirstOrDefault(b => (b as IInlineContentItem)?.ContentItem is HostedVideo);
            var tweet = result.Value.Elements.BodyCopyRichText.FirstOrDefault(b => (b as IInlineContentItem)?.ContentItem is Tweet);
            Assert.NotNull(hostedVideo);
            Assert.NotNull(tweet);
        }

        [Fact]
        public async Task AssetElementValueConverter_NoPresetSpecifiedInConfig_AssetUrlIsUntouched()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var client = CreateClient(mockHttp);
            var response = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();
            var teaserImage = response.Value.Elements.TeaserImage.FirstOrDefault();

            var assetUrl = "https://assets.kontent.ai:443/975bf280-fd91-488c-994c-2f04416e5ee3/e700596b-03b0-4cee-ac5c-9212762c027a/coffee-beverages-explained-1080px.jpg";

            Assert.Equal(assetUrl, teaserImage.Url);
        }

        [Fact]
        public async Task AssetElementValueConverter_DefaultPresetSpecifiedInConfig_AssetUrlContainsDefaultRenditionQuery()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var defaultRenditionPreset = "default";

            var client = CreateClient(mockHttp, new DeliveryOptions { EnvironmentId = _guid, DefaultRenditionPreset = defaultRenditionPreset });
            var response = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();
            var teaserImage = response.Value.Elements.TeaserImage.FirstOrDefault();

            var assetUrl = "https://assets.kontent.ai:443/975bf280-fd91-488c-994c-2f04416e5ee3/e700596b-03b0-4cee-ac5c-9212762c027a/coffee-beverages-explained-1080px.jpg";
            var defaultRenditionQuery = "w=200&h=150&fit=clip&rect=7,23,300,200";

            Assert.Equal($"{assetUrl}?{defaultRenditionQuery}", teaserImage.Url);
        }

        [Fact]
        public async Task AssetElementValueConverter_MobilePresetSpecifiedInConfig_AssetUrlIsUntouchedAsThereIsNoMobileRenditionSpecified()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var defaultRenditionPreset = "mobile";

            var client = CreateClient(mockHttp, new DeliveryOptions { EnvironmentId = _guid, DefaultRenditionPreset = defaultRenditionPreset });
            var response = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();
            var teaserImage = response.Value.Elements.TeaserImage.FirstOrDefault();

            var assetUrl = "https://assets.kontent.ai:443/975bf280-fd91-488c-994c-2f04416e5ee3/e700596b-03b0-4cee-ac5c-9212762c027a/coffee-beverages-explained-1080px.jpg";

            Assert.Equal(assetUrl, teaserImage.Url);
        }

        private IDeliveryClient CreateClient(MockHttpMessageHandler mockHttp, DeliveryOptions options = null)
        {
            var services = new ServiceCollection();
            var opts = options ?? new DeliveryOptions { EnvironmentId = _guid };
            services.AddDeliveryClient(opts, configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
            // Register custom content link url resolver to satisfy value converter usages
            services.AddSingleton<IContentLinkUrlResolver, Kontent.Ai.Delivery.ContentItems.ContentLinks.DefaultContentLinkUrlResolver>();
            return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
        }
    }
}
