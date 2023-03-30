using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Kontent.Ai.Delivery.Extensions.Universal;
using RichardSzalay.MockHttp;

namespace Kontent.Ai.Delivery.Extensions.Tests
{
    public class DeliveryClientExtensionsTests
    {
        private readonly Guid _guid;
        private readonly string _baseUrl;
        private readonly MockHttpMessageHandler _mockHttp;

        public DeliveryClientExtensionsTests()
        {
            _guid = Guid.NewGuid();
            var projectId = _guid.ToString();
            _baseUrl = $"https://deliver.kontent.ai/{projectId}";
            _mockHttp = new MockHttpMessageHandler();
        }

        


        [Fact]
        public async Task GetUniversalItemAsync_RespondCorrectly()
        {
            _mockHttp
                .When($"{_baseUrl}/items/complete_content_item")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}complete_content_item.json")));

            IDeliveryClient client = null;
            var response = await client.GetUniversalItemAsync("complete_content_item");

            Assert.All(response.Item.Elements, item =>
            {

                Assert.NotNull(item.Value.Name);
                Assert.NotNull(item.Value.Type);
                Assert.NotNull(item.Value.Codename);

                var element = item.Value as IContentElementValue<object>;

                switch (element)
                {
                    case TextElementValue text:
                        {
                            Assert.Equal("text", text.Type);
                            Assert.Equal("Text field", text.Name);
                            Assert.Equal("text_field", text.Codename);
                            Assert.Equal("Text field value", text.Value);
                            break;
                        }
                    case RichTextElementValue richText:
                        {
                            Assert.Equal("rich_text", richText.Type);
                            Assert.Equal("Rich text field", richText.Name);
                            Assert.Equal("rich_text_field", richText.Codename);
                            Assert.Equal("<p>Rich text field value</p>", richText.Value);
                            Assert.Empty(richText.ModularContent);
                            Assert.Empty(richText.Images);
                            Assert.Empty(richText.Links);
                            break;
                        }
                    case NumberElementValue number:
                        {
                            Assert.Equal("number", number.Type);
                            Assert.Equal("Number field", number.Name);
                            Assert.Equal("number_field", number.Codename);
                            Assert.Equal(99, number.Value);
                            break;
                        }
                    case MultipleChoiceElementValue multipleChoice:
                        {
                            Assert.Equal("multiple_choice", multipleChoice.Type);
                            if (multipleChoice.Codename == "multiple_choice_field_as_radio_buttons")
                            {

                                Assert.Equal("Multiple choice field as Radio buttons", multipleChoice.Name);
                                Assert.Collection(multipleChoice.Value,
                                item =>
                                    {
                                        Assert.Equal("Radio button 1", item.Name);
                                        Assert.Equal("radio_button_1", item.Codename);
                                    });
                            }
                            else if (multipleChoice.Codename == "multiple_choice_field_as_checkboxes")
                            {
                                Assert.Equal("Multiple choice field as Checkboxes", multipleChoice.Name);
                                Assert.Collection(multipleChoice.Value,
                                    item =>
                                    {
                                        Assert.Equal("Checkbox 1", item.Name);
                                        Assert.Equal("checkbox_1", item.Codename);
                                    },
                                    item =>
                                    {
                                        Assert.Equal("Checkbox 2", item.Name);
                                        Assert.Equal("checkbox_2", item.Codename);
                                    });
                            }
                            break;
                        }
                    case IDateTimeElementValue dateTime:
                        {
                            Assert.Equal("date_time", dateTime.Type);
                            Assert.Equal("Date & time field", dateTime.Name);
                            Assert.Equal("date___time_field", dateTime.Codename);
                            Assert.Equal(new DateTime(2017, 02, 23), dateTime.Value);
                            Assert.Equal("display_timezone", dateTime.DisplayTimezone);
                            break;
                        }
                    case AssetElementValue assets:
                        {
                            Assert.Equal("asset", assets.Type);
                            Assert.Equal("Asset field", assets.Name);
                            Assert.Equal("asset_field", assets.Codename);
                            Assert.Collection(assets.Value,
                                asset =>
                                {
                                    Assert.Equal("Fire.jpg", asset.Name);
                                    Assert.Equal("image/jpeg", asset.Type);
                                    Assert.Equal(129170, asset.Size);
                                    Assert.Equal("https://assets.kontent.ai:443/e1167a11-75af-4a08-ad84-0582b463b010/64096741-b658-46ee-b148-b287fe03ea16/Fire.jpg", asset.Url);
                                });
                            break;
                        }
                    case LinkedItemsElementValue linkedItems:
                        {
                            Assert.Equal("modular_content", linkedItems.Type);
                            Assert.Equal("Modular content field", linkedItems.Name);
                            Assert.Equal("linked_items_field", linkedItems.Codename);
                            Assert.Equal(new[] { "homepage" }, linkedItems.Value);
                            break;
                        }
                    case TaxonomyElementValue taxonomy:
                        {
                            Assert.Equal("taxonomy", taxonomy.Type);
                            Assert.Equal("Complete type taxonomy", taxonomy.Name);
                            Assert.Equal("complete_type_taxonomy", taxonomy.Codename);
                            Assert.Equal("complete_type_taxonomy", taxonomy.TaxonomyGroup);

                            Assert.Collection(taxonomy.Value,
                                taxonomyTerm =>
                                {
                                    Assert.Equal("Option 1", taxonomyTerm.Name);
                                    Assert.Equal("option_1", taxonomyTerm.Codename);
                                },
                                taxonomyTerm =>
                                {
                                    Assert.Equal("Option 2", taxonomyTerm.Name);
                                    Assert.Equal("option_2", taxonomyTerm.Codename);
                                });

                            break;
                        }
                    case UrlSlugElementValue urlSlug:
                        {
                            Assert.Equal("url_slug", urlSlug.Type);
                            Assert.Equal("Url slug field", urlSlug.Name);
                            Assert.Equal("url_slug_field", urlSlug.Codename);
                            Assert.Equal("complete-content-item-url-slug", urlSlug.Value);
                            break;
                        }
                    case CustomElementValue customElement:
                        {
                            Assert.Equal("custom", customElement.Type);
                            Assert.Equal("ColorPicker", customElement.Name);
                            Assert.Equal("custom_element_field", customElement.Codename);
                            Assert.Equal("#d7e119", customElement.Value);
                            break;
                        }
                }
            });
        }

        [Fact]
        public async Task GetUniversalItemsAsync_RespondCorrectly()
        {
            _mockHttp
                .When($"{_baseUrl}/items")
                .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

            IDeliveryClient client = null;

            var response = await client.GetUniversalItemsAsync();

            Assert.Equal(11, response.Items.Count);
            Assert.Equal(5, response.LinkedItems.Count);
            Assert.Equal(0, response.Pagination.Skip);
            Assert.Equal(0, response.Pagination.Limit);
            Assert.Equal(11, response.Pagination.Count);
            Assert.Null(response.Pagination.TotalCount);
            Assert.Null(response.Pagination.NextPageUrl);
        }
    }
}