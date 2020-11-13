using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FakeItEasy;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Kentico.Kontent.Delivery.ContentItems;
using Kentico.Kontent.Delivery.ContentItems.RichText.Blocks;
using Kentico.Kontent.Delivery.Extensions;
using Kentico.Kontent.Delivery.Tests.Factories;
using Kentico.Kontent.Delivery.Tests.Models;
using Kentico.Kontent.Delivery.Tests.Models.ContentTypes;
using Kentico.Kontent.Delivery.Urls.QueryParameters;
using Kentico.Kontent.Delivery.Urls.QueryParameters.Filters;
using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;
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
        public async Task GetItemAsync()
        {
            string url = $"{_baseUrl}/items/";

            _mockHttp
                .When($"{url}coffee_beverages_explained")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            _mockHttp
                .When($"{url}brazil_natural_barra_grande")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}brazil_natural_barra_grande.json")));

            _mockHttp
                .When($"{url}on_roasts")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}on_roasts.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var beveragesResponse = await client.GetItemAsync<Article>("coffee_beverages_explained");
            var beveragesItem = beveragesResponse.Item;
            var barraItem = (await client.GetItemAsync<Coffee>("brazil_natural_barra_grande")).Item;
            var roastsItem = (await client.GetItemAsync<Article>("on_roasts")).Item;

            Assert.Equal("article", beveragesItem.System.Type);
            Assert.Equal("en-US", beveragesItem.System.Language);
            Assert.NotEmpty(beveragesItem.System.SitemapLocation);
            Assert.NotEmpty(roastsItem.RelatedArticles);
            Assert.NotEmpty(roastsItem.RelatedArticlesInterface);
            Assert.NotEmpty(beveragesItem.Title);
            Assert.NotEmpty(beveragesItem.BodyCopy);
            Assert.True(beveragesItem.PostDate != null && beveragesItem.PostDate.Value != default);
            Assert.True(beveragesItem.TeaserImage.Any());
            Assert.True(beveragesItem.Personas.Any());
            Assert.True(barraItem.Price > 0);
            Assert.True(barraItem.Processing.Any());
            Assert.NotNull(beveragesResponse.ApiResponse.RequestUrl);
        }

        [Fact]
        public async Task GetItemWithImplementedTypes()
        {
            // Arrange
            _mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond("application/json",
                    await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));
            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            // Act
            var beveragesResponse = await client.GetItemAsync<ArticleWithImplementedTypes>("coffee_beverages_explained");

            // Assert
            Assert.NotEmpty(beveragesResponse.Item.TeaserImage);
            Assert.NotEmpty(beveragesResponse.Item.Personas);
        }

        [Fact]
        public async Task GetPagination()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .WithQueryString("limit=2&skip=1")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var articles = await client.GetItemsAsync<object>(new LimitParameter(2), new SkipParameter(1));

            Assert.Equal(2, articles.Pagination.Count);
            Assert.Equal(1, articles.Pagination.Skip);
            Assert.Equal(2, articles.Pagination.Limit);
            Assert.NotNull(articles.Pagination.NextPageUrl);
        }

        [Fact]
        public async Task AssetPropertiesNotEmpty()
        {
            _mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetItemAsync<Article>("coffee_beverages_explained");

            var model = response.Item;

            Assert.NotNull(model.TeaserImage.FirstOrDefault()?.Description);
            Assert.NotNull(model.TeaserImage.FirstOrDefault()?.Name);
            Assert.NotNull(model.TeaserImage.FirstOrDefault()?.Type);
            Assert.NotNull(model.TeaserImage.FirstOrDefault()?.Url);
            Assert.True(model.TeaserImage.FirstOrDefault()?.Width > 0);
            Assert.True(model.TeaserImage.FirstOrDefault()?.Height > 0);
            Assert.NotNull(response.ApiResponse.RequestUrl);
        }

        [Fact]
        public async Task IgnoredSerializationProperties()
        {
            _mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var beveragesItem = (await client.GetItemAsync<Article>("coffee_beverages_explained")).Item;

            Assert.NotNull(beveragesItem.TitleNotIgnored);
            Assert.Null(beveragesItem.TitleIgnored);
        }

        [Fact]
        public async Task GetItemAsync_NotFound()
        {
            string message = "{'message': 'The requested content item unscintillating_hemerocallidaceae_des_iroquois was not found.','request_id': '','error_code': 101,'specific_code': 0}";

            _mockHttp
                .When($"{_baseUrl}/items/unscintillating_hemerocallidaceae_des_iroquois")
                .Respond(HttpStatusCode.NotFound, "application/json", message);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetItemAsync<object>("unscintillating_hemerocallidaceae_des_iroquois"));
        }


        [Fact]
        public async Task GetItemAsync_ComplexRichTextTableCell_ParseCorrectly()
        {
            var mockedResponse = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}rich_text_complex_tables.json"));

            var expectedValue = JObject.Parse(mockedResponse).SelectToken("item.elements.rich_text.value").ToString();

            _mockHttp
                    .When($"{_baseUrl}/items/rich_text_complex_tables")
                    .Respond("application/json", mockedResponse);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);
            var typedResult = await client.GetItemAsync<SimpleRichText>("rich_text_complex_tables");

            Assert.Equal(expectedValue, typedResult.Item.RichTextString);
            Assert.Equal(9, typedResult.Item.RichText.Count());
            var tableBlock = typedResult.Item.RichText.ElementAt(4);
            Assert.NotNull(tableBlock);
            Assert.IsType<HtmlContent>(tableBlock);
        }

        [Fact]
        public async Task GetItemsAsyncWithTypeExtractor()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .WithQueryString("system.type=cafe")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}allendale.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetItemsAsync<Cafe>();

            Assert.NotEmpty(response.Items);
        }

        [Fact]
        public async Task GetItemsAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .WithQueryString("system.type%5Beq%5D=cafe")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}allendale.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetItemsAsync<object>(new EqualsFilter("system.type", "cafe"));

            Assert.NotEmpty(response.Items);
        }

        [Fact]
        public void GetItemsFeed_DepthParameter_ThrowsArgumentException()
        {
            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            Assert.Throws<ArgumentException>(() => client.GetItemsFeed<object>(new DepthParameter(2)));
        }

        [Fact]
        public void GetItemsFeed_LimitParameter_ThrowsArgumentException()
        {
            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            Assert.Throws<ArgumentException>(() => client.GetItemsFeed<object>(new LimitParameter(2)));
        }

        [Fact]
        public void GetItemsFeed_SkipParameter_ThrowsArgumentException()
        {
            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            Assert.Throws<ArgumentException>(() => client.GetItemsFeed<object>(new SkipParameter(2)));
        }

        [Fact]
        public async Task GetItemsFeed_SingleBatch_FetchNextBatchAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type=article")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var feed = client.GetItemsFeed<Article>();
            var items = new List<Article>();
            var timesCalled = 0;
            while (feed.HasMoreResults)
            {
                timesCalled++;
                var response = await feed.FetchNextBatchAsync();
                items.AddRange(response.Items);
            }

            Assert.Equal(6, items.Count);
            Assert.Equal(1, timesCalled);
        }

        [Fact]
        public async Task GetItemsFeed_MultipleBatches_FetchNextBatchAsync()
        {
            // Second batch
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type=article")
                .WithHeaders("X-Continuation", "token")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed_2.json")));

            // First batch
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type=article")
                .Respond(new[] { new KeyValuePair<string, string>("X-Continuation", "token"), }, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed_1.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var feed = client.GetItemsFeed<Article>();
            var items = new List<Article>();
            var timesCalled = 0;
            while (feed.HasMoreResults)
            {
                timesCalled++;
                var response = await feed.FetchNextBatchAsync();
                items.AddRange(response.Items);
            }

            Assert.Equal(6, items.Count);
            Assert.Equal(2, timesCalled);
        }

        [Fact]
        public async Task GetTypeAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/types/article")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}article.json")));

            _mockHttp
                .When($"{_baseUrl}/types/coffee")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee.json")));

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
            foreach (var element in articleType.Elements)
            {
                Assert.NotEmpty(element.Value.Codename);
            }

            Assert.Equal("number", coffeeType.Elements["price"].Type);
            Assert.Equal("taxonomy", coffeeType.Elements["processing"].Type);

            Assert.IsAssignableFrom<ITaxonomyElement>(taxonomyElement);
            Assert.Equal("personas", ((ITaxonomyElement)taxonomyElement).TaxonomyGroup);

            Assert.IsAssignableFrom<ITaxonomyElement>(processingTaxonomyElement);
            Assert.Equal("processing", ((ITaxonomyElement)processingTaxonomyElement).TaxonomyGroup);
        }

        [Fact]
        public async Task GetTypeAsync_NotFound()
        {
            string messsge = "{'message': 'The requested content type unequestrian_nonadjournment_sur_achoerodus was not found','request_id': '','error_code': 101,'specific_code': 0}";

            _mockHttp
                .When($"{_baseUrl}/types/unequestrian_nonadjournment_sur_achoerodus")
                .Respond(HttpStatusCode.NotFound, "application/json", messsge);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetTypeAsync("unequestrian_nonadjournment_sur_achoerodus"));
        }

        [Fact]
        public async Task GetTypesAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/types")
                .WithQueryString("skip=1")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetTypesAsync(new SkipParameter(1));

            Assert.NotNull(response.ApiResponse.RequestUrl);
            Assert.NotEmpty(response.Types);
        }

        [Fact]
        public async Task GetContentElementAsync()
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
            _mockHttp
                .When($"{url}/{Tweet.Codename}/elements/{Tweet.ThemeCodename}")
                .Respond("application/json", "{ 'type': 'multiple_choice', 'name': 'Theme', 'options': [ { 'name': 'Dark', 'codename': 'dark' }, { 'name': 'Light', 'codename': 'light' } ], 'codename': 'theme' }");

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var element = (await client.GetContentElementAsync(Article.Codename, Article.TitleCodename)).Element;
            var personasTaxonomyElement = (await client.GetContentElementAsync(Article.Codename, Article.PersonasCodename)).Element;
            var processingTaxonomyElement = (await client.GetContentElementAsync(Coffee.Codename, Coffee.ProcessingCodename)).Element;
            var themeMultipleChoiceElement = (await client.GetContentElementAsync(Tweet.Codename, Tweet.ThemeCodename)).Element;

            Assert.IsAssignableFrom<IContentElement>(element);
            Assert.Equal(Article.TitleCodename, element.Codename);

            Assert.IsAssignableFrom<ITaxonomyElement>(personasTaxonomyElement);
            Assert.Equal(Article.PersonasCodename, ((ITaxonomyElement)personasTaxonomyElement).TaxonomyGroup);

            Assert.IsAssignableFrom<ITaxonomyElement>(processingTaxonomyElement);
            Assert.Equal(Coffee.ProcessingCodename, ((ITaxonomyElement)processingTaxonomyElement).TaxonomyGroup);

            Assert.IsAssignableFrom<IMultipleChoiceElement>(themeMultipleChoiceElement);
            Assert.NotEmpty(((IMultipleChoiceElement)themeMultipleChoiceElement).Options);
        }

        [Fact]
        public async Task GetContentElementsAsync_NotFound()
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
        public async Task GetTaxonomyAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/taxonomies/personas")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json")));

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
        public async Task GetTaxonomyAsync_NotFound()
        {
            string url = $"{_baseUrl}/taxonomies/unequestrian_nonadjournment_sur_achoerodus";
            _mockHttp
                .When($"{url}")
                .Respond(HttpStatusCode.NotFound, "application/json", "{'message':'The requested taxonomy group unequestrian_nonadjournment_sur_achoerodus was not found.'}");

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetTaxonomyAsync("unequestrian_nonadjournment_sur_achoerodus"));
        }

        [Fact]
        public async Task GetTaxonomiesAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/taxonomies")
                .WithQueryString("skip=1")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetTaxonomiesAsync(new SkipParameter(1));

            Assert.NotNull(response.ApiResponse.RequestUrl);
            Assert.NotEmpty(response.Taxonomies);
        }

        [Fact]
        public async Task QueryParameters()
        {
            string url = $"{_baseUrl}/items?elements.personas%5Ball%5D=barista%2Ccoffee%2Cblogger&elements.personas%5Bany%5D=barista%2Ccoffee%2Cblogger&system.sitemap_locations%5Bcontains%5D=cafes&elements.product_name%5Beq%5D=Hario%20V60&elements.product_name%5Bneq%5D=Hario%20V42&elements.price%5Bgt%5D=1000&elements.price%5Bgte%5D=50&system.type%5Bin%5D=cafe%2Ccoffee&system.type%5Bnin%5D=article%2Cblog_post&elements.price%5Blt%5D=10&elements.price%5Blte%5D=4&elements.country%5Brange%5D=Guatemala%2CNicaragua&elements.price%5Bempty%5D&elements.country%5Bnempty%5D&depth=2&elements=price%2Cproduct_name&limit=10&order=elements.price%5Bdesc%5D&skip=2&language=en&includeTotalCount";
            _mockHttp
                .When($"{url}")
                .Respond("application/json", " { 'items': [],'modular_content': {},'pagination': {'skip': 2,'limit': 10,'count': 0, 'total_count': 0, 'next_page': ''}}");

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var parameters = new IQueryParameter[]
           {
                new AllFilter("elements.personas", "barista", "coffee", "blogger"),
                new AnyFilter("elements.personas", "barista", "coffee", "blogger"),
                new ContainsFilter("system.sitemap_locations", "cafes"),
                new EqualsFilter("elements.product_name", "Hario V60"),
                new NotEqualsFilter("elements.product_name", "Hario V42"),
                new GreaterThanFilter("elements.price", "1000"),
                new GreaterThanOrEqualFilter("elements.price", "50"),
                new InFilter("system.type", "cafe", "coffee"),
                new NotInFilter("system.type", "article", "blog_post"),
                new LessThanFilter("elements.price", "10"),
                new LessThanOrEqualFilter("elements.price", "4"),
                new RangeFilter("elements.country", "Guatemala", "Nicaragua"),
                new EmptyFilter("elements.price"),
                new NotEmptyFilter("elements.country"),
                new DepthParameter(2),
                new ElementsParameter("price", "product_name"),
                new LimitParameter(10),
                new OrderParameter("elements.price", SortOrder.Descending),
                new SkipParameter(2),
                new LanguageParameter("en"),
                new IncludeTotalCountParameter()
           };

            var response = await client.GetItemsAsync<object>(parameters);

            Assert.Equal(0, response.Items.Count);
        }

        [Fact]
        public async Task GetStrongTypesWithLimitedDepth()
        {
            _mockHttp
                .When($"{_baseUrl}/items/on_roasts")
                .WithQueryString("depth=1")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}on_roasts.json")));

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
        public async Task RecursiveLinkedItems()
        {
            _mockHttp
                .When($"{_baseUrl}/items/on_roasts")
                .WithQueryString("depth=15")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}onroast_recursive_linked_items.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            // Try to get recursive linked items on_roasts -> item -> on_roasts
            var article = await client.GetItemAsync<Article>("on_roasts", new DepthParameter(15));

            Assert.NotNull(article.Item);
        }

        [Fact]
        public async Task RecursiveInlineLinkedItems()
        {
            _mockHttp
                .When($"{_baseUrl}/items/on_roasts")
                .WithQueryString("depth=15")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}onroast_recursive_inline_linked_items.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var article = await client.GetItemAsync<Article>("on_roasts", new DepthParameter(15));

            Assert.NotNull(article.Item.BodyCopyRichText);
            Assert.IsType<InlineContentItem>(article.Item.BodyCopyRichText.First());
        }

        [Fact]
        public async Task GetStronglyTypedResponse()
        {
            _mockHttp
                .When($"{_baseUrl}/items/complete_content_item")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}complete_content_item.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetItemAsync<CompleteContentItemModel>("complete_content_item");
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
            Assert.NotNull(response.ApiResponse.RequestUrl);
        }

        [Fact]
        public async Task GetStronglyTypedGenericWithAttributesResponse()
        {
            _mockHttp
                .When($"{_baseUrl}/items/complete_content_item")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}complete_content_item.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper());

            // Arrange
            A.CallTo(() => _mockTypeProvider.GetType("complete_content_type"))
                .ReturnsLazily(() => typeof(ContentItemModelWithAttributes));
            A.CallTo(() => _mockTypeProvider.GetType("homepage")).ReturnsLazily(() => typeof(Homepage));

            ContentItemModelWithAttributes item = (ContentItemModelWithAttributes)(await client.GetItemAsync<object>("complete_content_item")).Item;

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
        public async Task GetStronglyTypedItemsResponse()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .WithQueryString("system.type%5Beq%5D=complete_content_type")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}complete_content_item_system_type.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            // Arrange
            A.CallTo(() => _mockTypeProvider.GetType("complete_content_type"))
                .ReturnsLazily(() => typeof(ContentItemModelWithAttributes));
            A.CallTo(() => _mockTypeProvider.GetType("homepage")).ReturnsLazily(() => typeof(Homepage));

            IList<object> items = (await client.GetItemsAsync<object>(new EqualsFilter("system.type", "complete_content_type"))).Items;

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
        public async Task GetStronglyTypedItemsFeed_SingleBatch_FetchNextBatchAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type%5Beq%5D=article&elements=title,summary,personas")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed.json")));

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
                items.AddRange(response.Items);
            }

            Assert.Equal(6, items.Count);
            Assert.Equal(1, timesCalled);
            Assert.True(items.All(i => i.GetType() == typeof(ArticlePartialItemModel)));
        }

        [Fact]
        public async Task GetStronglyTypedItemsFeed_MultipleBatches_FetchNextBatchAsync()
        {
            // Second batch
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type%5Beq%5D=article&elements=title,summary,personas")
                .WithHeaders("X-Continuation", "token")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed_2.json")));

            // First batch
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type%5Beq%5D=article&elements=title,summary,personas")
                .Respond(new[] { new KeyValuePair<string, string>("X-Continuation", "token"), }, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed_1.json")));

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
                items.AddRange(response.Items);
            }

            Assert.Equal(6, items.Count);
            Assert.Equal(2, timesCalled);
            Assert.True(items.All(i => i.GetType() == typeof(ArticlePartialItemModel)));
        }

        [Fact]
        public async Task CastResponse()
        {
            _mockHttp
                .When($"{_baseUrl}/items/complete_content_item")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}complete_content_item.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var response = await client.GetItemAsync<CompleteContentItemModel>("complete_content_item");

            // Assert
            Assert.Equal("Text field value", response.Item.TextField);
        }

        [Fact]
        public async Task CastListingResponse()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var response = await client.GetItemsAsync<CompleteContentItemModel>();

            // Assert
            Assert.NotNull(response);
            Assert.True(response.Items.Any());
        }

        [Fact]
        public async Task CastItemsFeedResponse()
        {
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type%5Beq%5D=article")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var feed = client.GetItemsFeed<ArticlePartialItemModel>(new EqualsFilter("system.type", "article"));
            var response = await feed.FetchNextBatchAsync();

            Assert.NotNull(response.Items);
            Assert.Equal(6, response.Items.Count);
        }

        [Fact]
        public async Task CastContentItem()
        {
            _mockHttp
                .When($"{_baseUrl}/items/complete_content_item")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}complete_content_item.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var item = await client.GetItemAsync<CompleteContentItemModel>("complete_content_item");

            // Assert
            Assert.Equal("Text field value", item.Item.TextField);
        }

        [Fact]
        public async Task CastContentItems()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            // Act
            var response = await client.GetItemsAsync<CompleteContentItemModel>();
            IEnumerable<CompleteContentItemModel> list = response
                .Items
                .Where(i => i.System.Type == "complete_content_type");

            // Assert
            Assert.True(list.Any());
        }

        [Fact]
        public async Task LongUrl()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(_guid, _mockHttp);
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
            var response = await client.GetItemsAsync<object>(elements, inFilter, allFilter, anyFilter);

            // Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async Task TooLongUrlThrows()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(_guid, _mockHttp);

            var elements = new ElementsParameter(Enumerable.Range(0, 1000000).Select(i => "test").ToArray());

            // Act / Assert
            await Assert.ThrowsAsync<UriFormatException>(async () => await client.GetItemsAsync<object>(elements));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task PreviewAndSecuredProductionThrowsWhenBothEnabled(bool usePreviewApi,
            bool useSecuredProduction)
        {
            if (usePreviewApi)
            {
                _mockHttp
                    .When($@"https://preview-deliver.kontent.ai/{_guid}/items")
                    .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));
            }
            else
            {
                _mockHttp
                    .When($"{_baseUrl}/items")
                    .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));
            }

            var options = new DeliveryOptions
            {
                ProjectId = _guid.ToString(),
                UsePreviewApi = usePreviewApi,
                UseSecureAccess = useSecuredProduction,
                PreviewApiKey = "someKey",
                SecureAccessApiKey = "someKey"
            };

            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithOptions(options, _mockHttp);

            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy())
                .Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            if (usePreviewApi && useSecuredProduction)
            {
                // Assert
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetItemsAsync<object>());
            }
            else
            {
                var response = await client.GetItemsAsync<object>();

                // Assert
                Assert.NotNull(response);
            }
        }

        [Fact]
        public async Task SecuredProductionAddCorrectHeader()
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
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithOptions(options, _mockHttp);

            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy())
                .Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            await client.GetItemsAsync<object>();
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetItemAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var response = await client.GetItemAsync<object>("coffee_beverages_explained");

            Assert.True(response.ApiResponse.HasStaleContent);
            Assert.IsType<Article>(response.Item);
        }

        [Fact]
        public async Task GetItemAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var response = await client.GetItemAsync<object>("coffee_beverages_explained");

            Assert.False(response.ApiResponse.HasStaleContent);
            Assert.IsType<Article>(response.Item);
        }

        [Fact]
        public async Task GetItemsAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var response = await client.GetItemsAsync<object>();

            Assert.True(response.ApiResponse.HasStaleContent);
            Assert.True(response.Items.Any());
        }

        [Fact]
        public async Task GetItemsAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var response = await client.GetItemsAsync<object>();

            Assert.False(response.ApiResponse.HasStaleContent);
            Assert.True(response.Items.Any());
        }

        [Fact]
        public async Task GetCustomItemAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var response = await client.GetItemAsync<Homepage>("coffee_beverages_explained");

            Assert.True(response.ApiResponse.HasStaleContent);
            Assert.NotNull(response.Item.System);
        }

        [Fact]
        public async Task GetCustomItemAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var response = await client.GetItemAsync<Homepage>("coffee_beverages_explained");

            Assert.False(response.ApiResponse.HasStaleContent);
            Assert.NotNull(response.Item.System);
        }

        [Fact]
        public async Task GetCustomItemsAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var response = await client.GetItemsAsync<Article>();

            Assert.True(response.ApiResponse.HasStaleContent);
            Assert.True(response.Items.Any());
        }

        [Fact]
        public async Task GetCustomItemsAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var response = await client.GetItemsAsync<Article>();

            Assert.False(response.ApiResponse.HasStaleContent);
            Assert.True(response.Items.Any());
        }

        [Fact]
        public async Task GetTypeAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/types/article")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}article.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTypeAsync("article");

            Assert.True(response.ApiResponse.HasStaleContent);
            Assert.Equal("Article", response.Type.System.Name);
        }

        [Fact]
        public async Task GetTypeAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/types/article")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}article.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTypeAsync("article");

            Assert.False(response.ApiResponse.HasStaleContent);
            Assert.Equal("Article", response.Type.System.Name);
        }

        [Fact]
        public async Task GetTypesAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/types")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTypesAsync();

            Assert.True(response.ApiResponse.HasStaleContent);
            Assert.True(response.Types.Any());
        }

        [Fact]
        public async Task GetTypesAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/types")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTypesAsync();

            Assert.False(response.ApiResponse.HasStaleContent);
            Assert.True(response.Types.Any());
        }

        [Fact]
        public async Task GetTaxonomyAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/taxonomies/personas")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTaxonomyAsync("personas");

            Assert.True(response.ApiResponse.HasStaleContent);
            Assert.NotNull(response.Taxonomy.System);
        }

        [Fact]
        public async Task GetTaxonomyAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/taxonomies/personas")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTaxonomyAsync("personas");

            Assert.False(response.ApiResponse.HasStaleContent);
            Assert.NotNull(response.Taxonomy.System);
        }

        [Fact]
        public async Task GetTaxonomiesAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/taxonomies")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTaxonomiesAsync();

            Assert.True(response.ApiResponse.HasStaleContent);
            Assert.True(response.Taxonomies.Any());
        }

        [Fact]
        public async Task GetTaxonomiesAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/taxonomies")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetTaxonomiesAsync();

            Assert.False(response.ApiResponse.HasStaleContent);
            Assert.True(response.Taxonomies.Any());
        }

        [Fact]
        public async Task GetElementAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/types/test/elements/test")
                .Respond(headers, "application/json", "{'type':'taxonomy','name':'Personas','codename':'Personas','taxonomy_group':'personas'}");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetContentElementAsync("test", "test");

            Assert.True(response.ApiResponse.HasStaleContent);
            Assert.NotNull(response.Element.Codename);
        }

        [Fact]
        public async Task GetElementAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/types/test/elements/test")
                .Respond(headers, "application/json", "{'type':'taxonomy','name':'Personas','codename':'Personas','taxonomy_group':'personas'}");

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetContentElementAsync("test", "test");

            Assert.False(response.ApiResponse.HasStaleContent);
            Assert.NotNull(response.Element.Codename);
        }

        [Fact]
        public async Task RetryPolicy_WithDefaultOptions_Retries()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond(request => new HttpResponseMessage(HttpStatusCode.RequestTimeout));
            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(_guid, _mockHttp);
            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy()).Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetItemsAsync<object>());

            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task RetryPolicy_Disabled_DoesNotRetry()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond(request => new HttpResponseMessage(HttpStatusCode.RequestTimeout));
            var options = new DeliveryOptions
            {
                ProjectId = _guid.ToString(),
                EnableRetryPolicy = false
            };
            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithOptions(options, _mockHttp);
            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy()).Returns(retryPolicy);

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetItemsAsync<object>());

            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._)).MustNotHaveHappened();
        }

        [Fact]
        [Trait("Issue", "146")]
        public async Task InitializeMultipleInlineContentItemsResolvers()
        {
            string url = $"{_baseUrl}/items/";
            const string tweetPrefix = "Tweet resolver: ";
            const string hostedVideoPrefix = "Video resolver: ";
            _mockHttp
                .When($"{url}coffee_beverages_explained")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var deliveryClient = DeliveryClientBuilder
                .WithProjectId(_guid)
                .WithInlineContentItemsResolver(InlineContentItemsResolverFactory.Instance
                    .ResolveTo<Tweet>(tweet => tweetPrefix + tweet.TweetLink))
                .WithInlineContentItemsResolver(InlineContentItemsResolverFactory.Instance
                    .ResolveTo<HostedVideo>(video => hostedVideoPrefix + video.VideoHost.First().Name))
                .WithTypeProvider(new CustomTypeProvider())
                .WithDeliveryHttpClient(new DeliveryHttpClient(_mockHttp.ToHttpClient()))
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
                new PropertyMapper(),
                new DeliveryJsonSerializer(), new HtmlParser());
            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(
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

        private DeliveryClient InitializeDeliveryClientWithCustomModelProvider(MockHttpMessageHandler handler, IPropertyMapper propertyMapper = null, ITypeProvider typeProvider = null)
        {
            var typer = typeProvider ?? _mockTypeProvider;
            var mapper = propertyMapper ?? A.Fake<IPropertyMapper>();
            var serializer = new DeliveryJsonSerializer();
            var modelProvider = new ModelProvider(null, null, typer, mapper, serializer, new HtmlParser());
            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(_guid, handler, modelProvider);

            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy())
                .Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            return client;
        }
    }
}