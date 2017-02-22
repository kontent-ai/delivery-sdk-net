using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery.Tests
{
    [TestFixture]
    public class DeliveryClientTests
    {
        private const string PROJECT_ID = "975bf280-fd91-488c-994c-2f04416e5ee3";

        [Test]
        public void GetItemAsync()
        {
            // Arrange
            var client = new DeliveryClient(PROJECT_ID);

            // Act
            var item = Task.Run(() => client.GetItemAsync("coffee_beverages_explained")).Result.Item;

            var textElement = item.GetString("title");
            var richTextElement = item.GetString("body_copy");
            var datetimeElement = item.GetDateTime("post_date");
            var assetElement = item.GetAssets("teaser_image");
            var modularContentElement = item.GetModularContent("related_articles");

            // Assert
            Assert.AreEqual("article", item.System.Type);
            Assert.AreEqual("Coffee Beverages Explained", textElement);
            Assert.That(() => richTextElement.Contains("Caffeine: &lt; 100 mg/cup<br/>"));
            Assert.AreEqual(DateTime.Parse("2014-11-18"), datetimeElement);
            Assert.AreEqual(1, assetElement.Count);
            Assert.AreEqual(0, modularContentElement.Count());
        }


        [Test]
        public void GetItemAsync_NonExistentCodename()
        {
            // Arrange
            var client = new DeliveryClient(PROJECT_ID);

            // Act
            AsyncTestDelegate d = async () => await client.GetItemAsync("sdk_test_item_non_existent");

            // Assert
            Assert.ThrowsAsync<DeliveryException>(d);
        }


        [Test]
        public void GetItemsAsync()
        {
            // Arrange
            var client = new DeliveryClient(PROJECT_ID);
            var filters = new List<IFilter> { new EqualsFilter("system.type", "cafe") };

            // Act
            var response = Task.Run(() => client.GetItemsAsync(filters)).Result;

            // Assert
            Assert.IsNotNull(response);
            Assert.GreaterOrEqual(response.Items.Count, 1);
        }


        [Test]
        public void GetStronglyTypedResponse()
        {
            const string SANDBOX_PROJECT_ID = "e1167a11-75af-4a08-ad84-0582b463b010";

            // Arrange
            var client = new DeliveryClient(SANDBOX_PROJECT_ID);

            // Act
            CompleteContentItemModel item = Task.Run(() => client.GetItemAsync<CompleteContentItemModel>("complete_content_item")).Result.Item;

            // Assert
            Assert.AreEqual("Text field value", item.TextField);

            Assert.AreEqual("<p>Rich text field value</p>", item.RichTextField);

            Assert.AreEqual(99, item.NumberField);

            Assert.AreEqual(1, item.MultipleChoiceFieldAsRadioButtons.Count);
            Assert.AreEqual("Radio button 1", item.MultipleChoiceFieldAsRadioButtons[0].Name);

            Assert.AreEqual(2, item.MultipleChoiceFieldAsCheckboxes.Count);
            Assert.AreEqual("Checkbox 1", item.MultipleChoiceFieldAsCheckboxes[0].Name);
            Assert.AreEqual("Checkbox 2", item.MultipleChoiceFieldAsCheckboxes[1].Name);

            Assert.AreEqual(new DateTime(2017, 2, 23), item.DateTimeField);

            Assert.AreEqual(1, item.AssetField.Count);
            Assert.AreEqual("Fire.jpg", item.AssetField[0].Name);

            Assert.AreEqual(1, item.ModularContentField.Count());

            Assert.AreEqual(2, item.CompleteTypeTaxonomy.Count);
        }
    }
}
