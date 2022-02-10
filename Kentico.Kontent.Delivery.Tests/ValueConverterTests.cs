using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FakeItEasy;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems;
using Kentico.Kontent.Delivery.Tests.Factories;
using Kentico.Kontent.Delivery.Tests.Models.ContentTypes;
using Kentico.Kontent.Delivery.Urls.QueryParameters;
using NodaTime;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests
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
    public class NodaTimeValueConverterAttribute : Attribute, IPropertyValueConverter<DateTime>
    {
        public Task<object> GetPropertyValueAsync<TElement>(PropertyInfo property, TElement element, ResolvingContext context) where TElement : IContentElementValue<DateTime>
        {
            var udt = DateTime.SpecifyKind(element.Value, DateTimeKind.Utc);
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
        public async void LinkedItemCodenamesValueConverter()
        {
            var mockHttp = new MockHttpMessageHandler();
            string url = $"{_baseUrl}/items/on_roasts";
            mockHttp.When(url).
               Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}on_roasts.json")));
            DeliveryClient client = InitializeDeliveryClient(mockHttp);

            var article = await client.GetItemAsync<Article>("on_roasts");

            Assert.Equal(new[]{"coffee_processing_techniques", "origins_of_arabica_bourbon"}, article.Item.RelatedArticleCodenames);
        }

        [Fact]
        public async void GreeterPropertyValueConverter()
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
        public async void NodaTimePropertyValueConverter()
        {
            var mockHttp = new MockHttpMessageHandler();
            string url = $"{_baseUrl}/items/on_roasts";
            mockHttp.When(url).
               Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}on_roasts.json")));
            DeliveryClient client = InitializeDeliveryClient(mockHttp);

            var article = await client.GetItemAsync<Article>("on_roasts");

            Assert.Equal(new ZonedDateTime(Instant.FromUtc(2014, 11, 7, 0, 0), DateTimeZone.Utc), article.Item.PostDateNodaTime);
        }

        [Fact]
        public async void RichTextViaValueConverter()
        {
            var mockHttp = new MockHttpMessageHandler();
            string url = $"{_baseUrl}/items/coffee_beverages_explained";
            mockHttp.When(url).
               WithQueryString("depth=15").
               Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));
            DeliveryClient client = InitializeDeliveryClient(mockHttp);

            // Try to get recursive linked items on_roasts -> item -> on_roasts
            var article = await client.GetItemAsync<Article>("coffee_beverages_explained", new DepthParameter(15));

            var hostedVideo = article.Item.BodyCopyRichText.FirstOrDefault(b => (b as IInlineContentItem)?.ContentItem is HostedVideo);
            var tweet = article.Item.BodyCopyRichText.FirstOrDefault(b => (b as IInlineContentItem)?.ContentItem is Tweet);

            Assert.NotNull(hostedVideo);
            Assert.NotNull(tweet);
        }

        private DeliveryClient InitializeDeliveryClient(MockHttpMessageHandler mockHttp)
        {
            var deliveryHttpClient = new DeliveryHttpClient(mockHttp.ToHttpClient());
            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var deliveryOptions = DeliveryOptionsFactory.CreateMonitor(new DeliveryOptions { ProjectId = _guid });
            var retryPolicy = A.Fake<IRetryPolicy>();
            var retryPolicyProvider = A.Fake<IRetryPolicyProvider>();
            A.CallTo(() => retryPolicyProvider.GetRetryPolicy()).Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._)).ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());
            var modelProvider = new ModelProvider(contentLinkUrlResolver, null, new CustomTypeProvider(), new PropertyMapper(), new DeliveryJsonSerializer(), new HtmlParser());
            var client = new DeliveryClient(deliveryOptions, modelProvider, retryPolicyProvider, null, deliveryHttpClient);

            return client;
        }
    }
}
