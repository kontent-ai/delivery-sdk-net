using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

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
	}
}
