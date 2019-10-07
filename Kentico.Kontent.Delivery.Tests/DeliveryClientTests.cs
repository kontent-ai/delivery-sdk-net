using FakeItEasy;
using Kentico.Kontent.Delivery.Tests.Factories;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Kentico.Kontent.Delivery.RetryPolicy;
using Kentico.Kontent.Delivery.StrongTyping;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests
{
    public class DeliveryClientTests
    {
        private readonly Guid _guid;
        private readonly string _baseUrl;
        private readonly MockHttpMessageHandler _mockHttp;
        private readonly ITypeProvider _mockTypeProvider;
        private readonly IContentLinkUrlResolver _mockContentLinkUrlResolver;

        public DeliveryClientTests()
        {
            _guid = Guid.NewGuid();
            var projectId = _guid.ToString();
            _baseUrl = $"https://deliver.kontent.ai/{projectId}";
            _mockHttp = new MockHttpMessageHandler();
            _mockTypeProvider = A.Fake<ITypeProvider>();
            _mockContentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
        }

        [Fact]
        public async void GetItemAsync()
        {
            string url = $"{_baseUrl}/items/";

            _mockHttp
                .When($"{url}{"coffee_beverages_explained"}")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            _mockHttp
                .When($"{url}{"brazil_natural_barra_grande"}")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}brazil_natural_barra_grande.json")));

            _mockHttp
                .When($"{url}{"on_roasts"}")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}on_roasts.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var beveragesResponse = await client.GetItemAsync("coffee_beverages_explained");
            var beveragesItem = beveragesResponse.Item;
            var barraItem = (await client.GetItemAsync("brazil_natural_barra_grande")).Item;
            var roastsItem = (await client.GetItemAsync("on_roasts")).Item;

            Assert.Equal("article", beveragesItem.System.Type);
            Assert.Equal("en-US", beveragesItem.System.Language);
            Assert.NotEmpty(beveragesItem.System.SitemapLocation);
            Assert.NotEmpty(roastsItem.GetLinkedItems("related_articles"));
            Assert.Equal(beveragesItem.Elements.title.value.ToString(), beveragesItem.GetString("title"));
            Assert.Equal(beveragesItem.Elements.body_copy.value.ToString(), beveragesItem.GetString("body_copy"));
            Assert.Equal(DateTime.Parse(beveragesItem.Elements.post_date.value.ToString()), beveragesItem.GetDateTime("post_date"));
            Assert.Equal(beveragesItem.Elements.teaser_image.value.Count, beveragesItem.GetAssets("teaser_image").Count());
            Assert.Equal(beveragesItem.Elements.personas.value.Count, beveragesItem.GetTaxonomyTerms("personas").Count());
            Assert.Equal(decimal.Parse(barraItem.Elements.price.value.ToString()), barraItem.GetNumber("price"));
            Assert.Equal(barraItem.Elements.processing.value.Count, barraItem.GetOptions("processing").Count());
            Assert.NotNull(beveragesResponse.ApiUrl);
        }

        [Fact]
        public async void GetPagination()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .WithQueryString("limit=2&skip=1")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var articles = await client.GetItemsAsync(new LimitParameter(2), new SkipParameter(1));

            Assert.Equal(2, articles.Pagination.Count);
            Assert.Equal(1, articles.Pagination.Skip);
            Assert.Equal(2, articles.Pagination.Limit);
            Assert.NotNull(articles.Pagination.NextPageUrl);
        }

        [Fact]
        public async void AssetPropertiesNotEmpty()
        {
            _mockHttp
                .When($"{_baseUrl}/items/{"coffee_beverages_explained"}")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetItemAsync("coffee_beverages_explained");
            var beveragesItem = response.Item;

            var model = beveragesItem.CastTo<Article>();

            Assert.NotNull(model.TeaserImage.FirstOrDefault()?.Description);
            Assert.NotNull(model.TeaserImage.FirstOrDefault()?.Name);
            Assert.NotNull(model.TeaserImage.FirstOrDefault()?.Type);
            Assert.NotNull(model.TeaserImage.FirstOrDefault()?.Url);
            Assert.True(model.TeaserImage.FirstOrDefault()?.Width > 0);
            Assert.True(model.TeaserImage.FirstOrDefault()?.Height > 0);
            Assert.NotNull(response.ApiUrl);
        }

        [Fact]
        public async void IgnoredSerializationProperties()
        {
            _mockHttp
                .When($"{_baseUrl}/items/{"coffee_beverages_explained"}")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var beveragesItem = (await client.GetItemAsync("coffee_beverages_explained")).Item;

            var model = beveragesItem.CastTo<Article>();

            Assert.NotNull(model.TitleNotIgnored);
            Assert.Null(model.TitleIgnored);
        }

        [Fact]
        public async void GetItemAsync_NotFound()
        {
            string messsge = "{'message': 'The requested content item unscintillating_hemerocallidaceae_des_iroquois was not found.','request_id': '','error_code': 101,'specific_code': 0}";

            _mockHttp
                .When($"{_baseUrl}/items/unscintillating_hemerocallidaceae_des_iroquois")
                .Respond(HttpStatusCode.NotFound, "application/json", messsge);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetItemAsync("unscintillating_hemerocallidaceae_des_iroquois"));
        }

        [Fact]
        public async void GetItemsAsyncWithTypeExtractor()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .WithQueryString("system.type=cafe")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}allendale.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetItemsAsync<Cafe>();

            Assert.NotEmpty(response.Items);
        }

        [Fact]
        public async void GetItemsAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .WithQueryString("system.type=cafe")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}allendale.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetItemsAsync(new EqualsFilter("system.type", "cafe"));

            Assert.NotEmpty(response.Items);
        }

        [Fact]
        public void GetItemsFeed_DepthParameter_ThrowsArgumentException()
        {
           var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

           Assert.Throws<ArgumentException>(() => client.GetItemsFeed(new DepthParameter(2)));
        }

        [Fact]
        public void GetItemsFeed_LimitParameter_ThrowsArgumentException()
        {
            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            Assert.Throws<ArgumentException>(() => client.GetItemsFeed(new LimitParameter(2)));
        }

        [Fact]
        public void GetItemsFeed_SkipParameter_ThrowsArgumentException()
        {
            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            Assert.Throws<ArgumentException>(() => client.GetItemsFeed(new SkipParameter(2)));
        }

        [Fact]
        public async void GetItemsFeed_SingleBatch_FetchNextBatchAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type=article")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var feed = client.GetItemsFeed(new EqualsFilter("system.type", "article"));
            var items = new List<ContentItem>();
            var timesCalled = 0;
            while (feed.HasMoreResults)
            {
                timesCalled++;
                var response = await feed.FetchNextBatchAsync();
                items.AddRange(response);
            }

            Assert.Equal(6, items.Count);
            Assert.Equal(1, timesCalled);
        }

        [Fact]
        public async void GetItemsFeed_MultipleBatches_FetchNextBatchAsync()
        {
            // Second batch
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type=article")
                .WithHeaders("X-Continuation", "token")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed_2.json")));

            // First batch
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type=article")
                .Respond(new[] { new KeyValuePair<string, string>("X-Continuation", "token"), }, "application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed_1.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var feed = client.GetItemsFeed(new EqualsFilter("system.type", "article"));
            var items = new List<ContentItem>();
            var timesCalled = 0;
            while (feed.HasMoreResults)
            {
                timesCalled++;
                var response = await feed.FetchNextBatchAsync();
                items.AddRange(response);
            }

            Assert.Equal(6, items.Count);
            Assert.Equal(2, timesCalled);
        }

        [Fact]
        public async void GetTypeAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/types/article")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}article.json")));

            _mockHttp
                .When($"{_baseUrl}/types/coffee")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var articleType = (await client.GetTypeAsync("article")).Type;
            var coffeeType = (await client.GetTypeAsync("coffee")).Type;

            var taxonomyElement = articleType.Elements["personas"];
            var processingTaxonomyElement = coffeeType.Elements["processing"];

            Assert.Equal("article", articleType.System.Codename);
            Assert.Equal("text", articleType.Elements["title"].Type);
            Assert.Equal("rich_text", articleType.Elements["body_copy"].Type);
            Assert.Equal("date_time", articleType.Elements["post_date"].Type);
            Assert.Equal("asset", articleType.Elements["teaser_image"].Type);
            Assert.Equal("modular_content", articleType.Elements["related_articles"].Type);
            Assert.Equal("taxonomy", articleType.Elements["personas"].Type);

            Assert.Equal("number", coffeeType.Elements["price"].Type);
            Assert.Equal("taxonomy", coffeeType.Elements["processing"].Type);

            Assert.Equal("personas", taxonomyElement.TaxonomyGroup);
            Assert.Equal("processing", processingTaxonomyElement.TaxonomyGroup);
        }

        [Fact]
        public async void GetTypeAsync_NotFound()
        {
            string messsge = "{'message': 'The requested content type unequestrian_nonadjournment_sur_achoerodus was not found','request_id': '','error_code': 101,'specific_code': 0}";

            _mockHttp
                .When($"{_baseUrl}/types/unequestrian_nonadjournment_sur_achoerodus")
                .Respond(HttpStatusCode.NotFound, "application/json", messsge);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetTypeAsync("unequestrian_nonadjournment_sur_achoerodus"));
        }

        [Fact]
        public async void GetTypesAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/types")
                .WithQueryString("skip=1")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetTypesAsync(new SkipParameter(1));

            Assert.NotNull(response.ApiUrl);
            Assert.NotEmpty(response.Types);
        }

        [Fact]
        public async void GetContentElementAsync()
        {
            string url = $"{_baseUrl}/types";

            _mockHttp
                .When($"{url}/{Article.Codename}/elements/{Article.TitleCodename}")
                .Respond("application/json", "{'type':'text','name':'Title','codename':'title'}");
            _mockHttp
                .When($"{url}/{Article.Codename}/elements/{Article.PersonasCodename}")
                .Respond("application/json", "{'type':'taxonomy','name':'Personas','codename':'Personas','taxonomy_group':'personas'}");
            _mockHttp
                .When($"{url}/{Coffee.Codename}/elements/{Coffee.ProcessingCodename}")
                .Respond("application/json", "{'type':'taxonomy','name':'Processing','taxonomy_group':'processing','codename':'processing'}");

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var element = (await client.GetContentElementAsync(Article.Codename, Article.TitleCodename)).Element;
            var personasTaxonomyElement = (await client.GetContentElementAsync(Article.Codename, Article.PersonasCodename)).Element;
            var processingTaxonomyElement = (await client.GetContentElementAsync(Coffee.Codename, Coffee.ProcessingCodename)).Element;

            Assert.Equal(Article.TitleCodename, element.Codename);
            Assert.Equal(Article.PersonasCodename, personasTaxonomyElement.TaxonomyGroup);
            Assert.Equal(Coffee.ProcessingCodename, processingTaxonomyElement.TaxonomyGroup);
        }

        [Fact]
        public async void GetContentElementsAsync_NotFound()
        {
            string url = $"{_baseUrl}/types/anticommunistical_preventure_sur_helxine/elements/unlacerated_topognosis_sur_nonvigilantness";

            string messsge = "{'message': 'The requested content type anticommunistical_preventure_sur_helxine was not found.','request_id': '','error_code': 101,'specific_code': 0}";
            _mockHttp
                .When($"{url}")
                .Respond(HttpStatusCode.NotFound, "application/json", messsge);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetContentElementAsync("anticommunistical_preventure_sur_helxine", "unlacerated_topognosis_sur_nonvigilantness"));
        }

        [Fact]
        public async void GetTaxonomyAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/taxonomies/personas")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var taxonomy = (await client.GetTaxonomyAsync("personas")).Taxonomy;
            var personasTerms = taxonomy.Terms.ToList();
            var coffeeExpertTerms = personasTerms[0].Terms.ToList();

            Assert.Equal("personas", taxonomy.System.Codename);
            Assert.Equal("Personas", taxonomy.System.Name);
            Assert.Equal("coffee_expert", personasTerms[0].Codename);
            Assert.Equal("Coffee expert", personasTerms[0].Name);
            Assert.Equal("cafe_owner", coffeeExpertTerms[1].Codename);
            Assert.Equal("Cafe owner", coffeeExpertTerms[1].Name);
        }

        [Fact]
        public async void GetTaxonomyAsync_NotFound()
        {
            string url = $"{_baseUrl}/taxonomies/unequestrian_nonadjournment_sur_achoerodus";
            _mockHttp
                .When($"{url}")
                .Respond(HttpStatusCode.NotFound, "application/json", "{'message':'The requested taxonomy group unequestrian_nonadjournment_sur_achoerodus was not found.'}");

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetTaxonomyAsync("unequestrian_nonadjournment_sur_achoerodus"));
        }

        [Fact]
        public async void GetTaxonomiesAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/taxonomies")
                .WithQueryString("skip=1")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetTaxonomiesAsync(new SkipParameter(1));

            Assert.NotNull(response.ApiUrl);
            Assert.NotEmpty(response.Taxonomies);
        }

        [Fact]
        public async void QueryParameters()
        {

            string url = $"{_baseUrl}/items?elements.personas%5Ball%5D=barista%2Ccoffee%2Cblogger&elements.personas%5Bany%5D=barista%2Ccoffee%2Cblogger&system.sitemap_locations%5Bcontains%5D=cafes&elements.product_name=Hario%20V60&elements.price%5Bgt%5D=1000&elements.price%5Bgte%5D=50&system.type%5Bin%5D=cafe%2Ccoffee&elements.price%5Blt%5D=10&elements.price%5Blte%5D=4&elements.country%5Brange%5D=Guatemala%2CNicaragua&depth=2&elements=price%2Cproduct_name&limit=10&order=elements.price%5Bdesc%5D&skip=2&language=en";
            _mockHttp
                .When($"{url}")
                .Respond("application/json", " { 'items': [],'modular_content': {},'pagination': {'skip': 2,'limit': 10,'count': 0,'next_page': ''}}");

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var parameters = new IQueryParameter[]
           {
                new AllFilter("elements.personas", "barista", "coffee", "blogger"),
                new AnyFilter("elements.personas", "barista", "coffee", "blogger"),
                new ContainsFilter("system.sitemap_locations", "cafes"),
                new EqualsFilter("elements.product_name", "Hario V60"),
                new GreaterThanFilter("elements.price", "1000"),
                new GreaterThanOrEqualFilter("elements.price", "50"),
                new InFilter("system.type", "cafe", "coffee"),
                new LessThanFilter("elements.price", "10"),
                new LessThanOrEqualFilter("elements.price", "4"),
                new RangeFilter("elements.country", "Guatemala", "Nicaragua"),
                new DepthParameter(2),
                new ElementsParameter("price", "product_name"),
                new LimitParameter(10),
                new OrderParameter("elements.price", SortOrder.Descending),
                new SkipParameter(2),
                new LanguageParameter("en")
           };

            var response = await client.GetItemsAsync(parameters);

            Assert.Equal(0, response.Items.Count);
        }

        [Fact]
        public async void GetStrongTypesWithLimitedDepth()
        {
            _mockHttp
                .When($"{_baseUrl}/items/on_roasts")
                .WithQueryString("depth=1")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}on_roasts.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            // Returns on_roasts content item with related_articles linked item to two other articles.
            // on_roasts
            // |- coffee_processing_techniques
            // |- origins_of_arabica_bourbon
            //   |- on_roasts
            var onRoastsItem = (await client.GetItemAsync<Article>("on_roasts", new DepthParameter(1))).Item;

            Assert.NotNull(onRoastsItem.TeaserImage.First().Description);
            Assert.Equal(2, onRoastsItem.RelatedArticles.Count());
            Assert.Empty(((Article)onRoastsItem.RelatedArticles.First()).RelatedArticles);
            Assert.Empty(((Article)onRoastsItem.RelatedArticles.ElementAt(1)).RelatedArticles);
        }

        [Fact]
        public async void RecursiveLinkedItems()
        {
            _mockHttp
                .When($"{_baseUrl}/items/on_roasts")
                .WithQueryString("depth=15")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}onroast_recursive_linked_items.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            // Try to get recursive linked items on_roasts -> item -> on_roasts
            var article = await client.GetItemAsync<Article>("on_roasts", new DepthParameter(15));

            Assert.NotNull(article.Item);
        }

        [Fact]
        public async void RecursiveInlineLinkedItems()
        {
            _mockHttp
                .When($"{_baseUrl}/items/on_roasts")
                .WithQueryString("depth=15")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}onroast_recursive_inline_linked_items.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var article = await client.GetItemAsync<Article>("on_roasts", new DepthParameter(15));

            Assert.NotNull(article.Item.BodyCopyRichText);
            Assert.IsType<InlineContentItem>(article.Item.BodyCopyRichText.First());
        }

        [Fact]
        public void GetStronglyTypedResponse()
        {
            _mockHttp
                .When($"{_baseUrl}/items/complete_content_item")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}complete_content_item.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = client.GetItemAsync<CompleteContentItemModel>("complete_content_item").Result;
            CompleteContentItemModel item = response.Item;

            // Assert
            Assert.Equal("Text field value", item.TextField);

            Assert.Equal("<p>Rich text field value</p>", item.RichTextField);

            Assert.Equal(99, item.NumberField);

            Assert.Single(item.MultipleChoiceFieldAsRadioButtons);
            Assert.Equal("Radio button 1", item.MultipleChoiceFieldAsRadioButtons.First().Name);

            Assert.Equal(2, item.MultipleChoiceFieldAsCheckboxes.Count());
            Assert.Equal("Checkbox 1", item.MultipleChoiceFieldAsCheckboxes.First().Name);
            Assert.Equal("Checkbox 2", item.MultipleChoiceFieldAsCheckboxes.ElementAt(1).Name);

            Assert.Equal(new DateTime(2017, 2, 23), item.DateTimeField);

            Assert.Single(item.AssetField);
            Assert.Equal("Fire.jpg", item.AssetField.First().Name);
            Assert.Equal(129170, item.AssetField.First().Size);
            Assert.Equal(
                "https://assets.kenticocloud.com:443/e1167a11-75af-4a08-ad84-0582b463b010/64096741-b658-46ee-b148-b287fe03ea16/Fire.jpg",
                item.AssetField.First().Url);

            Assert.Single(item.LinkedItemsField);
            Assert.Equal("Homepage", item.LinkedItemsField.First().System.Name);

            Assert.Equal(2, item.CompleteTypeTaxonomy.Count());
            Assert.Equal("Option 1", item.CompleteTypeTaxonomy.First().Name);
            Assert.Equal("#d7e119", item.CustomElementField);
            Assert.NotNull(response.ApiUrl);
        }

        [Fact]
        public void GetStronglyTypedGenericWithAttributesResponse()
        {
            _mockHttp
                .When($"{_baseUrl}/items/complete_content_item")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}complete_content_item.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper());

            // Arrange
            A.CallTo(() => _mockTypeProvider.GetType("complete_content_type"))
                .ReturnsLazily(() => typeof(ContentItemModelWithAttributes));
            A.CallTo(() => _mockTypeProvider.GetType("homepage")).ReturnsLazily(() => typeof(Homepage));

            ContentItemModelWithAttributes item = (ContentItemModelWithAttributes)client.GetItemAsync<object>("complete_content_item").Result.Item;

            // Assert
            Assert.Equal("Text field value", item.TextFieldWithADifferentName);

            Assert.Equal("<p>Rich text field value</p>", item.RichTextFieldWithADifferentName);

            Assert.Equal(99, item.NumberFieldWithADifferentName);

            Assert.Single(item.MultipleChoiceFieldAsRadioButtonsWithADifferentName);
            Assert.Equal("Radio button 1", item.MultipleChoiceFieldAsRadioButtonsWithADifferentName.First().Name);

            Assert.Equal(2, item.MultipleChoiceFieldAsCheckboxes.Count());
            Assert.Equal("Checkbox 1", item.MultipleChoiceFieldAsCheckboxes.First().Name);
            Assert.Equal("Checkbox 2", item.MultipleChoiceFieldAsCheckboxes.ElementAt(1).Name);

            Assert.Equal(new DateTime(2017, 2, 23), item.DateTimeFieldWithADifferentName);

            Assert.Single(item.AssetFieldWithADifferentName);
            Assert.Equal("Fire.jpg", item.AssetFieldWithADifferentName.First().Name);
            Assert.Equal(129170, item.AssetFieldWithADifferentName.First().Size);
            Assert.Equal(
                "https://assets.kenticocloud.com:443/e1167a11-75af-4a08-ad84-0582b463b010/64096741-b658-46ee-b148-b287fe03ea16/Fire.jpg",
                item.AssetFieldWithADifferentName.First().Url);

            Assert.Single(item.LinkedItemsFieldWithADifferentName);
            Assert.Equal("Homepage", ((Homepage)item.LinkedItemsFieldWithADifferentName.First()).System.Name);
            Assert.Equal("Homepage", ((Homepage)item.LinkedItemsFieldWithACollectionTypeDefined.First()).System.Name);
            Assert.True(item.LinkedItemsFieldWithAGenericTypeDefined.First().CallToAction.Length > 0);

            Assert.Equal(2, item.CompleteTypeTaxonomyWithADifferentName.Count());
            Assert.Equal("Option 1", item.CompleteTypeTaxonomyWithADifferentName.First().Name);
        }



        [Fact]
        public void GetStronglyTypedItemsResponse()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .WithQueryString("system.type=complete_content_type")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}complete_content_item_system_type.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            // Arrange
            A.CallTo(() => _mockTypeProvider.GetType("complete_content_type"))
                .ReturnsLazily(() => typeof(ContentItemModelWithAttributes));
            A.CallTo(() => _mockTypeProvider.GetType("homepage")).ReturnsLazily(() => typeof(Homepage));

            IReadOnlyList<object> items = client.GetItemsAsync<object>(new EqualsFilter("system.type", "complete_content_type")).Result.Items;

            // Assert
            Assert.True(items.All(i => i.GetType() == typeof(ContentItemModelWithAttributes)));
        }

        [Fact]
        public void GetStronglyTypedItemsFeed_DepthParameter_ThrowsArgumentException()
        {
            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            Assert.Throws<ArgumentException>(() => client.GetItemsFeed<object>(new DepthParameter(2)));
        }

        [Fact]
        public void GetStronglyTypedItemsFeed_LimitParameter_ThrowsArgumentException()
        {
            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            Assert.Throws<ArgumentException>(() => client.GetItemsFeed<object>(new LimitParameter(2)));
        }

        [Fact]
        public void GetStronglyTypedItemsFeed_SkipParameter_ThrowsArgumentException()
        {
            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            Assert.Throws<ArgumentException>(() => client.GetItemsFeed<object>(new SkipParameter(2)));
        }

        [Fact]
        public async void GetStronglyTypedItemsFeed_SingleBatch_FetchNextBatchAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type=article&elements=title,summary,personas")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);
            A.CallTo(() => _mockTypeProvider.GetType("article"))
                .ReturnsLazily(() => typeof(ArticlePartialItemModel));

            var feed = client.GetItemsFeed<object>(new EqualsFilter("system.type", "article"), new ElementsParameter("title", "summary", "personas"));
            var items = new List<object>();
            var timesCalled = 0;
            while (feed.HasMoreResults)
            {
                timesCalled++;
                var response = await feed.FetchNextBatchAsync();
                items.AddRange(response);
            }

            Assert.Equal(6, items.Count);
            Assert.Equal(1, timesCalled);
            Assert.True(items.All(i => i.GetType() == typeof(ArticlePartialItemModel)));
        }

        [Fact]
        public async void GetStronglyTypedItemsFeed_MultipleBatches_FetchNextBatchAsync()
        {
            // Second batch
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type=article&elements=title,summary,personas")
                .WithHeaders("X-Continuation", "token")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed_2.json")));

            // First batch
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type=article&elements=title,summary,personas")
                .Respond(new[] { new KeyValuePair<string, string>("X-Continuation", "token"), }, "application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed_1.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);
            A.CallTo(() => _mockTypeProvider.GetType("article"))
                .ReturnsLazily(() => typeof(ArticlePartialItemModel));

            var feed = client.GetItemsFeed<object>(new EqualsFilter("system.type", "article"), new ElementsParameter("title", "summary", "personas"));
            var items = new List<object>();
            var timesCalled = 0;
            while (feed.HasMoreResults)
            {
                timesCalled++;
                var response = await feed.FetchNextBatchAsync();
                items.AddRange(response);
            }

            Assert.Equal(6, items.Count);
            Assert.Equal(2, timesCalled);
            Assert.True(items.All(i => i.GetType() == typeof(ArticlePartialItemModel)));
        }

        [Fact]
        public void CastResponse()
        {
            _mockHttp
                .When($"{_baseUrl}/items/complete_content_item")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}complete_content_item.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper());

            var response = client.GetItemAsync("complete_content_item").Result;
            var stronglyTypedResponse = response.CastTo<CompleteContentItemModel>();

            // Assert
            Assert.Equal("Text field value", stronglyTypedResponse.Item.TextField);
        }

        [Fact]
        public void CastListingResponse()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = client.GetItemsAsync().Result;
            var stronglyTypedListingResponse = response.CastTo<CompleteContentItemModel>();

            // Assert
            Assert.NotNull(stronglyTypedListingResponse);
            Assert.True(stronglyTypedListingResponse.Items.Any());
        }

        [Fact]
        public async void CastItemsFeedResponse()
        {
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type=article")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var feed = client.GetItemsFeed(new EqualsFilter("system.type", "article"));
            var response = await feed.FetchNextBatchAsync();
            var items = response.CastTo<ArticlePartialItemModel>();

            Assert.NotNull(items);
            Assert.Equal(6, items.Count());
        }

        [Fact]
        public void CastContentItem()
        {
            _mockHttp
                .When($"{_baseUrl}/items/complete_content_item")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}complete_content_item.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper());

            var item = client.GetItemAsync("complete_content_item").Result.Item;
            var stronglyTypedResponse = item.CastTo<CompleteContentItemModel>();

            // Assert
            Assert.Equal("Text field value", stronglyTypedResponse.TextField);
        }

        [Fact]
        public void CastContentItems()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            // Act
            DeliveryItemListingResponse response = client.GetItemsAsync().Result;
            IEnumerable<CompleteContentItemModel> list = response
                .Items
                .Where(i => i.System.Type == "complete_content_type")
                .Select(a => a.CastTo<CompleteContentItemModel>());

            // Assert
            Assert.True(list.Any());
        }

        [Fact]
        public void LongUrl()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            var client = DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(_guid, _mockHttp);
            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy())
                .Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            var elements = new ElementsParameter(Enumerable.Range(0, 1000).Select(i => "test").ToArray());
            var inFilter = new InFilter("test", Enumerable.Range(0, 1000).Select(i => "test").ToArray());
            var allFilter = new AllFilter("test", Enumerable.Range(0, 1000).Select(i => "test").ToArray());
            var anyFilter = new AnyFilter("test", Enumerable.Range(0, 1000).Select(i => "test").ToArray());

            // Act
            var response = client.GetItemsAsync(elements, inFilter, allFilter, anyFilter).Result;

            // Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async void TooLongUrlThrows()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            var client = DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(_guid, _mockHttp);

            var elements = new ElementsParameter(Enumerable.Range(0, 1000000).Select(i => "test").ToArray());

            // Act / Assert
            await Assert.ThrowsAsync<UriFormatException>(async () => await client.GetItemsAsync(elements));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async void PreviewAndSecuredProductionThrowsWhenBothEnabled(bool usePreviewApi,
            bool useSecuredProduction)
        {
            if (usePreviewApi)
            {
                _mockHttp
                    .When($@"https://preview-deliver.kontent.ai/{_guid}/items")
                    .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));
            }
            else
            {
                _mockHttp
                    .When($"{_baseUrl}/items")
                    .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));
            }

            var options = new DeliveryOptions
            {
                ProjectId = _guid.ToString(),
                UsePreviewApi = usePreviewApi,
                UseSecureAccess = useSecuredProduction,
                PreviewApiKey = "someKey",
                SecureAccessApiKey = "someKey"
            };

            var client = DeliveryClientFactory.GetMockedDeliveryClientWithOptions(options, _mockHttp);

            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy())
                .Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            if (usePreviewApi && useSecuredProduction)
            {
                // Assert
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetItemsAsync());
            }
            else
            {
                var response = await client.GetItemsAsync();

                // Assert
                Assert.NotNull(response);
            }
        }

        [Fact]
        public async void SecuredProductionAddCorrectHeader()
        {
            var securityKey = "someKey";
            var options = new DeliveryOptions
            {
                ProjectId = _guid.ToString(),
                SecureAccessApiKey = securityKey,
                UseSecureAccess = true
            };
            _mockHttp
                .Expect($"{_baseUrl}/items")
                .WithHeaders("Authorization", $"Bearer {securityKey}")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            var client = DeliveryClientFactory.GetMockedDeliveryClientWithOptions(options, _mockHttp);

            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy())
                .Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            await client.GetItemsAsync();
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async void CorrectSdkVersionHeaderAdded()
        {
            var assembly = typeof(DeliveryClient).Assembly;
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            var sdkVersion = fileVersionInfo.ProductVersion;
            var sdkPackageId = assembly.GetName().Name;

            _mockHttp
                .Expect($"{_baseUrl}/items")
                .WithHeaders("X-KC-SDKID", $"nuget.org;{sdkPackageId};{sdkVersion}")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            var client = DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(_guid, _mockHttp);

            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy())
                .Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            await client.GetItemsAsync();

            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async void GetItemAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/items/test")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetItemAsync("test");

            Assert.True(response.HasStaleContent);
        }

        [Fact]
        public async void GetItemAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/items/test")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetItemAsync("test");

            Assert.False(response.HasStaleContent);
        }

        [Fact]
        public async void GetItemsAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetItemsAsync();

            Assert.True(response.HasStaleContent);
        }

        [Fact]
        public async void GetItemsAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetItemsAsync();

            Assert.False(response.HasStaleContent);
        }

        [Fact]
        public async void GetCustomItemAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/items/test")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetItemAsync<Homepage>("test");

            Assert.True(response.HasStaleContent);
        }

        [Fact]
        public async void GetCustomItemAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/items/test")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetItemAsync<Homepage>("test");

            Assert.False(response.HasStaleContent);
        }

        [Fact]
        public async void GetCustomItemsAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetItemsAsync<Homepage>();

            Assert.True(response.HasStaleContent);
        }

        [Fact]
        public async void GetCustomItemsAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetItemsAsync<Homepage>();

            Assert.False(response.HasStaleContent);
        }

        [Fact]
        public async void GetTypeAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/types/test")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTypeAsync("test");

            Assert.True(response.HasStaleContent);
        }

        [Fact]
        public async void GetTypeAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/types/test")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTypeAsync("test");

            Assert.False(response.HasStaleContent);
        }

        [Fact]
        public async void GetTypesAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/types")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTypesAsync();

            Assert.True(response.HasStaleContent);
        }

        [Fact]
        public async void GetTypesAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/types")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTypesAsync();

            Assert.False(response.HasStaleContent);
        }

        [Fact]
        public async void GetTaxonomyAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/taxonomies/test")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTaxonomyAsync("test");

            Assert.True(response.HasStaleContent);
        }

        [Fact]
        public async void GetTaxonomyAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/taxonomies/test")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTaxonomyAsync("test");

            Assert.False(response.HasStaleContent);
        }

        [Fact]
        public async void GetTaxonomiesAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/taxonomies")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTaxonomiesAsync();

            Assert.True(response.HasStaleContent);
        }

        [Fact]
        public async void GetTaxonomiesAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/taxonomies")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTaxonomiesAsync();

            Assert.False(response.HasStaleContent);
        }

        [Fact]
        public async void GetElementAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/types/test/elements/test")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetContentElementAsync("test", "test");

            Assert.True(response.HasStaleContent);
        }

        [Fact]
        public async void GetElementAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/types/test/elements/test")
                .Respond(headers, "application/json", "{ }");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetContentElementAsync("test", "test");

            Assert.False(response.HasStaleContent);
        }

        [Fact]
        public async void RetryPolicy_WithDefaultOptions_Retries()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond((request) => new HttpResponseMessage(HttpStatusCode.RequestTimeout));
            var client = DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(_guid, _mockHttp);
            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy()).Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetItemsAsync());

            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async void RetryPolicy_Disabled_DoesNotRetry()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond(request => new HttpResponseMessage(HttpStatusCode.RequestTimeout));
            var options = new DeliveryOptions
            {
                ProjectId = _guid.ToString(),
                EnableRetryPolicy = false
            };
            var client = DeliveryClientFactory.GetMockedDeliveryClientWithOptions(options, _mockHttp);
            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy()).Returns(retryPolicy);

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetItemsAsync());

            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._)).MustNotHaveHappened();
        }

        [Fact]
        [Trait("Issue", "146")]
        public async void InitializeMultipleInlineContentItemsResolvers()
        {
            string url = $"{_baseUrl}/items/";
            const string tweetPrefix = "Tweet resolver: ";
            const string hostedVideoPrefix = "Video resolver: ";
            _mockHttp
                .When($"{url}{"coffee_beverages_explained"}")
                .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var deliveryClient = DeliveryClientBuilder
                .WithProjectId(_guid)
                .WithInlineContentItemsResolver(InlineContentItemsResolverFactory.Instance
                    .ResolveTo<Tweet>(tweet => tweetPrefix + tweet.TweetLink))
                .WithInlineContentItemsResolver(InlineContentItemsResolverFactory.Instance
                    .ResolveTo<HostedVideo>(video => hostedVideoPrefix + video.VideoHost.First().Name))
                .WithTypeProvider(new CustomTypeProvider())
                .WithHttpClient(_mockHttp.ToHttpClient())
                .Build();

            var article = await deliveryClient.GetItemAsync<Article>("coffee_beverages_explained");

            Assert.Contains(tweetPrefix, article.Item.BodyCopy);
            Assert.Contains(hostedVideoPrefix, article.Item.BodyCopy);
        }

        private DeliveryClient InitializeDeliveryClientWithACustomTypeProvider(MockHttpMessageHandler handler)
        {
            var customTypeProvider = new CustomTypeProvider();
            var modelProvider = new ModelProvider(
                _mockContentLinkUrlResolver,
                null,
                customTypeProvider,
                new PropertyMapper());
            var client = DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(
                _guid,
                handler,
                modelProvider,
                typeProvider: customTypeProvider);

            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy())
                .Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            return client;
        }

        private DeliveryClient InitializeDeliveryClientWithCustomModelProvider(MockHttpMessageHandler handler, IPropertyMapper propertyMapper = null)
        {
            var mapper = propertyMapper ?? A.Fake<IPropertyMapper>();
            var modelProvider = new ModelProvider(null, null, _mockTypeProvider, mapper);
            var client = DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(_guid, handler, modelProvider);

            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy())
                .Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            return client;
        }

        private HttpResponseMessage GetResponseAndLogRequest(HttpStatusCode httpStatusCode, ref int actualHttpRequestCount)
        {
            actualHttpRequestCount++;
            return new HttpResponseMessage(httpStatusCode);
        }
    }
}