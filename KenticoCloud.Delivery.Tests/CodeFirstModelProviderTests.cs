using System;
using System.Reflection;
using FakeItEasy;
using KenticoCloud.Delivery.InlineContentItems;
using Newtonsoft.Json.Linq;
using Xunit;

namespace KenticoCloud.Delivery.Tests
{
    public class CodeFirstModelProviderTests
    {
        [Fact]
        // During processing of inline content items, item which detects circular dependency ( A refs B, B refs A ) should resolve resolved item
        // as if there were no inline content items, which will prevent circular dependency
        public void RetrievingContentModelWithCircularDependencyDoesNotCycle()
        {
            var codeFirstTypeProvider = A.Fake<ICodeFirstTypeProvider>();
            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var propertyMapper = A.Fake<ICodeFirstPropertyMapper>(); //new CodeFirstPropertyMapper();
            A.CallTo(() => propertyMapper.IsMatch(A<PropertyInfo>._, A<string>._, A<string>._)).Returns(true);
            A.CallTo(() => codeFirstTypeProvider.GetType(A<string>._)).Returns(typeof(ContentItemWithSingleRTE));

            var processor = new InlineContentItemsProcessor(null, null);
            processor.RegisterTypeResolver(new RichTextInlineResolver());
            var retriever = new CodeFirstModelProvider(contentLinkUrlResolver, processor, codeFirstTypeProvider, propertyMapper);

            var item = JToken.FromObject(rt1);
            var modularContent = JToken.FromObject(modularContentForItemWithTwoReferencedContentItems);

            var result = retriever.GetContentItemModel<ContentItemWithSingleRTE>(item, modularContent);

            Assert.Equal("<span>FirstRT</span><span>SecondRT</span><span>FirstRT</span>", result.RT);
            Assert.IsType<ContentItemWithSingleRTE>(result);
        }

        [Fact]
        // In case item is referencing itself ( A refs A ) we'd like to go through second processing as if there were no inline content items,
        // this is same as in other cases, because as soon as we start processing item which is already being processed we remove inline content items.
        public void RetrievingContentModelWithItemInlineReferencingItselfDoesNotCycle()
        {
            var codeFirstTypeProvider = A.Fake<ICodeFirstTypeProvider>();
            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var propertyMapper = A.Fake<ICodeFirstPropertyMapper>();
            A.CallTo(() => codeFirstTypeProvider.GetType(A<string>._)).Returns(typeof(ContentItemWithSingleRTE));
            A.CallTo(() => propertyMapper.IsMatch(A<PropertyInfo>._, A<string>._, A<string>._)).Returns(true);

            var processor = new InlineContentItemsProcessor(null, null);
            processor.RegisterTypeResolver(new RichTextInlineResolver());
            var retriever = new CodeFirstModelProvider(contentLinkUrlResolver, processor, codeFirstTypeProvider, propertyMapper);

            var item = JToken.FromObject(rt3);
            var modularContent = JToken.FromObject(modularContentForItemReferencingItself);

            var result = retriever.GetContentItemModel<ContentItemWithSingleRTE>(item, modularContent);

            Assert.Equal("<span>RT</span><span>RT</span>", result.RT);
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

        private static object modularContentForItemWithTwoReferencedContentItems = new
        {
            rt2,
            rt1
        };

        private static object rt3 = new
        {
            system = new
            {
                id = "9dc3ca3a-22e0-4414-a56d-7a504e9f1eb2",
                name = "RT3",
                codename = "rt3",
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
                    modular_content = new[] { "rt3" },
                    value = "<span>RT</span><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"rt3\"></object>"
                }

            }
        };

        private static object modularContentForItemReferencingItself = new
        {
            rt3
        };
    }


}
