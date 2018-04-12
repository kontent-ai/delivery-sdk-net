using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

using RichardSzalay.MockHttp;
using Xunit;

namespace KenticoCloud.Delivery.Rx.Tests
{
    public class DeliveryObservableFactoryTests
    {
        private const string BEVERAGES_IDENTIFIER = "coffee_beverages_explained";
        readonly string guid = string.Empty;
        readonly string baseUrl = string.Empty;
        readonly MockHttpMessageHandler mockHttp;

        public DeliveryObservableFactoryTests()
        {
            guid = Guid.NewGuid().ToString();
            baseUrl = $"https://deliver.kenticocloud.com/{guid}";
            mockHttp = new MockHttpMessageHandler();
        }

        [Fact]
        public async void ItemJsonRetrieved()
        {
            var factory = GetObservableFactory(MockItem);
            var observable = factory.ItemJson(BEVERAGES_IDENTIFIER);
            var itemJson = await observable.FirstOrDefaultAsync();

            Assert.Single(observable.ToEnumerable());
            Assert.NotNull(itemJson);
        }

        [Fact]
        public void ItemsJsonRetrieved()
        {
            var factory = GetObservableFactory(MockItems);
            var observable = factory.ItemsJson("limit=2", "skip=1");
            var itemsJson = observable.ToEnumerable().ToList();

            Assert.NotEmpty(itemsJson);
            Assert.InRange(itemsJson[0]["items"].Count(), 2, int.MaxValue);
        }

        [Fact]
        public async void ContentItemRetrieved()
        {
            var factory = GetObservableFactory(MockItem);
            var observable = factory.Item(BEVERAGES_IDENTIFIER);
            var item = await observable.FirstOrDefaultAsync();

            Assert.NotNull(item);
            AssertItemPropertiesNotNull(item);
        }

        [Fact]
        public async void TypedItemRetrieved()
        {
            var factory = GetObservableFactory(MockItem);
            var observable = factory.Item<Article>(BEVERAGES_IDENTIFIER);
            var item = await observable.FirstOrDefaultAsync();

            Assert.NotNull(item);
            AssertArticlePropertiesNotNull(item);
        }

        [Fact]
        public async void RuntimeTypedItemRetrieved()
        {
            var factory = GetObservableFactory(MockItem);
            var observable = factory.Item<object>(BEVERAGES_IDENTIFIER);
            var item = await observable.FirstOrDefaultAsync();

            Assert.IsType<Article>(item);
            Assert.NotNull(item);
            AssertArticlePropertiesNotNull((Article)item);
        }

        [Fact]
        public void ContentItemsRetrieved()
        {
            var factory = GetObservableFactory(MockItems);
            var observable = factory.Items(new LimitParameter(2), new SkipParameter(1));
            var items = observable.ToEnumerable().ToList();

            Assert.NotEmpty(items);
            Assert.InRange(items.Count, 2, int.MaxValue);
            items.ForEach(i => AssertItemPropertiesNotNull(i));
        }

        [Fact]
        public void TypedItemsRetrieved()
        {
            var factory = GetObservableFactory(MockArticles);
            var observable = factory.Items<Article>();
            var items = observable.ToEnumerable().ToList();

            Assert.NotEmpty(items);
            Assert.InRange(items.Count, 2, int.MaxValue);
            items.ForEach(a => AssertArticlePropertiesNotNull(a));
        }

        [Fact]
        public void RuntimeTypedItemsRetrieved()
        {
            var factory = GetObservableFactory(MockArticles);
            var observable = factory.Items<Article>();
            var articles = observable.ToEnumerable().ToList();

            Assert.NotEmpty(articles);
            articles.ForEach(a => Assert.IsType<Article>(a));
            articles.ForEach(a => AssertArticlePropertiesNotNull(a));
        }

        [Fact]
        public async void TypeJsonRetrieved()
        {
            var factory = GetObservableFactory(MockType);
            var observable = factory.TypeJson(Article.Codename);
            var type = await observable.FirstOrDefaultAsync();

            Assert.Single(observable.ToEnumerable());
            Assert.NotNull(type);
        }

        [Fact]
        public void TypesJsonRetrieved()
        {
            var factory = GetObservableFactory(MockTypes);
            var observable = factory.TypesJson();
            var types = observable.ToEnumerable().ToList();

            Assert.NotEmpty(types);
            Assert.InRange(types[0]["types"].Count(), 2, int.MaxValue);
        }

        [Fact]
        public async void TypeRetrieved()
        {
            var factory = GetObservableFactory(MockType);
            var observable = factory.Type(Article.Codename);
            var type = await observable.FirstOrDefaultAsync();

            Assert.Single(observable.ToEnumerable());
            Assert.NotNull(type.System);
            Assert.NotEmpty(type.Elements);
        }

        [Fact]
        public void TypesRetrieved()
        {
            var factory = GetObservableFactory(MockTypes);
            var observable = factory.Types();
            var types = observable.ToEnumerable().ToList();

            Assert.NotEmpty(types);
            types.ForEach(t => Assert.NotNull(t.System));
            types.ForEach(t => Assert.NotEmpty(t.Elements));
        }

        [Fact]
        public async void ElementRetrieved()
        {
            var factory = GetObservableFactory(MockElement);
            var observable = factory.Element(Article.Codename, Article.TitleCodename);
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
            var factory = GetObservableFactory(MockTaxonomy);
            var observable = factory.TaxonomyJson("personas");
            var taxonomyJson = await observable.FirstOrDefaultAsync();

            Assert.NotNull(taxonomyJson);
            Assert.NotNull(taxonomyJson["system"]);
            Assert.NotNull(taxonomyJson["terms"]);
        }

        [Fact]
        public void TaxonomiesJsonRetrieved()
        {
            var factory = GetObservableFactory(MockTaxonomies);
            var observable = factory.TaxonomiesJson("skip=1");
            var taxonomiesJson = observable.ToEnumerable().ToList();

            Assert.NotNull(taxonomiesJson);
            Assert.NotNull(taxonomiesJson[0]["taxonomies"]);
            Assert.NotNull(taxonomiesJson[0]["pagination"]);
        }

        [Fact]
        public async void TaxonomyRetrieved()
        {
            var factory = GetObservableFactory(MockTaxonomy);
            var observable = factory.Taxonomy("personas");
            var taxonomy = await observable.FirstOrDefaultAsync();

            Assert.NotNull(taxonomy);
            Assert.NotNull(taxonomy.System);
            Assert.NotNull(taxonomy.Terms);
        }

        [Fact]
        public void TaxonomiesRetrieved()
        {
            var factory = GetObservableFactory(MockTaxonomies);
            var observable = factory.Taxonomies(new SkipParameter(1));
            var taxonomies = observable.ToEnumerable().ToList();

            Assert.NotEmpty(taxonomies);
            taxonomies.ForEach(t => Assert.NotNull(t.System));
            taxonomies.ForEach(t => Assert.NotNull(t.Terms));
        }

        private DeliveryObservableFactory GetObservableFactory(Action mockAction)
        {
            mockAction();
            var httpClient = mockHttp.ToHttpClient();

            var observableFactory = new DeliveryObservableFactory(guid)
            {
                DeliveryClient = new DeliveryClient(guid)
                {
                    CodeFirstModelProvider = { TypeProvider = new CustomTypeProvider() },
                    HttpClient = httpClient
                }
            };

            return observableFactory;
        }

        private void MockItem()
        {
            mockHttp.When($"{baseUrl}/items/{BEVERAGES_IDENTIFIER}")
                .Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\coffee_beverages_explained.json")));
        }

        private void MockItems()
        {
            mockHttp.When($"{baseUrl}/items")
                .WithQueryString("limit=2&skip=1")
                .Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\items.json")));
        }

        private void MockArticles()
        {
            mockHttp.When($"{baseUrl}/items")
                .WithQueryString($"system.type={Article.Codename}")
                .Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\articles.json")));
        }

        private void MockType()
        {
            mockHttp.When($"{baseUrl}/types/{Article.Codename}")
                .Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\articleType.json")));
        }

        private void MockTypes()
        {
            mockHttp.When($"{baseUrl}/types")
                .Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\types.json")));
        }

        private void MockElement()
        {
            mockHttp.When($"{baseUrl}/types/{Article.Codename}/elements/{Article.TitleCodename}")
                .Respond("application/json", "{'type':'text','name':'Title','codename':'title'}");
        }

        private void MockTaxonomy()
        {
            mockHttp.When($"{baseUrl}/taxonomies/personas")
                .Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\taxonomies_personas.json")));
        }

        private void MockTaxonomies()
        {
            mockHttp.When($"{baseUrl}/taxonomies")
                .WithQueryString("skip=1")
                .Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\taxonomies_multiple.json")));
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
