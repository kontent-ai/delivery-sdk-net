using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using Xunit;
using RichardSzalay.MockHttp;
using System.IO;
using System.Net;

namespace KenticoCloud.Delivery.Tests
{
    public class DeliveryClientTests
    {
        readonly string guid = string.Empty;
        readonly string baseUrl = string.Empty;
        readonly MockHttpMessageHandler mockHttp;

        public DeliveryClientTests()
        {
            guid = Guid.NewGuid().ToString();
            baseUrl = $"https://deliver.kenticocloud.com/{guid}";
            mockHttp = new MockHttpMessageHandler();
        }

        [Fact]
        public async void GetItemAsync()
        {
            string url = $"{baseUrl}/items/";

            mockHttp.When($"{url}{"coffee_beverages_explained"}").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\coffee_beverages_explained.json")));

            mockHttp.When($"{url}{"brazil_natural_barra_grande"}").
              Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\brazil_natural_barra_grande.json")));

            mockHttp.When($"{url}{"on_roasts"}").
            Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\on_roasts.json")));

            var httpClient = mockHttp.ToHttpClient();
            DeliveryClient client = new DeliveryClient(guid) { HttpClient = httpClient };

            var beveragesResponse = (await client.GetItemAsync("coffee_beverages_explained"));
            var beveragesItem = beveragesResponse.Item;
            var barraItem = (await client.GetItemAsync("brazil_natural_barra_grande")).Item;
            var roastsItem = (await client.GetItemAsync("on_roasts")).Item;
            Assert.Equal("article", beveragesItem.System.Type);
            Assert.Equal("en-US", beveragesItem.System.Language);
            Assert.NotEmpty(beveragesItem.System.SitemapLocation);
            Assert.NotEmpty(roastsItem.GetModularContent("related_articles"));
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
            mockHttp.When($"{baseUrl}/items").
                WithQueryString("limit=2&skip=1").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\articles.json")));

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            var articles = await client.GetItemsAsync(new LimitParameter(2), new SkipParameter(1));

            Assert.Equal(2, articles.Pagination.Count);
            Assert.Equal(1, articles.Pagination.Skip);
            Assert.Equal(2, articles.Pagination.Limit);
            Assert.NotNull(articles.Pagination.NextPageUrl);
        }

        [Fact]
        public async void AssetPropertiesNotEmpty()
        {
            mockHttp.When($"{baseUrl}/items/{"coffee_beverages_explained"}").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\coffee_beverages_explained.json")));

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            var response = await client.GetItemAsync("coffee_beverages_explained");
            var beveragesItem = response.Item;

            var model = beveragesItem.CastTo<Article>();

            Assert.NotNull(model.TeaserImage.FirstOrDefault().Description);
            Assert.NotNull(model.TeaserImage.FirstOrDefault().Name);
            Assert.NotNull(model.TeaserImage.FirstOrDefault().Type);
            Assert.NotNull(model.TeaserImage.FirstOrDefault().Url);
            Assert.NotNull(response.ApiUrl);
        }

        [Fact]
        public async void IgnoredSerializationProperties()
        {
            mockHttp.When($"{baseUrl}/items/{"coffee_beverages_explained"}").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\coffee_beverages_explained.json")));

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            var beveragesItem = (await client.GetItemAsync("coffee_beverages_explained")).Item;

            var model = beveragesItem.CastTo<Article>();

            Assert.NotNull(model.TitleNotIgnored);
            Assert.Null(model.TitleIgnored);
        }

        [Fact]
        public async void GetItemAsync_NotFound()
        {
            string messsge = "{'message': 'The requested content item unscintillating_hemerocallidaceae_des_iroquois was not found.','request_id': '','error_code': 101,'specific_code': 0}";

            mockHttp.When($"{baseUrl}/items/unscintillating_hemerocallidaceae_des_iroquois").
                Respond(HttpStatusCode.NotFound, "application/json", messsge);

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetItemAsync("unscintillating_hemerocallidaceae_des_iroquois"));
        }

        [Fact]
        public async void GetItemsAsync()
        {
            mockHttp.When($"{baseUrl}/items").
                WithQueryString("system.type=cafe").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\allendale.json")));

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            var response = await client.GetItemsAsync(new EqualsFilter("system.type", "cafe"));

            Assert.NotEmpty(response.Items);
        }

        [Fact]
        public async void GetTypeAsync()
        {
            mockHttp.When($"{baseUrl}/types/article").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\article.json")));

            mockHttp.When($"{baseUrl}/types/coffee").
              Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\coffee.json")));

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            var articleType = await client.GetTypeAsync("article");
            var coffeeType = await client.GetTypeAsync("coffee");

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

            mockHttp.When($"{baseUrl}/types/unequestrian_nonadjournment_sur_achoerodus").
                Respond(HttpStatusCode.NotFound, "application/json", messsge);

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetTypeAsync("unequestrian_nonadjournment_sur_achoerodus"));
        }

        [Fact]
        public async void GetTypesAsync()
        {
            mockHttp.When($"{baseUrl}/types").
                WithQueryString("skip=1").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\types_accessory.json")));

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            var response = await client.GetTypesAsync(new SkipParameter(1));

            Assert.NotNull(response.ApiUrl);
            Assert.NotEmpty(response.Types);
        }

        [Fact]
        public async void GetContentElementAsync()
        {
            string url = $"{baseUrl}/types";

            mockHttp.When($"{url}/{Article.Codename}/elements/{Article.TitleCodename}").
                Respond("application/json", "{'type':'text','name':'Title','codename':'title'}");
            mockHttp.When($"{url}/{Article.Codename}/elements/{Article.PersonasCodename}").
               Respond("application/json", "{'type':'taxonomy','name':'Personas','codename':'Personas','taxonomy_group':'personas'}");
            mockHttp.When($"{url}/{Coffee.Codename}/elements/{Coffee.ProcessingCodename}").
               Respond("application/json", "{'type':'taxonomy','name':'Processing','taxonomy_group':'processing','codename':'processing'}");

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            var element = await client.GetContentElementAsync(Article.Codename, Article.TitleCodename);
            var personasTaxonomyElement = await client.GetContentElementAsync(Article.Codename, Article.PersonasCodename);
            var processingTaxonomyElement = await client.GetContentElementAsync(Coffee.Codename, Coffee.ProcessingCodename);

            Assert.Equal(Article.TitleCodename, element.Codename);
            Assert.Equal(Article.PersonasCodename, personasTaxonomyElement.TaxonomyGroup);
            Assert.Equal(Coffee.ProcessingCodename, processingTaxonomyElement.TaxonomyGroup);
        }

        [Fact]
        public async void GetContentElementsAsync_NotFound()
        {
            string url = $"{baseUrl}/types/anticommunistical_preventure_sur_helxine/elements/unlacerated_topognosis_sur_nonvigilantness";

            string messsge = "{'message': 'The requested content type anticommunistical_preventure_sur_helxine was not found.','request_id': '','error_code': 101,'specific_code': 0}";
            mockHttp.When($"{url}").
                Respond(HttpStatusCode.NotFound, "application/json", messsge);

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetContentElementAsync("anticommunistical_preventure_sur_helxine", "unlacerated_topognosis_sur_nonvigilantness"));
        }

        [Fact]
        public async void GetTaxonomyAsync()
        {
            mockHttp.When($"{baseUrl}/taxonomies/personas").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\taxonomies_personas.json")));

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            var taxonomy = await client.GetTaxonomyAsync("personas");
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
            string url = $"{baseUrl}/taxonomies/unequestrian_nonadjournment_sur_achoerodus";
            mockHttp.When($"{url}").
                Respond(HttpStatusCode.NotFound, "application/json", "{'message':'The requested taxonomy group unequestrian_nonadjournment_sur_achoerodus was not found.'}");

            var httpClient = mockHttp.ToHttpClient();

            DeliveryClient client = new DeliveryClient(guid)
            {
                CodeFirstModelProvider = { TypeProvider = new CustomTypeProvider() },
                HttpClient = httpClient
            };

            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetTaxonomyAsync("unequestrian_nonadjournment_sur_achoerodus"));
        }

        [Fact]
        public async void GetTaxonomiesAsync()
        {
            mockHttp.When($"{baseUrl}/taxonomies").
                WithQueryString("skip=1").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\taxonomies_multiple.json")));

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            var response = await client.GetTaxonomiesAsync(new SkipParameter(1));

            Assert.NotNull(response.ApiUrl);
            Assert.NotEmpty(response.Taxonomies);
        }

        [Fact]
        public async void QueryParameters()
        {

            string url = $"{baseUrl}/items?elements.personas%5Ball%5D=barista%2Ccoffee%2Cblogger&elements.personas%5Bany%5D=barista%2Ccoffee%2Cblogger&system.sitemap_locations%5Bcontains%5D=cafes&elements.product_name=Hario%20V60&elements.price%5Bgt%5D=1000&elements.price%5Bgte%5D=50&system.type%5Bin%5D=cafe%2Ccoffee&elements.price%5Blt%5D=10&elements.price%5Blte%5D=4&elements.country%5Brange%5D=Guatemala%2CNicaragua&depth=2&elements=price%2Cproduct_name&limit=10&order=elements.price%5Bdesc%5D&skip=2&language=en";
            mockHttp.When($"{url}").
                        Respond("application/json", " { 'items': [],'modular_content': {},'pagination': {'skip': 2,'limit': 10,'count': 0,'next_page': ''}}");

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

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
            mockHttp.When($"{baseUrl}/items/on_roasts").
                WithQueryString("depth=1").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\on_roasts.json")));

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            // Returns on_roasts content item with related_articles modular element to two other articles.
            // on_roasts
            // |- coffee_processing_techniques
            // |- origins_of_arabica_bourbon
            //   |- on_roasts
            var onRoastsItem = (await client.GetItemAsync<Article>("on_roasts", new DepthParameter(1))).Item;

            Assert.NotNull(onRoastsItem.TeaserImage.First().Description);
            Assert.Equal(2, onRoastsItem.RelatedArticles.Count());
            Assert.Equal(0, ((Article)onRoastsItem.RelatedArticles.First()).RelatedArticles.Count());
            Assert.Equal(0, ((Article)onRoastsItem.RelatedArticles.ElementAt(1)).RelatedArticles.Count());
        }

        [Fact]
        public async void RecursiveModularContent()
        {
            mockHttp.When($"{baseUrl}/items/on_roasts").
                WithQueryString("depth=15").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\onroast_deep15.json")));

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            // Try to get recursive modular content on_roasts -> item -> on_roasts
            var article = await client.GetItemAsync<Article>("on_roasts", new DepthParameter(15));

            Assert.NotNull(article.Item);
        }

        [Fact]
        public void GetStronglyTypedResponse()
        {
            mockHttp.When($"{baseUrl}/items/complete_content_item").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\complete_content_item.json")));

            DeliveryClient client = InitializeDeliverClientWithACustomeTypeProvider();

            var response = client.GetItemAsync<CompleteContentItemModel>("complete_content_item").Result;
            CompleteContentItemModel item = response.Item;

            // Assert
            Assert.Equal("Text field value", item.TextField);

            Assert.Equal("<p>Rich text field value</p>", item.RichTextField);

            Assert.Equal(99, item.NumberField);

            Assert.Equal(1, item.MultipleChoiceFieldAsRadioButtons.Count());
            Assert.Equal("Radio button 1", item.MultipleChoiceFieldAsRadioButtons.First().Name);

            Assert.Equal(2, item.MultipleChoiceFieldAsCheckboxes.Count());
            Assert.Equal("Checkbox 1", item.MultipleChoiceFieldAsCheckboxes.First().Name);
            Assert.Equal("Checkbox 2", item.MultipleChoiceFieldAsCheckboxes.ElementAt(1).Name);

            Assert.Equal(new DateTime(2017, 2, 23), item.DateTimeField);

            Assert.Equal(1, item.AssetField.Count());
            Assert.Equal("Fire.jpg", item.AssetField.First().Name);
            Assert.Equal(129170, item.AssetField.First().Size);
            Assert.Equal("https://assets.kenticocloud.com:443/e1167a11-75af-4a08-ad84-0582b463b010/64096741-b658-46ee-b148-b287fe03ea16/Fire.jpg", item.AssetField.First().Url);

            Assert.Equal(1, item.ModularContentField.Count());
            Assert.Equal("Homepage", item.ModularContentField.First().System.Name);

            Assert.Equal(2, item.CompleteTypeTaxonomy.Count());
            Assert.Equal("Option 1", item.CompleteTypeTaxonomy.First().Name);
            Assert.NotNull(response.ApiUrl);
        }

        [Fact]
        public void GetStronglyTypedGenericWithAttributesResponse()
        {
            mockHttp.When($"{baseUrl}/items/complete_content_item").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\complete_content_item.json")));

            var httpClient = mockHttp.ToHttpClient();

            DeliveryClient client = new DeliveryClient(guid)
            {
                CodeFirstModelProvider = { TypeProvider = A.Fake<ICodeFirstTypeProvider>() },
                HttpClient = httpClient
            };


            // Arrange
            A.CallTo(() => client.CodeFirstModelProvider.TypeProvider.GetType("complete_content_type")).ReturnsLazily(() => typeof(ContentItemModelWithAttributes));
            A.CallTo(() => client.CodeFirstModelProvider.TypeProvider.GetType("homepage")).ReturnsLazily(() => typeof(Homepage));

            ContentItemModelWithAttributes item = (ContentItemModelWithAttributes)client.GetItemAsync<object>("complete_content_item").Result.Item;

            // Assert
            Assert.Equal("Text field value", item.TextFieldWithADifferentName);

            Assert.Equal("<p>Rich text field value</p>", item.RichTextFieldWithADifferentName);

            Assert.Equal(99, item.NumberFieldWithADifferentName);

            Assert.Equal(1, item.MultipleChoiceFieldAsRadioButtonsWithADifferentName.Count());
            Assert.Equal("Radio button 1", item.MultipleChoiceFieldAsRadioButtonsWithADifferentName.First().Name);

            Assert.Equal(2, item.MultipleChoiceFieldAsCheckboxes.Count());
            Assert.Equal("Checkbox 1", item.MultipleChoiceFieldAsCheckboxes.First().Name);
            Assert.Equal("Checkbox 2", item.MultipleChoiceFieldAsCheckboxes.ElementAt(1).Name);

            Assert.Equal(new DateTime(2017, 2, 23), item.DateTimeFieldWithADifferentName);

            Assert.Equal(1, item.AssetFieldWithADifferentName.Count());
            Assert.Equal("Fire.jpg", item.AssetFieldWithADifferentName.First().Name);
            Assert.Equal(129170, item.AssetFieldWithADifferentName.First().Size);
            Assert.Equal("https://assets.kenticocloud.com:443/e1167a11-75af-4a08-ad84-0582b463b010/64096741-b658-46ee-b148-b287fe03ea16/Fire.jpg", item.AssetFieldWithADifferentName.First().Url);

            Assert.Equal(1, item.ModularContentFieldWithADifferentName.Count());
            Assert.Equal("Homepage", ((Homepage)item.ModularContentFieldWithADifferentName.First()).System.Name);
            Assert.Equal("Homepage", ((Homepage)item.ModularContentFieldWithACollectionTypeDefined.First()).System.Name);
            Assert.True(item.ModularContentFieldWithAGenericTypeDefined.First().CallToAction.Length > 0);

            Assert.Equal(2, item.CompleteTypeTaxonomyWithADifferentName.Count());
            Assert.Equal("Option 1", item.CompleteTypeTaxonomyWithADifferentName.First().Name);
        }

        [Fact]
        public void GetStronglyTypedItemsResponse()
        {
            mockHttp.When($"{baseUrl}/items").
                WithQueryString("system.type=complete_content_type").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\complete_content_item_system_type.json")));

            var httpClient = mockHttp.ToHttpClient();

            DeliveryClient client = new DeliveryClient(guid)
            {
                CodeFirstModelProvider = { TypeProvider = A.Fake<ICodeFirstTypeProvider>() },
                HttpClient = httpClient
            };


            // Arrange
            A.CallTo(() => client.CodeFirstModelProvider.TypeProvider.GetType("complete_content_type")).ReturnsLazily(() => typeof(ContentItemModelWithAttributes));
            A.CallTo(() => client.CodeFirstModelProvider.TypeProvider.GetType("homepage")).ReturnsLazily(() => typeof(Homepage));

            IReadOnlyList<object> items = client.GetItemsAsync<object>(new EqualsFilter("system.type", "complete_content_type")).Result.Items;

            // Assert
            Assert.True(items.All(i => i.GetType() == typeof(ContentItemModelWithAttributes)));
        }

        [Fact]
        public void CastResponse()
        {
            mockHttp.When($"{baseUrl}/items/complete_content_item").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\complete_content_item.json")));

            var httpClient = mockHttp.ToHttpClient();

            DeliveryClient client = new DeliveryClient(guid)
            {
                HttpClient = httpClient
            };

            var response = client.GetItemAsync("complete_content_item").Result;
            var stronglyTypedResponse = response.CastTo<CompleteContentItemModel>();

            // Assert
            Assert.Equal("Text field value", stronglyTypedResponse.Item.TextField);
        }

        [Fact]
        public void CastListingResponse()
        {
            mockHttp.When($"{baseUrl}/items").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\items.json")));

            var httpClient = mockHttp.ToHttpClient();

            DeliveryClient client = new DeliveryClient(guid)
            {
                HttpClient = httpClient
            };

            var response = client.GetItemsAsync().Result;
            var stronglyTypedListingResponse = response.CastTo<CompleteContentItemModel>();

            // Assert
            Assert.NotNull(stronglyTypedListingResponse);
            Assert.True(stronglyTypedListingResponse.Items.Any());
        }

        [Fact]
        public void CastContentItem()
        {
            mockHttp.When($"{baseUrl}/items/complete_content_item").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\complete_content_item.json")));

            var httpClient = mockHttp.ToHttpClient();

            DeliveryClient client = new DeliveryClient(guid)
            {
                HttpClient = httpClient
            };


            var item = client.GetItemAsync("complete_content_item").Result.Item;
            var stronglyTypedResponse = item.CastTo<CompleteContentItemModel>();

            // Assert
            Assert.Equal("Text field value", stronglyTypedResponse.TextField);
        }

        [Fact]
        public void CastContentItems()
        {
            mockHttp.When($"{baseUrl}/items").
                Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\DeliveryClient\\items.json")));

            var httpClient = mockHttp.ToHttpClient();

            DeliveryClient client = new DeliveryClient(guid)
            {
                HttpClient = httpClient
            };

            // Act
            DeliveryItemListingResponse response = client.GetItemsAsync().Result;
            IEnumerable<CompleteContentItemModel> list = response.Items.Where(i => i.System.Type == "complete_content_type").Select(a => a.CastTo<CompleteContentItemModel>());

            // Assert
            Assert.True(list.Any());
        }

        private DeliveryClient InitializeDeliverClientWithACustomeTypeProvider()
        {
            var httpClient = mockHttp.ToHttpClient();

            DeliveryClient client = new DeliveryClient(guid)
            {
                CodeFirstModelProvider = { TypeProvider = new CustomTypeProvider() },
                HttpClient = httpClient
            };
            return client;
        }
    }
}
