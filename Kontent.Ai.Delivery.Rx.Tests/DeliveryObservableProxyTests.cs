using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FakeItEasy;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Rx.Tests.Models.ContentTypes;
using Kontent.Ai.Urls.Delivery.QueryParameters;
using Kontent.Ai.Urls.Delivery.QueryParameters.Filters;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Rx.Tests
{
    public class DeliveryObservableProxyTests
    {
        private const string BEVERAGES_IDENTIFIER = "coffee_beverages_explained";
        readonly string _guid;
        readonly string _baseUrl;
        readonly MockHttpMessageHandler _mockHttp;

        public DeliveryObservableProxyTests()
        {
            _guid = Guid.NewGuid().ToString();
            _baseUrl = $"https://deliver.kontent.ai/{_guid}";
            _mockHttp = new MockHttpMessageHandler();
        }

        [Fact]
        public async void TypedItemRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockItem)).GetItemObservable<Article>(BEVERAGES_IDENTIFIER, new LanguageParameter("es-ES"));
            var item = await observable.FirstOrDefaultAsync();

            Assert.NotNull(item);
            AssertArticlePropertiesNotNull(item);
        }

        [Fact]
        public async void RuntimeTypedItemRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockItem)).GetItemObservable<object>(BEVERAGES_IDENTIFIER, new LanguageParameter("es-ES"));
            var item = await observable.FirstOrDefaultAsync();

            Assert.IsType<Article>(item);
            Assert.NotNull(item);
            AssertArticlePropertiesNotNull((Article)item);
        }

        [Fact]
        public void TypedItemsRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockArticles)).GetItemsObservable<Article>(new ContainsFilter("elements.personas", "barista"));
            var items = observable.ToEnumerable().ToList();

            Assert.NotEmpty(items);
            Assert.Equal(6, items.Count);
            Assert.All(items, AssertArticlePropertiesNotNull);
        }

        [Fact]
        public void TypedItemsFeedRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockFeedArticles)).GetItemsFeedObservable<Article>(new ContainsFilter("elements.personas", "barista"));
            var items = observable.ToEnumerable().ToList();

            Assert.NotEmpty(items);
            Assert.Equal(6, items.Count);
            Assert.All(items, AssertArticlePropertiesNotNull);
        }

        [Fact]
        public void RuntimeTypedItemsRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockArticles)).GetItemsObservable<Article>(new ContainsFilter("elements.personas", "barista"));
            var articles = observable.ToEnumerable().ToList();

            Assert.NotEmpty(articles);
            Assert.All(articles, article => Assert.IsType<Article>(article));
            Assert.All(articles, AssertArticlePropertiesNotNull);
        }

        [Fact]
        public void RuntimeTypedItemsFeedRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockFeedArticles)).GetItemsFeedObservable<Article>(new ContainsFilter("elements.personas", "barista"));
            var articles = observable.ToEnumerable().ToList();

            Assert.NotEmpty(articles);
            Assert.All(articles, article => Assert.IsType<Article>(article));
            Assert.All(articles, AssertArticlePropertiesNotNull);
        }

        [Fact]
        public async void TypeRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockType)).GetTypeObservable(Article.Codename);
            var type = await observable.FirstOrDefaultAsync();

            Assert.Single(observable.ToEnumerable());
            Assert.NotNull(type.System);
            Assert.NotEmpty(type.Elements);
        }

        [Fact]
        public void TypesRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockTypes)).GetTypesObservable(new SkipParameter(2));
            var types = observable.ToEnumerable().ToList();

            Assert.NotEmpty(types);
            Assert.All(types, Assert.NotNull);
            Assert.All(types, type => Assert.NotEmpty(type.Elements));
        }

        [Fact]
        public async void ElementRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockElement)).GetElementObservable(Article.Codename, Article.TitleCodename);
            var element = await observable.FirstOrDefaultAsync();

            Assert.NotNull(element);
            Assert.NotNull(element.Codename);
            Assert.NotNull(element.Name);
            Assert.NotNull(element.Type);
        }

        [Fact]
        public async void TaxonomyElementRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockTaxonomyElement)).GetElementObservable(Coffee.Codename, Coffee.ProcessingCodename);
            var element = await observable.FirstOrDefaultAsync();

            Assert.IsAssignableFrom<ITaxonomyElement>(element);
        }

        [Fact]
        public async void MultipleChoiceElementRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockMultipleChoiceElement)).GetElementObservable(Tweet.Codename, Tweet.ThemeCodename);
            var element = await observable.FirstOrDefaultAsync();

            Assert.IsAssignableFrom<IMultipleChoiceElement>(element);
        }

        [Fact]
        public async void TaxonomyRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockTaxonomy)).GetTaxonomyObservable("personas");
            var taxonomy = await observable.FirstOrDefaultAsync();

            Assert.NotNull(taxonomy);
            Assert.NotNull(taxonomy.System);
            Assert.NotNull(taxonomy.Terms);
        }

        [Fact]
        public void TaxonomiesRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockTaxonomies)).GetTaxonomiesObservable(new SkipParameter(1));
            var taxonomies = observable.ToEnumerable().ToList();

            Assert.NotEmpty(taxonomies);
            Assert.All(taxonomies, taxonomy => Assert.NotNull(taxonomy.System));
            Assert.All(taxonomies, taxonomy => Assert.NotNull(taxonomy.Terms));
        }

        [Fact]
        public void LanguagesRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockLanguages)).GetLanguagesObservable(new SkipParameter(1));
            var languages = observable.ToEnumerable().ToList();

            Assert.NotEmpty(languages);
            Assert.All(languages, language => Assert.NotNull(language.System));
        }
        public static IOptionsMonitor<DeliveryOptions> CreateMonitor(DeliveryOptions options)
        {
            var mock = A.Fake<IOptionsMonitor<DeliveryOptions>>();
            A.CallTo(() => mock.CurrentValue).Returns(options);
            return mock;
        }

        private IDeliveryClient GetDeliveryClient(Action mockAction)
        {
            mockAction();
            var deliveryHttpClient = new DeliveryHttpClient(_mockHttp.ToHttpClient());
            var deliveryOptions = CreateMonitor(new DeliveryOptions { ProjectId = _guid });
            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var contentItemsProcessor = A.Fake<IInlineContentItemsProcessor>();
            var contentPropertyMapper = new PropertyMapper();
            var contentTypeProvider = new CustomTypeProvider();
            var serializer = new DeliveryJsonSerializer();
            var modelProvider = new ModelProvider(contentLinkUrlResolver, contentItemsProcessor, contentTypeProvider, contentPropertyMapper, serializer, new HtmlParser(), deliveryOptions);
            var retryPolicy = A.Fake<IRetryPolicy>();
            var retryPolicyProvider = A.Fake<IRetryPolicyProvider>();
            A.CallTo(() => retryPolicyProvider.GetRetryPolicy()).Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(call => call.GetArgument<Func<Task<HttpResponseMessage>>>(0)());
            var client = new DeliveryClient(
                deliveryOptions,
                modelProvider,
                retryPolicyProvider,
                contentTypeProvider,
                deliveryHttpClient,
                serializer
            );

            return client;
        }

        private void MockItem()
        {
            _mockHttp.When($"{_baseUrl}/items/{BEVERAGES_IDENTIFIER}?language=es-ES")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));
        }

        private void MockArticles()
        {
            _mockHttp.When($"{_baseUrl}/items")
                .WithQueryString(new[] { new KeyValuePair<string, string>("system.type", Article.Codename), new KeyValuePair<string, string>("elements.personas[contains]", "barista") })
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}articles.json")));
        }

        private void MockFeedArticles()
        {
            _mockHttp.When($"{_baseUrl}/items-feed")
                .WithQueryString(new[] { new KeyValuePair<string, string>("system.type", Article.Codename), new KeyValuePair<string, string>("elements.personas[contains]", "barista") })
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}articles.json")));
        }

        private void MockType()
        {
            _mockHttp.When($"{_baseUrl}/types/{Article.Codename}")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}article-type.json")));
        }

        private void MockTypes()
        {
            _mockHttp.When($"{_baseUrl}/types?skip=2")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}types.json")));
        }

        private void MockElement()
        {
            _mockHttp.When($"{_baseUrl}/types/{Article.Codename}/elements/{Article.TitleCodename}")
                .Respond("application/json", "{'type':'text','name':'Title','codename':'title'}");
        }
        private void MockTaxonomyElement()
        {
            _mockHttp
                   .When($"{_baseUrl}/types/{Coffee.Codename}/elements/{Coffee.ProcessingCodename}")
                   .Respond("application/json", "{'type':'taxonomy','name':'Processing','taxonomy_group':'processing','codename':'processing'}");
        }

        private void MockMultipleChoiceElement()
        {
            _mockHttp
                .When($"{_baseUrl}/types/{Tweet.Codename}/elements/{Tweet.ThemeCodename}")
                .Respond("application/json", "{ 'type': 'multiple_choice', 'name': 'Theme', 'options': [ { 'name': 'Dark', 'codename': 'dark' }, { 'name': 'Light', 'codename': 'light' } ], 'codename': 'theme' }");
        }

        private void MockTaxonomy()
        {
            _mockHttp.When($"{_baseUrl}/taxonomies/personas")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}taxonomies_personas.json")));
        }

        private void MockTaxonomies()
        {
            _mockHttp.When($"{_baseUrl}/taxonomies")
                .WithQueryString("skip=1")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}taxonomies_multiple.json")));
        }

        private void MockLanguages()
        {
            _mockHttp.When($"{_baseUrl}/languages")
                .WithQueryString("skip=1")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}languages.json")));
        }

        private static void AssertArticlePropertiesNotNull(Article item)
        {
            Assert.NotNull(item.System);
            Assert.NotNull(item.Personas);
            Assert.NotNull(item.Title);
            Assert.NotNull(item.TeaserImage);
            Assert.NotNull(item.PostDate);
            Assert.NotNull(item.Summary);
            Assert.NotNull(item.BodyCopy);
            Assert.NotNull(item.RelatedArticles);
            Assert.NotNull(item.MetaKeywords);
            Assert.NotNull(item.MetaDescription);
            Assert.NotNull(item.UrlPattern);
        }
    }
}
