using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FakeItEasy;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Tests.Factories;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Kontent.Ai.Urls.Delivery.QueryParameters;
using NodaTime;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TestGreeterValueConverterAttribute : Attribute, IPropertyValueConverter<string>
    {
        public Task<object> GetPropertyValueAsync<TElement>(PropertyInfo property, TElement element, ResolvingContext context) where TElement : IContentElementValue<string>
        {
            return Task.FromResult((object)$"Hello {element.Value}!");
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TestLinkedItemCodenamesValueConverterAttribute : Attribute, IPropertyValueConverter<List<string>>
    {
        public Task<object> GetPropertyValueAsync<TElement>(PropertyInfo property, TElement element, ResolvingContext context) where TElement : IContentElementValue<List<string>>
        {
            return Task.FromResult((object)element.Value);
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class NodaTimeValueConverterAttribute : Attribute, IPropertyValueConverter<DateTime?>
    {
        public Task<object> GetPropertyValueAsync<TElement>(PropertyInfo property, TElement element, ResolvingContext context) where TElement : IContentElementValue<DateTime?>
        {
            if (!element.Value.HasValue)
            {
                return Task.FromResult((object)null);
            }

            var udt = DateTime.SpecifyKind(element.Value.Value, DateTimeKind.Utc);
            return Task.FromResult((object)ZonedDateTime.FromDateTimeOffset(udt));
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
            DeliveryClient client = InitializeDeliveryClient(mockHttp);

            var article = await client.GetItemAsync<Article>("on_roasts");

            Assert.Equal(new[]{"coffee_processing_techniques", "origins_of_arabica_bourbon"}, article.Item.RelatedArticleCodenames);
        }

        [Fact]
        public async Task GreeterPropertyValueConverter()
        {
            var mockHttp = new MockHttpMessageHandler();
            string url = $"{_baseUrl}/items/on_roasts";
            mockHttp.When(url).
                Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}on_roasts.json")));
            DeliveryClient client = InitializeDeliveryClient(mockHttp);

            var article = await client.GetItemAsync<Article>("on_roasts");

            Assert.Equal("Hello On Roasts!", article.Item.TitleConverted);
        }

        [Fact]
        public async Task NodaTimePropertyValueConverter()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{_baseUrl}/items/on_roasts")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}on_roasts.json")));
            DeliveryClient client = InitializeDeliveryClient(mockHttp);

            var article = await client.GetItemAsync<Article>("on_roasts");

            Assert.Equal(new ZonedDateTime(Instant.FromUtc(2014, 11, 7, 0, 0), DateTimeZone.Utc), article.Item.PostDateNodaTime);
        }

        [Fact]
        public async Task RichTextViaValueConverter()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{_baseUrl}/items/coffee_beverages_explained")
                .WithQueryString("depth=15")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));
            DeliveryClient client = InitializeDeliveryClient(mockHttp);

            // Try to get recursive linked items on_roasts -> item -> on_roasts
            var article = await client.GetItemAsync<Article>("coffee_beverages_explained", new DepthParameter(15));

            var hostedVideo = article.Item.BodyCopyRichText.FirstOrDefault(b => (b as IInlineContentItem)?.ContentItem is HostedVideo);
            var tweet = article.Item.BodyCopyRichText.FirstOrDefault(b => (b as IInlineContentItem)?.ContentItem is Tweet);

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

            var client = InitializeDeliveryClient(mockHttp);

            var response = await client.GetItemAsync<Article>("coffee_beverages_explained");
            var teaserImage = response.Item.TeaserImage.FirstOrDefault();

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

            var client = InitializeDeliveryClient(mockHttp, new DeliveryOptions { EnvironmentId = _guid, DefaultRenditionPreset = defaultRenditionPreset});

            var response = await client.GetItemAsync<Article>("coffee_beverages_explained");
            var teaserImage = response.Item.TeaserImage.FirstOrDefault();

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

            var client = InitializeDeliveryClient(mockHttp, new DeliveryOptions { EnvironmentId = _guid, DefaultRenditionPreset = defaultRenditionPreset});
            
            var response = await client.GetItemAsync<Article>("coffee_beverages_explained");
            var teaserImage = response.Item.TeaserImage.FirstOrDefault();

            var assetUrl = "https://assets.kontent.ai:443/975bf280-fd91-488c-994c-2f04416e5ee3/e700596b-03b0-4cee-ac5c-9212762c027a/coffee-beverages-explained-1080px.jpg";

            Assert.Equal(assetUrl, teaserImage.Url);
        }

        private DeliveryClient InitializeDeliveryClient(MockHttpMessageHandler mockHttp, DeliveryOptions options = null)
        {
            var deliveryHttpClient = new DeliveryHttpClient(mockHttp.ToHttpClient());
            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var deliveryOptions = DeliveryOptionsFactory.CreateMonitor(options ?? new DeliveryOptions { EnvironmentId = _guid });
            var retryPolicy = A.Fake<IRetryPolicy>();
            var retryPolicyProvider = A.Fake<IRetryPolicyProvider>();
            A.CallTo(() => retryPolicyProvider.GetRetryPolicy()).Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._)).ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());
            var modelProvider = new ModelProvider(contentLinkUrlResolver, null, new CustomTypeProvider(), new PropertyMapper(), new DeliveryJsonSerializer(), new HtmlParser(), deliveryOptions);
            var client = new DeliveryClient(deliveryOptions, modelProvider, retryPolicyProvider, null, deliveryHttpClient);

            return client;
        }
    }
}
