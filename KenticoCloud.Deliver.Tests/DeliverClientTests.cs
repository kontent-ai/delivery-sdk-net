using System;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace KenticoCloud.Deliver.Tests
{
    [TestFixture]
    public class DeliverClientTests
    {
        private const string PROJECT_ID = "ac9df07a-1251-4721-9ccf-6935ad5b6c1f";

        [Test]
        public void GetItemAsync()
        {
            var client = new DeliverClient(PROJECT_ID);

            var item = Task.Run(() => client.GetItemAsync("sdk_test_item")).Result.Item;

            Assert.AreEqual("sdk_test_item", item.System.Codename);

            var textElement = item.GetString("text");
            var richTextElement = item.GetString("rich_text");
            var numberElement = item.GetNumber("number");
            var datetimeElement = item.GetDatetime("datetime");
            var assetElement = item.GetAssets("assets");
            var modularContentElement = item.GetModularContent("modular_content");

            Assert.AreEqual("random textz", textElement);
            Assert.AreEqual("<p>this is <strong>rich</strong>!</p>", richTextElement);
            Assert.AreEqual(21.63, numberElement);
            Assert.AreEqual(DateTime.Parse("21.10.2016 0:00:00"), datetimeElement);
            Assert.AreEqual(2, assetElement.Count);
            Assert.AreEqual(4, modularContentElement.Count());
        }


        [Test]
        public void GetItemsAsync()
        {
            var client = new DeliverClient(PROJECT_ID);

            var filters = new List<IFilter> { new EqualsFilter("system.type", "sdk_test_type") };

            var response = Task.Run(() => client.GetItemsAsync(filters)).Result;

            Assert.IsNotNull(response);
        }
    }
}
