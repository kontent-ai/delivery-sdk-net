using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.InlineContentItems;
using Kentico.Kontent.Delivery.Abstractions.RetryPolicy;
using Kentico.Kontent.Delivery.StrongTyping;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kentico.Kontent.Delivery.Rx.Tests
{
    public class DeliveryObservableProxyTests
    {
        private const string BEVERAGES_IDENTIFIER = "coffee_beverages_explained";
        readonly string guid = string.Empty;
        readonly string baseUrl = string.Empty;
        readonly MockHttpMessageHandler mockHttp;

        public DeliveryObservableProxyTests()
        {
            guid = Guid.NewGuid().ToString();
            baseUrl = $"https://deliver.kontent.ai/{guid}";
            mockHttp = new MockHttpMessageHandler();
        }

        [Fact]
        public async void ItemJsonRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockItem)).GetItemJsonObservable(BEVERAGES_IDENTIFIER, "language=es-ES");
            var itemJson = await observable.FirstOrDefaultAsync();

            Assert.Single(observable.ToEnumerable());
            Assert.NotNull(itemJson);
        }

        [Fact]
        public void ItemsJsonRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockItems)).GetItemsJsonObservable("limit=2", "skip=1");
            var itemsJson = observable.ToEnumerable().ToList();

            Assert.NotEmpty(itemsJson);
            Assert.Equal(2, itemsJson[0]["items"].Count());
        }

        [Fact]
        public async void ContentItemRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockItem)).GetItemObservable(BEVERAGES_IDENTIFIER, new LanguageParameter("es-ES"));
            var item = await observable.FirstOrDefaultAsync();

            Assert.NotNull(item);
            AssertItemPropertiesNotNull(item);
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
        public void ContentItemsRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockItems)).GetItemsObservable(new LimitParameter(2), new SkipParameter(1));
            var items = observable.ToEnumerable().ToList();

            Assert.NotEmpty(items);
            Assert.Equal(2, items.Count);
            Assert.All(items, item => AssertItemPropertiesNotNull(item));
        }

        [Fact]
        public void ContentItemsFeedRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockFeedItems)).GetItemsFeedObservable();
            var items = observable.ToEnumerable().ToList();

            Assert.NotEmpty(items);
            Assert.Equal(2, items.Count);
            Assert.All(items, item => AssertItemPropertiesNotNull(item));
        }

        [Fact]
        public void TypedItemsRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockArticles)).GetItemsObservable<Article>(new ContainsFilter("elements.personas", "barista"));
            var items = observable.ToEnumerable().ToList();

            Assert.NotEmpty(items);
            Assert.Equal(6, items.Count);
            Assert.All(items, article => AssertArticlePropertiesNotNull(article));
        }

        [Fact]
        public void TypedItemsFeedRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockFeedArticles)).GetItemsFeedObservable<Article>(new ContainsFilter("elements.personas", "barista"));
            var items = observable.ToEnumerable().ToList();

            Assert.NotEmpty(items);
            Assert.Equal(6, items.Count);
            Assert.All(items, article => AssertArticlePropertiesNotNull(article));
        }

        [Fact]
        public void RuntimeTypedItemsRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockArticles)).GetItemsObservable<Article>(new ContainsFilter("elements.personas", "barista"));
            var articles = observable.ToEnumerable().ToList();

            Assert.NotEmpty(articles);
            Assert.All(articles, article => Assert.IsType<Article>(article));
            Assert.All(articles, article => AssertArticlePropertiesNotNull(article));
        }

        [Fact]
        public void RuntimeTypedItemsFeedRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockFeedArticles)).GetItemsFeedObservable<Article>(new ContainsFilter("elements.personas", "barista"));
            var articles = observable.ToEnumerable().ToList();

            Assert.NotEmpty(articles);
            Assert.All(articles, article => Assert.IsType<Article>(article));
            Assert.All(articles, article => AssertArticlePropertiesNotNull(article));
        }

        [Fact]
        public async void TypeJsonRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockType)).GetTypeJsonObservable(Article.Codename);
            var type = await observable.FirstOrDefaultAsync();

            Assert.Single(observable.ToEnumerable());
            Assert.NotNull(type);
        }

        [Fact]
        public void TypesJsonRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockTypes)).GetTypesJsonObservable("skip=2");
            var types = observable.ToEnumerable().ToList();

            Assert.NotEmpty(types);
            Assert.Equal(13, types[0]["types"].Count());
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
            Assert.All(types, type => Assert.NotNull(type));
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
            Assert.NotNull(element.Options);
            Assert.NotNull(element.TaxonomyGroup);
            Assert.NotNull(element.Type);
        }

        [Fact]
        public async void TaxonomyJsonRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockTaxonomy)).GetTaxonomyJsonObservable("personas");
            var taxonomyJson = await observable.FirstOrDefaultAsync();

            Assert.NotNull(taxonomyJson);
            Assert.NotNull(taxonomyJson["system"]);
            Assert.NotNull(taxonomyJson["terms"]);
        }

        [Fact]
        public void TaxonomiesJsonRetrieved()
        {
            var observable = new DeliveryObservableProxy(GetDeliveryClient(MockTaxonomies)).GetTaxonomiesJsonObservable("skip=1");
            var taxonomiesJson = observable.ToEnumerable().ToList();

            Assert.NotNull(taxonomiesJson);
            Assert.NotNull(taxonomiesJson[0]["taxonomies"]);
            Assert.NotNull(taxonomiesJson[0]["pagination"]);
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

        private IDeliveryClient GetDeliveryClient(Action mockAction)
        {
            mockAction();
            var httpClient = mockHttp.ToHttpClient();
            var deliveryOptions = new OptionsWrapper<DeliveryOptions>(new DeliveryOptions { ProjectId = guid });
            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var contentItemsProcessor = A.Fake<IInlineContentItemsProcessor>();
            var contentPropertyMapper =  new PropertyMapper();
            var contentTypeProvider = new CustomTypeProvider();
            var modelProvider = new ModelProvider(contentLinkUrlResolver, contentItemsProcessor, contentTypeProvider, contentPropertyMapper);
            var retryPolicy = A.Fake<IRetryPolicy>();
            var retryPolicyProvider = A.Fake<IRetryPolicyProvider>();
            A.CallTo(() => retryPolicyProvider.GetRetryPolicy()).Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(call => call.GetArgument<Func<Task<HttpResponseMessage>>>(0)());
            var client = new DeliveryClient(
                deliveryOptions,
                httpClient,
                contentLinkUrlResolver, 
                null,
                modelProvider,
                retryPolicyProvider,
                contentTypeProvider
            );

            return client;
        }

        private void MockItem()
        {
            mockHttp.When($"{baseUrl}/items/{BEVERAGES_IDENTIFIER}?language=es-ES")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));
        }

        private void MockItems()
        {
            mockHttp.When($"{baseUrl}/items")
                .WithQueryString("limit=2&skip=1")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}items.json")));
        }

        private void MockFeedItems()
        {
            mockHttp.When($"{baseUrl}/items-feed")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}items.json")));
        }

        private void MockArticles()
        {
            mockHttp.When($"{baseUrl}/items")
                .WithQueryString(new[] { new KeyValuePair<string, string>("system.type", Article.Codename), new KeyValuePair<string, string>("elements.personas[contains]", "barista") })
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}articles.json")));
        }

        private void MockFeedArticles()
        {
            mockHttp.When($"{baseUrl}/items-feed")
                .WithQueryString(new[] { new KeyValuePair<string, string>("system.type", Article.Codename), new KeyValuePair<string, string>("elements.personas[contains]", "barista") })
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}articles.json")));
        }

        private void MockType()
        {
            mockHttp.When($"{baseUrl}/types/{Article.Codename}")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}article-type.json")));
        }

        private void MockTypes()
        {
            mockHttp.When($"{baseUrl}/types?skip=2")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}types.json")));
        }

        private void MockElement()
        {
            mockHttp.When($"{baseUrl}/types/{Article.Codename}/elements/{Article.TitleCodename}")
                .Respond("application/json", "{'type':'text','name':'Title','codename':'title'}");
        }

        private void MockTaxonomy()
        {
            mockHttp.When($"{baseUrl}/taxonomies/personas")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}taxonomies_personas.json")));
        }

        private void MockTaxonomies()
        {
            mockHttp.When($"{baseUrl}/taxonomies")
                .WithQueryString("skip=1")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}taxonomies_multiple.json")));
        }

        private static void AssertItemPropertiesNotNull(ContentItem item)
        {
            Assert.NotNull(item.System);
            Assert.NotNull(item.Elements);
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
