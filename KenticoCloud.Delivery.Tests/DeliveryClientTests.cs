using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using Xunit;

namespace KenticoCloud.Delivery.Tests
{
    public class DeliveryClientTests
    {
        public const string PROJECT_ID = "975bf280-fd91-488c-994c-2f04416e5ee3";
        private const string SANDBOX_PROJECT_ID = "e1167a11-75af-4a08-ad84-0582b463b010";

        private readonly DeliveryClient client;

        public DeliveryClientTests()
        {
            client = new DeliveryClient(PROJECT_ID) { CodeFirstModelProvider = { TypeProvider = new CustomTypeProvider() } };
        }

        [Fact]
        public async void GetItemAsync()
        {
            var beveragesItem = (await client.GetItemAsync("coffee_beverages_explained")).Item;
            var barraItem = (await client.GetItemAsync("brazil_natural_barra_grande")).Item;
            var roastsItem = (await client.GetItemAsync("on_roasts")).Item;
            Assert.Equal("article", beveragesItem.System.Type);
            Assert.NotEmpty(beveragesItem.System.SitemapLocation);
            Assert.NotEmpty(roastsItem.GetModularContent("related_articles"));
            Assert.Equal(beveragesItem.Elements.title.value.ToString(), beveragesItem.GetString("title"));
            Assert.Equal(beveragesItem.Elements.body_copy.value.ToString(), beveragesItem.GetString("body_copy"));
            Assert.Equal(DateTime.Parse(beveragesItem.Elements.post_date.value.ToString()), beveragesItem.GetDateTime("post_date"));
            Assert.Equal(beveragesItem.Elements.teaser_image.value.Count, beveragesItem.GetAssets("teaser_image").Count());
            Assert.Equal(beveragesItem.Elements.personas.value.Count, beveragesItem.GetTaxonomyTerms("personas").Count());
            Assert.Equal(decimal.Parse(barraItem.Elements.price.value.ToString()), barraItem.GetNumber("price"));
            Assert.Equal(barraItem.Elements.processing.value.Count, barraItem.GetOptions("processing").Count());
        }

        [Fact]
        public async void GetItemAsync_NotFound()
        {
            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetItemAsync("unscintillating_hemerocallidaceae_des_iroquois"));
        }

        [Fact]
        public async void GetItemsAsync()
        {
            var response = await client.GetItemsAsync(new EqualsFilter("system.type", "cafe"));

            Assert.NotEmpty(response.Items);
        }

        [Fact]
        public async void GetTypeAsync()
        {
            var articleType = await client.GetTypeAsync("article");
            var coffeeType = await client.GetTypeAsync("coffee");
            var taxonomyElement = articleType.Elements["personas"];
            var multipleChoiceElement = coffeeType.Elements["processing"];

            Assert.Equal("article", articleType.System.Codename);
            Assert.Equal("text", articleType.Elements["title"].Type);
            Assert.Equal("rich_text", articleType.Elements["body_copy"].Type);
            Assert.Equal("date_time", articleType.Elements["post_date"].Type);
            Assert.Equal("asset", articleType.Elements["teaser_image"].Type);
            Assert.Equal("modular_content", articleType.Elements["related_articles"].Type);
            Assert.Equal("taxonomy", articleType.Elements["personas"].Type);
            Assert.Equal("number", coffeeType.Elements["price"].Type);
            Assert.Equal("multiple_choice", coffeeType.Elements["processing"].Type);

            Assert.Equal("personas", taxonomyElement.TaxonomyGroup);
            Assert.NotEmpty(multipleChoiceElement.Options);
        }

        [Fact]
        public async void GetTypeAsync_NotFound()
        {
            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetTypeAsync("unequestrian_nonadjournment_sur_achoerodus"));
        }

        [Fact]
        public async void GetTypesAsync()
        {
            var response = await client.GetTypesAsync(new SkipParameter(1));

            Assert.NotEmpty(response.Types);
        }

        [Fact]
        public async void GetContentElementAsync()
        {
            var element = await client.GetContentElementAsync("article", "title");
            var taxonomyElement = await client.GetContentElementAsync("article", "personas");
            var multipleChoiceElement = await client.GetContentElementAsync("coffee", "processing");

            Assert.Equal("title", element.Codename);
            Assert.Equal("personas", taxonomyElement.TaxonomyGroup);
            Assert.NotEmpty(multipleChoiceElement.Options);
        }

        [Fact]
        public async void GetContentElementsAsync_NotFound()
        {
            await Assert.ThrowsAsync<DeliveryException>(async () => await client.GetContentElementAsync("anticommunistical_preventure_sur_helxine", "unlacerated_topognosis_sur_nonvigilantness"));
        }

        [Fact]
        public async void QueryParameters()
        {
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
            // Returns on_roasts content item with related_articles modular element to two other articles.
            // on_roasts
            // |- coffee_processing_techniques
            // |- origins_of_arabica_bourbon
            //   |- on_roasts
            var onRoastsItem = (await client.GetItemAsync<Article>("on_roasts", new DepthParameter(1))).Item;

            Assert.Equal(2, onRoastsItem.RelatedArticles.Count());
            Assert.Equal(0, ((Article)onRoastsItem.RelatedArticles.First()).RelatedArticles.Count());
            Assert.Equal(0, ((Article)onRoastsItem.RelatedArticles.ElementAt(1)).RelatedArticles.Count());
        }

        [Fact]
        public async void RecursiveModularContent()
        {
            // Try to get recursive modular content on_roasts -> item -> on_roasts
            var article = await client.GetItemAsync<Article>("on_roasts", new DepthParameter(15));

            Assert.NotNull(article.Item);
        }

        [Fact]
        public void GetStronglyTypedResponse()
        {
            // Arrange
            var client2 = new DeliveryClient(SANDBOX_PROJECT_ID);

            // Act
            CompleteContentItemModel item = client2.GetItemAsync<CompleteContentItemModel>("complete_content_item").Result.Item;

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
        }

        [Fact]
        public void GetStronglyTypedGenericWithAttributesResponse()
        {
            // Arrange
            var client2 = new DeliveryClient(SANDBOX_PROJECT_ID) { CodeFirstModelProvider = { TypeProvider = A.Fake<ICodeFirstTypeProvider>() } };
            A.CallTo(() => client2.CodeFirstModelProvider.TypeProvider.GetType("complete_content_type")).ReturnsLazily(() => typeof(ContentItemModelWithAttributes));
            A.CallTo(() => client2.CodeFirstModelProvider.TypeProvider.GetType("homepage")).ReturnsLazily(() => typeof(Homepage));

            // Act
            ContentItemModelWithAttributes item = (ContentItemModelWithAttributes)client2.GetItemAsync<object>("complete_content_item").Result.Item;

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
            // Arrange
            var client2 = new DeliveryClient(SANDBOX_PROJECT_ID) { CodeFirstModelProvider = { TypeProvider = A.Fake<ICodeFirstTypeProvider>() } };
            A.CallTo(() => client2.CodeFirstModelProvider.TypeProvider.GetType("complete_content_type")).ReturnsLazily(() => typeof(ContentItemModelWithAttributes));
            A.CallTo(() => client2.CodeFirstModelProvider.TypeProvider.GetType("homepage")).ReturnsLazily(() => typeof(Homepage));

            // Act
            IReadOnlyList<object> items = client2.GetItemsAsync<object>(new EqualsFilter("system.type", "complete_content_type")).Result.Items;

            // Assert
            Assert.True(items.All(i => i.GetType() == typeof(ContentItemModelWithAttributes)));
        }

        [Fact]
        public void CastResponse()
        {
            // Arrange
            var client2 = new DeliveryClient(SANDBOX_PROJECT_ID);

            // Act
            var response = client2.GetItemAsync("complete_content_item").Result;
            var stronglyTypedResponse = response.CastTo<CompleteContentItemModel>();

            // Assert
            Assert.Equal("Text field value", stronglyTypedResponse.Item.TextField);
        }

        [Fact]
        public void CastListingResponse()
        {
            // Arrange
            var client2 = new DeliveryClient(SANDBOX_PROJECT_ID);

            // Act
            var response = client2.GetItemsAsync().Result;
            var stronglyTypedListingResponse = response.CastTo<CompleteContentItemModel>();

            // Assert
            Assert.NotNull(stronglyTypedListingResponse);
            Assert.True(stronglyTypedListingResponse.Items.Any());
        }

        [Fact]
        public void CastContentItem()
        {
            // Arrange
            var client2 = new DeliveryClient(SANDBOX_PROJECT_ID);

            // Act
            var item = client2.GetItemAsync("complete_content_item").Result.Item;
            var stronglyTypedResponse = item.CastTo<CompleteContentItemModel>();

            // Assert
            Assert.Equal("Text field value", stronglyTypedResponse.TextField);
        }

        [Fact]
        public void CastContentItems()
        {
            // Arrange
            var client2 = new DeliveryClient(SANDBOX_PROJECT_ID);

            // Act
            DeliveryItemListingResponse response = client2.GetItemsAsync().Result;
            IEnumerable<CompleteContentItemModel> list = response.Items.Where(i => i.System.Type == "complete_content_type").Select(a => a.CastTo<CompleteContentItemModel>());

            // Assert
            Assert.True(list.Any());
        }
    }
}
