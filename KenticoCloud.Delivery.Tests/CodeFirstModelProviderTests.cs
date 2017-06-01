using System.Collections.Generic;
using AngleSharp.Dom.Events;
using FakeItEasy;
using KenticoCloud.Delivery.InlineContentItems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace KenticoCloud.Delivery.Tests
{
    public class CodeFirstModelProviderTests
    {
        [Fact]
        public void RetrievingContentModelWithCircularDependencyDoesNotCycle()
        {
            var fakeDeliverClient = A.Fake<IDeliveryClient>();
            var codeFirstTypeProvider = A.Fake<ICodeFirstTypeProvider>();
            A.CallTo(() => codeFirstTypeProvider.GetType(A<string>._)).Returns(typeof(ContentItemWithSingleRTE));
            
            var processor = A.Fake<IInlineContentItemsProcessor>();
            A.CallTo(() => processor.Process(A<string>.Ignored, A<Dictionary<string,object>>.Ignored)).Returns("a");
            
            A.CallTo(() => fakeDeliverClient.InlineContentItemsProcessor).Returns(processor);

            var retriever = new CodeFirstModelProvider(fakeDeliverClient);
            retriever.TypeProvider = codeFirstTypeProvider;

            var item = JToken.Parse(contentItemContaningRTE);
            var modularContent = JToken.Parse(modularContentReferencingRTE);

            var result = retriever.GetContentItemModel<ContentItemWithSingleRTE>(item, modularContent);


            Assert.IsType<ContentItemWithSingleRTE>(result);
            A.CallTo(() => processor.Process(A<string>._, A<Dictionary<string, object>>._))
                .MustHaveHappened(Repeated.Like(i => i == 2));
            A.CallTo(() => processor.RemoveAll(A<string>._))
                .MustHaveHappened(Repeated.Like(i => i == 1));
        }

        private class ContentItemWithSingleRTE
        {
            public string RT { get; set; }
        }

        private const string contentItemContaningRTE =
            "{\r\n    \"system\": {\r\n      \"id\": \"9dc3ca3a-22e0-4414-a56d-7a504e9f1eb2\",\r\n      \"name\": \"RT1\",\r\n      \"codename\": \"rt1\",\r\n      \"type\": \"simple_richtext\",\r\n      \"sitemap_locations\": [],\r\n      \"last_modified\": \"2017-06-01T11:43:33.1968174Z\"\r\n    },\r\n    \"elements\": {\r\n      \"rt\": {\r\n        \"type\": \"rich_text\",\r\n        \"name\": \"RT\",\r\n        \"images\": {},\r\n        \"links\": {},\r\n        \"modular_content\": [\r\n          \"rt2\"\r\n        ],\r\n        \"value\": \"<p><br></p>\\n<object type=\\\"application/kenticocloud\\\" data-type=\\\"item\\\" data-codename=\\\"rt2\\\"></object>\"\r\n      }\r\n    }\r\n  }";

        private const string modularContentReferencingRTE =
            "{\r\n    \"rt2\": {\r\n      \"system\": {\r\n        \"id\": \"c7e516cb-28c9-41a4-8531-3c88a70aa54f\",\r\n        \"name\": \"RT2\",\r\n        \"codename\": \"rt2\",\r\n        \"type\": \"simple_richtext\",\r\n        \"sitemap_locations\": [],\r\n        \"last_modified\": \"2017-06-01T11:43:50.2741506Z\"\r\n      },\r\n      \"elements\": {\r\n        \"rt\": {\r\n          \"type\": \"rich_text\",\r\n          \"name\": \"RT\",\r\n          \"images\": {},\r\n          \"links\": {},\r\n          \"modular_content\": [\r\n            \"rt1\"\r\n          ],\r\n          \"value\": \"<p><br></p>\\n<object type=\\\"application/kenticocloud\\\" data-type=\\\"item\\\" data-codename=\\\"rt1\\\"></object>\"\r\n        }\r\n      }\r\n    },\r\n    \"rt1\": {\r\n      \"system\": {\r\n        \"id\": \"9dc3ca3a-22e0-4414-a56d-7a504e9f1eb2\",\r\n        \"name\": \"RT1\",\r\n        \"codename\": \"rt1\",\r\n        \"type\": \"simple_richtext\",\r\n        \"sitemap_locations\": [],\r\n        \"last_modified\": \"2017-06-01T11:43:33.1968174Z\"\r\n      },\r\n      \"elements\": {\r\n        \"rt\": {\r\n          \"type\": \"rich_text\",\r\n          \"name\": \"RT\",\r\n          \"images\": {},\r\n          \"links\": {},\r\n          \"modular_content\": [\r\n            \"rt2\"\r\n          ],\r\n          \"value\": \"<p><br></p>\\n<object type=\\\"application/kenticocloud\\\" data-type=\\\"item\\\" data-codename=\\\"rt2\\\"></object>\"\r\n        }\r\n      }\r\n    }\r\n}";
    }


}
