using System;
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
            
            var processor = new InlineContentItemsProcessor(null, null);
            processor.RegisterTypeResolver(new RichTextInlineResolver());
            var retriever = new CodeFirstModelProvider(fakeDeliverClient);
            retriever.TypeProvider = codeFirstTypeProvider;
            A.CallTo(() => fakeDeliverClient.InlineContentItemsProcessor).Returns(processor);

            var item = JToken.FromObject(rt1);
            var modularContent = JToken.FromObject(modularContentObject);

            var result = retriever.GetContentItemModel<ContentItemWithSingleRTE>(item, modularContent);

            Assert.Equal("<span>FirstRT</span><span>SecondRT</span><span>FirstRT</span>", result.RT);
            Assert.IsType<ContentItemWithSingleRTE>(result);
        }

        private class ContentItemWithSingleRTE
        {
            public string RT { get; set; }
        }

        private class RichTextInlineResolver: IInlineContentItemsResolver<ContentItemWithSingleRTE>
        {
            public string Resolve(ResolvedContentItemData<ContentItemWithSingleRTE> data)
            {
                return data.Item.RT;
            }
        }

        private static object rt1 = new
        {
            system = new
            {
                id = "9dc3ca3a-22e0-4414-a56d-7a504e9f1eb2",
                name = "RT1" ,
                codename = "rt1",
                type = "simple_richtext",
                sitemap_location = new string[0],
                last_modified = new DateTime(2017,06,01, 11,43,33)
            },
            elements = new
            {
                rt = new
                {
                    type = "rich_text",
                    name = "RT",
                    modular_content = new [] { "rt2"},
                    value = "<span>FirstRT</span><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"rt2\"></object"
                }

            }
        };

        private static object rt2 = new
        {
            system = new
            {
                id = "c7e516cb-28c9-41a4-8531-3c88a70aa54f",
                name = "RT2",
                codename = "rt2",
                type = "simple_richtext",
                sitemap_location = new string[0],
                last_modified = new DateTime(2017, 06, 01, 11, 43, 33)
            },
            elements = new
            {
                rt = new
                {
                    type = "rich_text",
                    name = "RT",
                    modular_content = new [] {"rt1"},
                    value =
                    "<span>SecondRT</span><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"rt1\"></object>"
                }
            }
        };

        private static object modularContentObject = new
        {
            rt2,
            rt1
        };

    }


}
