using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace KenticoCloud.Delivery.Tests
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TestGreeterValueConverterAttribute : Attribute, IPropertyValueConverter
    {
        public object GetPropertyValue(PropertyInfo property, JToken elementData, CodeFirstResolvingContext context)
        {
            var element = (JObject)elementData;
            var str = element.Property("value")?.Value?.ToObject<string>();
            return $"Hello {str}!";
        }
    }


    public class ValueConverterTests
    {
        private readonly DeliveryClient client;

        public ValueConverterTests()
        {
            client = new DeliveryClient(DeliveryClientTests.PROJECT_ID) { CodeFirstModelProvider = { TypeProvider = new CustomTypeProvider() } };
        }

        [Fact]
        public async void GreeterPropertyValueConverter()
        {
            var article = await client.GetItemAsync<Article>("on_roasts");

            Assert.Equal("Hello On Roasts!", article.Item.TitleConverted);
        }

        [Fact]
        public async void RichTextViaValueConverter()
        {
            // Try to get recursive modular content on_roasts -> item -> on_roasts
            var article = await client.GetItemAsync<Article>("coffee_beverages_explained", new DepthParameter(15));

            var hostedVideo = article.Item.BodyCopyRichText.Blocks.FirstOrDefault(b => (b as IInlineContentItem)?.ContentItem is HostedVideo);
            var tweet = article.Item.BodyCopyRichText.Blocks.FirstOrDefault(b => (b as IInlineContentItem)?.ContentItem is Tweet);

            Assert.NotNull(hostedVideo);
            Assert.NotNull(tweet);
        }
    }
}
