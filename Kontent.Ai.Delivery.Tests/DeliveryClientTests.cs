using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Html.Parser;
using FakeItEasy;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Builders.DeliveryClient;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;
using Kontent.Ai.Delivery.SharedModels;
using Kontent.Ai.Delivery.Sync;
using Kontent.Ai.Delivery.Tests.Factories;
using Kontent.Ai.Delivery.Tests.Models;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Kontent.Ai.Urls.Delivery.QueryParameters;
using Kontent.Ai.Urls.Delivery.QueryParameters.Filters;
using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests
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
            var barraResponse = await client.GetItemAsync<Coffee>("brazil_natural_barra_grande");
            var barraItem = barraResponse.Item;
            var roastsResponse = await client.GetItemAsync<Article>("on_roasts");
            var roastsItem = roastsResponse.Item;

            Assert.True(beveragesResponse?.ApiResponse?.IsSuccess);
            Assert.True(barraResponse?.ApiResponse?.IsSuccess);
            Assert.True(roastsResponse?.ApiResponse?.IsSuccess);
            Assert.Equal("article", beveragesItem.System.Type);
            Assert.Equal("en-US", beveragesItem.System.Language);
            Assert.Equal("default", beveragesItem.System.Collection);
            Assert.Equal("published", beveragesItem.System.WorkflowStep);
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
            Assert.True(beveragesResponse?.ApiResponse?.IsSuccess);
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
        public async Task GetItemAsync_AssetElement_PropertiesHaveCorrectValues()
        {
            _mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetItemAsync<Article>("coffee_beverages_explained");

            var teaserImage = response.Item.TeaserImage.FirstOrDefault();
            Assert.NotNull(teaserImage);
            Assert.Equal("Professional Espresso Machine", teaserImage.Description);
            Assert.Equal("coffee-beverages-explained-1080px.jpg", teaserImage.Name);
            Assert.Equal("image/jpeg", teaserImage.Type);
            Assert.Equal("https://assets.kontent.ai:443/975bf280-fd91-488c-994c-2f04416e5ee3/e700596b-03b0-4cee-ac5c-9212762c027a/coffee-beverages-explained-1080px.jpg", teaserImage.Url);
            Assert.Equal(800, teaserImage.Width);
            Assert.Equal(600, teaserImage.Height);
            Assert.NotNull(teaserImage.Renditions);
            Assert.NotEmpty(teaserImage.Renditions);

            var defaultRendition = response.Item.TeaserImage.First().Renditions["default"];
            Assert.NotNull(defaultRendition);
            Assert.Equal("d44a2887-74cc-4ab0-8376-ae96f3f534e5", defaultRendition.RenditionId);
            Assert.Equal("a6d98cd5-8b2c-4e50-99c9-15192bce2490", defaultRendition.PresetId);
            Assert.Equal(200, defaultRendition.Width);
            Assert.Equal(150, defaultRendition.Height);
            Assert.Equal("w=200&h=150&fit=clip&rect=7,23,300,200", defaultRendition.Query);
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
        public async Task GetItemAsync_NotFound_RespondsWithApiError()
        {
            var expectedError = new Error()
            {
                Message = "The requested content item unscintillating_hemerocallidaceae_des_iroquois was not found.",
                RequestId = "",
                ErrorCode = 100,
                SpecificCode = 0
            };
            var expectedResponse = CreateApiErrorResponse(expectedError);

            _mockHttp
                .When($"{_baseUrl}/items/unscintillating_hemerocallidaceae_des_iroquois")
                .Respond(HttpStatusCode.NotFound, "application/json", expectedResponse);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var actualResponse = await client.GetItemAsync<Article>("unscintillating_hemerocallidaceae_des_iroquois");

            AssertErrorResponse(actualResponse, expectedError);
            Assert.Null(actualResponse.Item);
        }
        
        [Fact]
        public async Task GetItemAsync_InvalidProjectId_RespondsWithApiError()
        {
            var expectedError = CreateInvalidProjectIdApiError();
            var response = CreateApiErrorResponse(expectedError);

            _mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond(HttpStatusCode.NotFound, "application/json", response);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var actualResponse = await client.GetItemAsync<object>("coffee_beverages_explained");
                
            AssertErrorResponse(actualResponse, expectedError);
            Assert.Null(actualResponse.Item);
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
        public async Task GetItemAsync_DateTimeElement_ParseCorrectly()
        {
            var mockedResponse = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json"));

            var expectedDateTimeValue = JObject.Parse(mockedResponse).SelectToken("item.elements.post_date.value").ToObject<DateTime?>();
            var expectedDateTimeDisplayTimezone = JObject.Parse(mockedResponse).SelectToken("item.elements.post_date.display_timezone").ToString();

            _mockHttp
                .When($"{_baseUrl}/items/coffee_beverages_explained")
                .Respond("application/json", mockedResponse);

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());
            var typedResult = await client.GetItemAsync<Article>("coffee_beverages_explained");

            Assert.Equal(expectedDateTimeValue, typedResult.Item.PostDate);

            var postDateContent = typedResult.Item.PostDateContent;
            Assert.NotNull(postDateContent);
            Assert.Equal(expectedDateTimeValue, postDateContent.Value);
            Assert.Equal(expectedDateTimeDisplayTimezone, postDateContent.DisplayTimezone);
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

            Assert.True(response?.ApiResponse?.IsSuccess);
            Assert.NotEmpty(response.Items);
        }
        
        [Fact]
        public async Task GetItemsAsync_InvalidProjectId_RespondsWithApiError()
        {
            var expectedError = CreateInvalidProjectIdApiError();
            var response = CreateApiErrorResponse(expectedError);

            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond(HttpStatusCode.NotFound, "application/json", response);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var actualResponse = await client.GetItemsAsync<object>();
                
            AssertErrorResponse(actualResponse, expectedError);
            Assert.Null(actualResponse.Items);
            Assert.Null(actualResponse.Pagination);
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
        public async Task GetItemsFeed_SingleBatchWithContinuationToken_FetchNextBatchAsync()
        {
            // Single batch with specific continuation token.
            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .WithQueryString("system.type=article")
                .WithHeaders("X-Continuation", "token")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var feed = client.GetItemsFeed<Article>();
            var items = new List<Article>();
            var timesCalled = 0;
            while (feed.HasMoreResults)
            {
                timesCalled++;
                var response = await feed.FetchNextBatchAsync("token");
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
        public async Task GetItemsFeed_InvalidProjectId_RespondsWithApiError()
        {
            var expectedError = CreateInvalidProjectIdApiError();
            var response = CreateApiErrorResponse(expectedError);

            _mockHttp
                .When($"{_baseUrl}/items-feed")
                .Respond(HttpStatusCode.NotFound, "application/json", response);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var actualResponse = await client.GetItemsFeed<object>().FetchNextBatchAsync();
                
            AssertErrorResponse(actualResponse, expectedError);
            Assert.Null(actualResponse.Items);
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

            var articleTypeResponse = await client.GetTypeAsync("article");
            var articleType = articleTypeResponse.Type;

            var coffeeTypeResponse = await client.GetTypeAsync("coffee");
            var coffeeType = coffeeTypeResponse.Type;

            var taxonomyElement = articleType.Elements["personas"];
            var processingTaxonomyElement = coffeeType.Elements["processing"];

            Assert.True(articleTypeResponse?.ApiResponse?.IsSuccess);
            Assert.True(coffeeTypeResponse?.ApiResponse?.IsSuccess);
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
        public async Task GetTypeAsync_NotFound_RespondsWithApiError()
        {
            var expectedError = new Error()
            {
                Message = "The requested content type unequestrian_nonadjournment_sur_achoerodus was not found.",
                RequestId = "",
                ErrorCode = 100,
                SpecificCode = 0
            };
            var expectedResponse = CreateApiErrorResponse(expectedError);

            _mockHttp
                .When($"{_baseUrl}/types/unequestrian_nonadjournment_sur_achoerodus")
                .Respond(HttpStatusCode.NotFound, "application/json", expectedResponse);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var actualResponse = await client.GetTypeAsync("unequestrian_nonadjournment_sur_achoerodus");

            AssertErrorResponse(actualResponse, expectedError);
            Assert.Null(actualResponse.Type);
        }
        
        [Fact]
        public async Task GetTypeAsync_InvalidProjectId_RespondsWithApiError()
        {
            var expectedError = CreateInvalidProjectIdApiError();
            var response = CreateApiErrorResponse(expectedError);

            _mockHttp
                .When($"{_baseUrl}/types/unequestrian_nonadjournment_sur_achoerodus")
                .Respond(HttpStatusCode.NotFound, "application/json", response);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var actualResponse = await client.GetTypeAsync("unequestrian_nonadjournment_sur_achoerodus");
                
            AssertErrorResponse(actualResponse, expectedError);
            Assert.Null(actualResponse.Type);
        }

        [Fact]
        public async Task GetItemsAsync_NoElements()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .WithQueryString("system.type%5Beq%5D=cafe")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}no_elements.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetItemsAsync<Cafe>(new EqualsFilter("system.type", "cafe"));

            Assert.Collection(response.Items, item =>
            {
                Assert.Null(item.City);
                Assert.Null(item.Country);
                Assert.Null(item.Email);
                Assert.Null(item.Phone);
                Assert.Null(item.Photo);
                Assert.Null(item.Sitemap);
                Assert.Null(item.State);
                Assert.Null(item.Street);
                Assert.NotNull(item.System);
            });
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

            Assert.True(response?.ApiResponse?.IsSuccess);
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

            var elementResponse = await client.GetContentElementAsync(Article.Codename, Article.TitleCodename);
            var element = elementResponse.Element;
            var personasTaxonomyElementResponse = await client.GetContentElementAsync(Article.Codename, Article.PersonasCodename);
            var personasTaxonomyElement = personasTaxonomyElementResponse.Element;
            var processingTaxonomyElementResponse = await client.GetContentElementAsync(Coffee.Codename, Coffee.ProcessingCodename);
            var processingTaxonomyElement = processingTaxonomyElementResponse.Element;
            var themeMultipleChoiceElementResponse = await client.GetContentElementAsync(Tweet.Codename, Tweet.ThemeCodename);
            var themeMultipleChoiceElement = themeMultipleChoiceElementResponse.Element;

            Assert.True(elementResponse?.ApiResponse?.IsSuccess);
            Assert.IsAssignableFrom<IContentElement>(element);
            Assert.Equal(Article.TitleCodename, element.Codename);

            Assert.True(personasTaxonomyElementResponse?.ApiResponse?.IsSuccess);
            Assert.IsAssignableFrom<ITaxonomyElement>(personasTaxonomyElement);
            Assert.Equal(Article.PersonasCodename, ((ITaxonomyElement)personasTaxonomyElement).TaxonomyGroup);

            Assert.True(processingTaxonomyElementResponse?.ApiResponse?.IsSuccess);
            Assert.IsAssignableFrom<ITaxonomyElement>(processingTaxonomyElement);
            Assert.Equal(Coffee.ProcessingCodename, ((ITaxonomyElement)processingTaxonomyElement).TaxonomyGroup);

            Assert.True(themeMultipleChoiceElementResponse?.ApiResponse?.IsSuccess);
            Assert.IsAssignableFrom<IMultipleChoiceElement>(themeMultipleChoiceElement);
            Assert.NotEmpty(((IMultipleChoiceElement)themeMultipleChoiceElement).Options);
        }

        [Fact]
        public async Task GetContentElementsAsync_NotFound()
        {
            string url = $"{_baseUrl}/types/anticommunistical_preventure_sur_helxine/elements/unlacerated_topognosis_sur_nonvigilantness";

            string messsge = "{'message': 'The requested content element unlacerated_topognosis_sur_nonvigilantness was not found in content type anticommunistical_preventure_sur_helxine','request_id': '','error_code': 102,'specific_code': 0}";
            _mockHttp
                .When($"{url}")
                .Respond(HttpStatusCode.NotFound, "application/json", messsge);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var result = await client.GetContentElementAsync("anticommunistical_preventure_sur_helxine", "unlacerated_topognosis_sur_nonvigilantness");

            Assert.NotNull(result?.ApiResponse?.Error);
            Assert.Equal(102, result?.ApiResponse?.Error?.ErrorCode);
            Assert.False(result?.ApiResponse?.IsSuccess);
            Assert.Equal("The requested content element unlacerated_topognosis_sur_nonvigilantness was not found in content type anticommunistical_preventure_sur_helxine", result?.ApiResponse?.Error?.Message);
            Assert.Equal(string.Empty, result?.ApiResponse?.Error?.RequestId);
            Assert.Equal(0, result?.ApiResponse?.Error?.SpecificCode);
        }

        [Fact]
        public async Task GetTaxonomyAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/taxonomies/personas")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetTaxonomyAsync("personas");
            var taxonomy = response.Taxonomy;
            var personasTerms = taxonomy.Terms.ToList();
            var coffeeExpertTerms = personasTerms[0].Terms.ToList();

            Assert.True(response?.ApiResponse?.IsSuccess);
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
                .Respond(HttpStatusCode.NotFound, "application/json", "{'message':'The requested taxonomy group unequestrian_nonadjournment_sur_achoerodus was not found.','request_id': '','error_code': 104,'specific_code': 0}");

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var result = await client.GetTaxonomyAsync("unequestrian_nonadjournment_sur_achoerodus");

            Assert.NotNull(result?.ApiResponse?.Error);
            Assert.Equal(104, result?.ApiResponse?.Error?.ErrorCode);
            Assert.False(result?.ApiResponse?.IsSuccess);
            Assert.Equal("The requested taxonomy group unequestrian_nonadjournment_sur_achoerodus was not found.", result?.ApiResponse?.Error?.Message);
            Assert.Equal(string.Empty, result?.ApiResponse?.Error?.RequestId);
            Assert.Equal(0, result?.ApiResponse?.Error?.SpecificCode);
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

            Assert.True(response?.ApiResponse?.IsSuccess);
            Assert.NotNull(response.ApiResponse.RequestUrl);
            Assert.NotEmpty(response.Taxonomies);
        }
        
        [Fact]
        public async Task GetTaxonomiesAsync_InvalidProjectId_RespondsWithApiError()
        {
            var expectedError = CreateInvalidProjectIdApiError();
            var response = CreateApiErrorResponse(expectedError);

            _mockHttp
                .When($"{_baseUrl}/taxonomies")
                .Respond(HttpStatusCode.NotFound, "application/json", response);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var actualResponse = await client.GetTaxonomiesAsync();
                
            AssertErrorResponse(actualResponse, expectedError);
            Assert.Null(actualResponse.Taxonomies);
            Assert.Null(actualResponse.Pagination);
        }

        [Fact]
        public async Task GetLanguagesAsync()
        {
            _mockHttp
                .When($"{_baseUrl}/languages")
                .WithQueryString("skip=1")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}languages.json")));

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var response = await client.GetLanguagesAsync(new SkipParameter(1));

            Assert.NotNull(response.ApiResponse.RequestUrl);
            Assert.NotEmpty(response.Languages);
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
        public async Task QueryParametersWithGenericFilter()
        {
            string url = $"{_baseUrl}/items?elements.personas%5Ball%5D=barista%2Ccoffee%2Cblogger&elements.personas%5Bany%5D=barista%2Ccoffee%2Cblogger&system.sitemap_locations%5Bcontains%5D=cafes&elements.last_modified%5Beq%5D=2023-03-01T00%3A00%3A00Z&elements.last_modified%5Bneq%5D=2023-03-02T00%3A00%3A00Z&elements.price%5Bgt%5D=&elements.price%5Bgte%5D=4&elements.price%5Bin%5D=100%2C50&elements.price%5Bnin%5D=300%2C400&elements.price%5Blt%5D=10.0001&elements.price%5Blte%5D=10000000000&elements.price%5Brange%5D=2022-01-01T00%3A00%3A00Z%2C2023-01-01T00%3A00%3A00Z&depth=2&elements=price%2Cproduct_name&limit=10&order=elements.price%5Bdesc%5D&skip=2&language=en&includeTotalCount";
            _mockHttp
                .When($"{url}")
                .Respond("application/json", " { 'items': [],'modular_content': {},'pagination': {'skip': 2,'limit': 10,'count': 0, 'total_count': 0, 'next_page': ''}}");

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var parameters = new IQueryParameter[]
           {
                new AllFilter("elements.personas", "barista", "coffee", "blogger"),
                new AnyFilter("elements.personas", "barista", "coffee", "blogger"),
                new ContainsFilter("system.sitemap_locations", "cafes"),
                new EqualsFilter<DateTime>("elements.last_modified", new DateTime(2023, 3, 1)),
                new NotEqualsFilter<DateTime>("elements.last_modified", new DateTime(2023, 3, 2)),
                new GreaterThanFilter<decimal>("elements.price", 0.00000000001m),
                new GreaterThanOrEqualFilter<decimal>("elements.price", 4),
                new InFilter<decimal>("elements.price", 100, 50.0m),
                new NotInFilter<decimal>("elements.price", 300, 400),
                new LessThanFilter<decimal>("elements.price", 10.0001m),
                new LessThanOrEqualFilter<decimal>("elements.price", 10000000000m),
                new RangeFilter<DateTime>("elements.price", new DateTime(2022, 1, 1), new DateTime(2023, 1, 1)),
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
                "https://assets.kontent.ai:443/e1167a11-75af-4a08-ad84-0582b463b010/64096741-b658-46ee-b148-b287fe03ea16/Fire.jpg",
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
                "https://assets.kontent.ai:443/e1167a11-75af-4a08-ad84-0582b463b010/64096741-b658-46ee-b148-b287fe03ea16/Fire.jpg",
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
        public async Task GetTypesAsync_InvalidProjectId_RespondsWithApiError()
        {
            var expectedError = CreateInvalidProjectIdApiError();
            var response = CreateApiErrorResponse(expectedError);

            _mockHttp
                .When($"{_baseUrl}/types")
                .Respond(HttpStatusCode.NotFound, "application/json", response);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var actualResponse = await client.GetTypesAsync();
                
            AssertErrorResponse(actualResponse, expectedError);
            Assert.Null(actualResponse.Types);
            Assert.Null(actualResponse.Pagination);
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
        public async Task GetLanguagesAsync_ApiReturnsStaleContent_ResponseIndicatesStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "1")
            };

            _mockHttp
                .When($"{_baseUrl}/languages")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}languages.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetLanguagesAsync();

            Assert.True(response.ApiResponse.HasStaleContent);
            Assert.True(response.Languages.Any());
        }

        [Fact]
        public async Task GetLanguagesAsync_ApiDoesNotReturnStaleContent_ResponseDoesNotIndicateStaleContent()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-Stale-Content", "0")
            };

            _mockHttp
                .When($"{_baseUrl}/languages")
                .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                        $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}languages.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var response = await client.GetLanguagesAsync();

            Assert.False(response.ApiResponse.HasStaleContent);
            Assert.True(response.Languages.Any());
        }
        
        [Fact]
        public async Task GetLanguagesAsync_InvalidProjectId_RespondsWithApiError()
        {
            var expectedError = CreateInvalidProjectIdApiError();
            var response = CreateApiErrorResponse(expectedError);

            _mockHttp
                .When($"{_baseUrl}/languages")
                .Respond(HttpStatusCode.NotFound, "application/json", response);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var actualResponse = await client.GetLanguagesAsync();
                
            AssertErrorResponse(actualResponse, expectedError);
            Assert.Null(actualResponse.Languages);
            Assert.Null(actualResponse.Pagination);
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
        public async Task GetElementAsync_InvalidProjectId_RespondsWithApiError()
        {
            var expectedError = CreateInvalidProjectIdApiError();
            var response = CreateApiErrorResponse(expectedError);

            _mockHttp
                .When($"{_baseUrl}/types/test/elements/test")
                .Respond(HttpStatusCode.NotFound, "application/json", response);

            var client = InitializeDeliveryClientWithACustomTypeProvider(_mockHttp);

            var actualResponse = await client.GetContentElementAsync("test", "test");
                
            AssertErrorResponse(actualResponse, expectedError);
            Assert.Null(actualResponse.Element);
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

        [Fact]
        public async Task RetrieveContentItem_GetLinkedItems_TypeItemsManually()
        {
            _mockHttp
               .When($"{_baseUrl}/items/coffee_beverages_explained")
               .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp, new PropertyMapper(), new CustomTypeProvider());

            var response = await client.GetItemAsync<object>("coffee_beverages_explained");

            var content = JObject.Parse(response.ApiResponse.Content ?? "{}");

            dynamic linkedItems = content["modular_content"].DeepClone();

            Assert.NotNull(linkedItems);

            var linkedItemsTyped = (linkedItems as JObject).Values();
            Assert.NotNull(linkedItemsTyped);
            Assert.Equal(2, linkedItemsTyped.Count());

            var itemTasks = linkedItemsTyped?.Select
            (
                async source =>
                {
                    return await client.ModelProvider.GetContentItemModelAsync<object>(source, linkedItems);
                }
            );
            var items = (await Task.WhenAll(itemTasks)).ToList();
            Assert.Equal(2, items.Count());
            
            var tweetItem = items[0];
            var hostedVideoItem = items[1];
            Assert.Equal(tweetItem.GetType(), typeof(Tweet));
            Assert.Equal(hostedVideoItem.GetType(), typeof(HostedVideo));
            Assert.Equal("https://twitter.com/DeCubaNorwich/status/879265763978358784", (tweetItem as Tweet).TweetLink);
            Assert.Equal("2Ao5b6uqI40", (hostedVideoItem as HostedVideo).VideoId);
        }

        [Fact]
        public async Task SyncApi_PostSyncInitAsync_GetContinuationToken()
        {
            var mockedResponse = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}sync_init.json"));

            _mockHttp
                .When($"{_baseUrl}/sync/init")
                .Respond(new[] { new KeyValuePair<string, string>("X-Continuation", "token"), }, "application/json", mockedResponse);

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var syncInit = await client.PostSyncInitAsync();

            Assert.NotNull(syncInit.ApiResponse.ContinuationToken);
            Assert.Empty(syncInit.SyncItems);
        }

        [Fact]
        public async Task SyncApi_PostSyncInitAsync_WithParameters_GetContinuationToken()
        {
            var mockedResponse = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}sync_init.json"));

            _mockHttp
                .When($"{_baseUrl}/sync/init")
                .Respond(new[] { new KeyValuePair<string, string>("X-Continuation", "token"), }, "application/json", mockedResponse);

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var syncInit = await client.PostSyncInitAsync(
                new LanguageParameter("cs"),
                new EqualsFilter("system.type", "article"),
                new NotEqualsFilter("system.collection", "default"));

            var requestUri = new Uri(syncInit.ApiResponse.RequestUrl);

            var requestQuery = HttpUtility.ParseQueryString(requestUri.Query);
            
            Assert.Equal(3, requestQuery.Count);
            Assert.Equal("language", requestQuery.Keys[0]);
            Assert.Equal("system.type[eq]", requestQuery.Keys[1]);
            Assert.Equal("system.collection[neq]", requestQuery.Keys[2]);
            Assert.NotNull(syncInit.ApiResponse.ContinuationToken);
            Assert.Empty(syncInit.SyncItems);
        }

        [Fact]
        public async Task SyncApi_GetSyncAsync_GetSyncItems()
        {
            var mockedResponse = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}sync.json"));

            var expectedValue = JObject.Parse(mockedResponse).SelectToken("items").ToObject<IList<SyncItem>>();
            
            _mockHttp
                .When($"{_baseUrl}/sync")
                .WithHeaders("X-Continuation", "token")
                .Respond(new[] { new KeyValuePair<string, string>("X-Continuation", "token"), }, "application/json", mockedResponse);

            var client = InitializeDeliveryClientWithCustomModelProvider(_mockHttp);

            var sync = await client.GetSyncAsync("token");

            Assert.NotNull(sync.ApiResponse.ContinuationToken);

            Assert.Equal(2, sync.SyncItems.Count);

            Assert.Equal(expectedValue[0].Codename, sync.SyncItems[0].Codename);
            Assert.Equal(expectedValue[0].Id, sync.SyncItems[0].Id);
            Assert.Equal(expectedValue[0].Type, sync.SyncItems[0].Type);
            Assert.Equal(expectedValue[0].Language, sync.SyncItems[0].Language);
            Assert.Equal(expectedValue[0].Collection, sync.SyncItems[0].Collection);
            Assert.Equal(expectedValue[0].ChangeType, sync.SyncItems[0].ChangeType);
            Assert.Equal(expectedValue[0].Timestamp, sync.SyncItems[0].Timestamp);

            Assert.Equal(expectedValue[1].Codename, sync.SyncItems[1].Codename);
            Assert.Equal(expectedValue[1].Id, sync.SyncItems[1].Id);
            Assert.Equal(expectedValue[1].Type, sync.SyncItems[1].Type);
            Assert.Equal(expectedValue[1].Language, sync.SyncItems[1].Language);
            Assert.Equal(expectedValue[1].Collection, sync.SyncItems[1].Collection);
            Assert.Equal(expectedValue[1].ChangeType, sync.SyncItems[1].ChangeType);
            Assert.Equal(expectedValue[1].Timestamp, sync.SyncItems[1].Timestamp);
        }

        private DeliveryClient InitializeDeliveryClientWithACustomTypeProvider(MockHttpMessageHandler handler)
        {
            var customTypeProvider = new CustomTypeProvider();
            var modelProvider = new ModelProvider(
                _mockContentLinkUrlResolver,
                null,
                customTypeProvider,
                new PropertyMapper(),
                new DeliveryJsonSerializer(),
                new HtmlParser(),
                DeliveryOptionsFactory.CreateMonitor(_guid));
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
            var modelProvider = new ModelProvider(null, null, typer, mapper, serializer, new HtmlParser(), DeliveryOptionsFactory.CreateMonitor(_guid));
            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithProjectId(_guid, handler, modelProvider);

            var retryPolicy = A.Fake<IRetryPolicy>();
            A.CallTo(() => client.RetryPolicyProvider.GetRetryPolicy())
                .Returns(retryPolicy);
            A.CallTo(() => retryPolicy.ExecuteAsync(A<Func<Task<HttpResponseMessage>>>._))
                .ReturnsLazily(c => c.GetArgument<Func<Task<HttpResponseMessage>>>(0)());

            return client;
        }
        
        private  Error CreateInvalidProjectIdApiError() => new Error()
        {
            ErrorCode = 105,
            RequestId = "",
            SpecificCode = 0,
            Message = $"Project with the ID '{_guid.ToString()}' does not exist. Check for the correct project ID in the API keys section of the UI. See https://kontent.ai/learn/reference/delivery-api for more details."
        };

        private static string CreateApiErrorResponse(Error error)
           => $"{{\"message\": \"{error.Message}\",\"request_id\": \"{error.RequestId}\",\"error_code\": {error.ErrorCode},\"specific_code\": {error.SpecificCode}}}";

        private static void AssertErrorResponse(IResponse actualResponse, IError expectedError)
        {
            var actualError = actualResponse.ApiResponse.Error;
            
            Assert.NotNull(actualResponse.ApiResponse.Error);
            Assert.False(actualResponse.ApiResponse.IsSuccess);
            Assert.Equal(expectedError.Message, actualError.Message);
            Assert.Equal(expectedError.ErrorCode, actualError.ErrorCode);
            Assert.Equal(expectedError.RequestId, actualError.RequestId);
            Assert.Equal(expectedError.SpecificCode, actualError.SpecificCode);
        }
    }
}