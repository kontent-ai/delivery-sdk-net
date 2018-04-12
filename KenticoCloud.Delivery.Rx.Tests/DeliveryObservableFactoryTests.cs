using System;
using System.Collections;
using System.Collections.Generic;
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
            var observable = DeliveryObservableFactory.ItemJson(GetDeliveryClient(MockItem), BEVERAGES_IDENTIFIER, "language=es-ES");
            var itemJson = await observable.FirstOrDefaultAsync();

            Assert.Single(observable.ToEnumerable());
            Assert.NotNull(itemJson);
        }

        [Fact]
        public void ItemsJsonRetrieved()
        {
            var observable = DeliveryObservableFactory.ItemsJson(GetDeliveryClient(MockItems), "limit=2", "skip=1");
            var itemsJson = observable.ToEnumerable().ToList();

            Assert.NotEmpty(itemsJson);
            Assert.InRange(itemsJson[0]["items"].Count(), 2, int.MaxValue);
        }

        [Fact]
        public async void ContentItemRetrieved()
        {
            var observable = DeliveryObservableFactory.Item(GetDeliveryClient(MockItem), BEVERAGES_IDENTIFIER, new LanguageParameter("es-ES"));
            var item = await observable.FirstOrDefaultAsync();

            Assert.NotNull(item);
            AssertItemPropertiesNotNull(item);
        }

        [Fact]
        public async void TypedItemRetrieved()
        {
            var observable = DeliveryObservableFactory.Item<Article>(GetDeliveryClient(MockItem), BEVERAGES_IDENTIFIER, new LanguageParameter("es-ES"));
            var item = await observable.FirstOrDefaultAsync();

            Assert.NotNull(item);
            AssertArticlePropertiesNotNull(item);
        }

        [Fact]
        public async void RuntimeTypedItemRetrieved()
        {
            var observable = DeliveryObservableFactory.Item<object>(GetDeliveryClient(MockItem), BEVERAGES_IDENTIFIER, new LanguageParameter("es-ES"));
            var item = await observable.FirstOrDefaultAsync();

            Assert.IsType<Article>(item);
            Assert.NotNull(item);
            AssertArticlePropertiesNotNull((Article)item);
        }

        [Fact]
        public void ContentItemsRetrieved()
        {
            var observable = DeliveryObservableFactory.Items(GetDeliveryClient(MockItems), new LimitParameter(2), new SkipParameter(1));
            var items = observable.ToEnumerable().ToList();

            Assert.NotEmpty(items);
            Assert.InRange(items.Count, 2, int.MaxValue);
            items.ForEach(i => AssertItemPropertiesNotNull(i));
        }

        [Fact]
        public void TypedItemsRetrieved()
        {
            var observable = DeliveryObservableFactory.Items<Article>(GetDeliveryClient(MockArticles), new ContainsFilter("elements.personas", "barista"));
            var items = observable.ToEnumerable().ToList();

            Assert.NotEmpty(items);
            Assert.InRange(items.Count, 2, int.MaxValue);
            items.ForEach(a => AssertArticlePropertiesNotNull(a));
        }

        [Fact]
        public void RuntimeTypedItemsRetrieved()
        {
            var observable = DeliveryObservableFactory.Items<Article>(GetDeliveryClient(MockArticles), new ContainsFilter("elements.personas", "barista"));
            var articles = observable.ToEnumerable().ToList();

            Assert.NotEmpty(articles);
            articles.ForEach(a => Assert.IsType<Article>(a));
            articles.ForEach(a => AssertArticlePropertiesNotNull(a));
        }

        [Fact]
        public async void TypeJsonRetrieved()
        {
            var observable = DeliveryObservableFactory.TypeJson(GetDeliveryClient(MockType), Article.Codename);
            var type = await observable.FirstOrDefaultAsync();

            Assert.Single(observable.ToEnumerable());
            Assert.NotNull(type);
        }

        [Fact]
        public void TypesJsonRetrieved()
        {
            var observable = DeliveryObservableFactory.TypesJson(GetDeliveryClient(MockTypes), "skip=2");
            var types = observable.ToEnumerable().ToList();

            Assert.NotEmpty(types);
            Assert.InRange(types[0]["types"].Count(), 2, int.MaxValue);
        }

        [Fact]
        public async void TypeRetrieved()
        {
            var observable = DeliveryObservableFactory.Type(GetDeliveryClient(MockType), Article.Codename);
            var type = await observable.FirstOrDefaultAsync();

            Assert.Single(observable.ToEnumerable());
            Assert.NotNull(type.System);
            Assert.NotEmpty(type.Elements);
        }

        [Fact]
        public void TypesRetrieved()
        {
            var observable = DeliveryObservableFactory.Types(GetDeliveryClient(MockTypes), new SkipParameter(2));
            var types = observable.ToEnumerable().ToList();

            Assert.NotEmpty(types);
            types.ForEach(t => Assert.NotNull(t.System));
            types.ForEach(t => Assert.NotEmpty(t.Elements));
        }

        [Fact]
        public async void ElementRetrieved()
        {
            var observable = DeliveryObservableFactory.Element(GetDeliveryClient(MockElement), Article.Codename, Article.TitleCodename);
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
            var observable = DeliveryObservableFactory.TaxonomyJson(GetDeliveryClient(MockTaxonomy), "personas");
            var taxonomyJson = await observable.FirstOrDefaultAsync();

            Assert.NotNull(taxonomyJson);
            Assert.NotNull(taxonomyJson["system"]);
            Assert.NotNull(taxonomyJson["terms"]);
        }

        [Fact]
        public void TaxonomiesJsonRetrieved()
        {
            var observable = DeliveryObservableFactory.TaxonomiesJson(GetDeliveryClient(MockTaxonomies), "skip=1");
            var taxonomiesJson = observable.ToEnumerable().ToList();

            Assert.NotNull(taxonomiesJson);
            Assert.NotNull(taxonomiesJson[0]["taxonomies"]);
            Assert.NotNull(taxonomiesJson[0]["pagination"]);
        }

        [Fact]
        public async void TaxonomyRetrieved()
        {
            var observable = DeliveryObservableFactory.Taxonomy(GetDeliveryClient(MockTaxonomy), "personas");
            var taxonomy = await observable.FirstOrDefaultAsync();

            Assert.NotNull(taxonomy);
            Assert.NotNull(taxonomy.System);
            Assert.NotNull(taxonomy.Terms);
        }

        [Fact]
        public void TaxonomiesRetrieved()
        {
            var observable = DeliveryObservableFactory.Taxonomies(GetDeliveryClient(MockTaxonomies), new SkipParameter(1));
            var taxonomies = observable.ToEnumerable().ToList();

            Assert.NotEmpty(taxonomies);
            taxonomies.ForEach(t => Assert.NotNull(t.System));
            taxonomies.ForEach(t => Assert.NotNull(t.Terms));
        }

        private IDeliveryClient GetDeliveryClient(Action mockAction)
        {
            mockAction();
            var httpClient = mockHttp.ToHttpClient();

            return new DeliveryClient(guid)
            {
                CodeFirstModelProvider = { TypeProvider = new CustomTypeProvider() },
                HttpClient = httpClient
            };
        }

        private void MockItem()
        {
            mockHttp.When($"{baseUrl}/items/{BEVERAGES_IDENTIFIER}?language=es-ES")
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
                .WithQueryString(new[] { new KeyValuePair<string, string>("system.type", Article.Codename), new KeyValuePair<string, string>("elements.personas[contains]", "barista") })
                .Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\articles.json")));
        }

        private void MockType()
        {
            mockHttp.When($"{baseUrl}/types/{Article.Codename}")
                .Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\article-type.json")));
        }

        private void MockTypes()
        {
            mockHttp.When($"{baseUrl}/types?skip=2")
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
