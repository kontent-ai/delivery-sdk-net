using System;
using System.Reflection;
using System.Threading.Tasks;
using FakeItEasy;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems;
using Kentico.Kontent.Delivery.Tests.Factories;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests
{
    public class ModelProviderTests
    {
        [Fact]
        // During processing of inline content items, item which detects circular dependency ( A refs B, B refs A ) should resolve resolved item
        // as if there were no inline content items, which will prevent circular dependency
        public async Task RetrievingContentModelWithCircularDependencyDoesNotCycle()
        {
            var typeProvider = A.Fake<ITypeProvider>();
            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var propertyMapper = A.Fake<IPropertyMapper>();
            A.CallTo(() => propertyMapper.IsMatch(A<PropertyInfo>._, A<string>._, A<string>._)).Returns(true);
            A.CallTo(() => typeProvider.GetType(A<string>._)).Returns(typeof(ContentItemWithSingleRte));

            var processor = InlineContentItemsProcessorFactory
                .WithResolver(ResolveItemWithSingleRte)
                .Build();
            var retriever = new ModelProvider(contentLinkUrlResolver, processor, typeProvider, propertyMapper, new DeliveryJsonSerializer());

            var item = JToken.FromObject(Rt1);
            var linkedItems = JToken.FromObject(LinkedItemsForItemWithTwoReferencedContentItems);

            var result = await retriever.GetContentItemModel<ContentItemWithSingleRte>(item, linkedItems);

            Assert.Equal("<span>FirstRT</span><span>SecondRT</span><span>FirstRT</span>", result.Rt);
            Assert.IsType<ContentItemWithSingleRte>(result);
        }

        [Fact]
        public async Task RetrievingNonExistentContentModelCreatesWarningInRichtext()
        {
            var typeProvider = A.Fake<ITypeProvider>();
            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var propertyMapper = A.Fake<IPropertyMapper>();
            A.CallTo(() => typeProvider.GetType(A<string>._)).Returns(null);
            A.CallTo(() => propertyMapper.IsMatch(A<PropertyInfo>._, A<string>._, A<string>._)).Returns(true);

            var processor = InlineContentItemsProcessorFactory
                .WithResolver(factory => factory.ResolveTo<UnknownContentItem>(unknownItem => $"Content type '{unknownItem.Type}' has no corresponding model."))
                .Build();
            var retriever = new ModelProvider(contentLinkUrlResolver, processor, typeProvider, propertyMapper, new DeliveryJsonSerializer());

            var item = JToken.FromObject(Rt5);
            var linkedItems = JToken.FromObject(LinkedItemWithNoModel);
            var expectedResult =
                $"<span>RT</span>Content type '{linkedItems.SelectToken("linkedItemWithNoModel.system.type")}' has no corresponding model.";

            var result = await retriever.GetContentItemModel<ContentItemWithSingleRte>(item, linkedItems);

            Assert.Equal(expectedResult, result.Rt);
            Assert.IsType<ContentItemWithSingleRte>(result);
        }

        [Fact]
        // In case item is referencing itself ( A refs A ) we'd like to go through second processing as if there were no inline content items,
        // this is same as in other cases, because as soon as we start processing item which is already being processed we remove inline content items.
        public async Task RetrievingContentModelWithItemInlineReferencingItselfDoesNotCycle()
        {
            var typeProvider = A.Fake<ITypeProvider>();
            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var propertyMapper = A.Fake<IPropertyMapper>();
            A.CallTo(() => typeProvider.GetType(A<string>._)).Returns(typeof(ContentItemWithSingleRte));
            A.CallTo(() => propertyMapper.IsMatch(A<PropertyInfo>._, A<string>._, A<string>._)).Returns(true);

            var processor = InlineContentItemsProcessorFactory
                .WithResolver(ResolveItemWithSingleRte)
                .Build();
            var retriever = new ModelProvider(contentLinkUrlResolver, processor, typeProvider, propertyMapper, new DeliveryJsonSerializer());

            var item = JToken.FromObject(Rt3);
            var linkedItems = JToken.FromObject(LinkedItemsForItemReferencingItself);

            var result = await retriever.GetContentItemModel<ContentItemWithSingleRte>(item, linkedItems);

            Assert.Equal("<span>RT</span><span>RT</span>", result.Rt);
            Assert.IsType<ContentItemWithSingleRte>(result);
        }

        /// <seealso href="https://github.com/Kentico/kontent-delivery-sdk-net/issues/126"/>
        [Fact]
        public async Task GetContentItemModelRetrievingContentModelWithUnknownTypeReturnNull()
        {
            var item = JToken.FromObject(Rt4);
            var linkedItems = JToken.FromObject(LinkedItemsForItemWithTwoReferencedContentItems);

            var contentLinkUrlResolver = A.Fake<IContentLinkUrlResolver>();
            var inlineContentItemsProcessor = A.Fake<IInlineContentItemsProcessor>();
            var typeProvider = A.Fake<ITypeProvider>();
            var propertyMapper = A.Fake<IPropertyMapper>();
            A.CallTo(() => typeProvider.GetType("newType")).Returns(null);
            var modelProvider = new ModelProvider(contentLinkUrlResolver, inlineContentItemsProcessor, typeProvider, propertyMapper, new DeliveryJsonSerializer());

            Assert.Null(await modelProvider.GetContentItemModel<object>(item, linkedItems));
        }

        private static readonly object Rt1 = new
        {
            system = new
            {
                id = "9dc3ca3a-22e0-4414-a56d-7a504e9f1eb2",
                name = "RT1",
                codename = "rt1",
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
                    modular_content = new[] { "rt2" },
                    value = "<span>FirstRT</span><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"rt2\"></object"
                }

            }
        };

        private static readonly object Rt2 = new
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
                    modular_content = new[] { "rt1" },
                    value =
                    "<span>SecondRT</span><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"rt1\"></object>"
                }
            }
        };

        private static readonly object Rt3 = new
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

        private static readonly object Rt4 = new
        {
            system = new
            {
                id = "43e2d109-c727-4bb0-9a54-0dc8af018be9",
                name = "RT4",
                codename = "rt4",
                type = "newType",
                sitemap_location = new string[0],
                last_modified = new DateTime(2017, 06, 01, 11, 43, 33)
            },
            elements = new { }
        };

        private static readonly object Rt5 = new
        {
            system = new
            {
                id = "43e2d109-c727-4bb0-9a54-0dc8af018be9",
                name = "RT5",
                codename = "rt5",
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
                    modular_content = new[] { "linkedItemWithNoModel" },
                    value = "<span>RT</span><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"linkedItemWithNoModel\"></object>"
                }
            }
        };

        private static readonly object LinkedItemWithNoModel = new
        {
            linkedItemWithNoModel = new
            {
                system = new
                {
                    id = "473cd60b-a2a7-4e5b-8353-5d1995dd4b50",
                    name = "linkedItemWithNoModel",
                    codename = "linkedItemWithNoModel",
                    language = "en-US",
                    type = "newType",
                    sitemap_locations = new string[0],
                    last_modified = new DateTime(2017, 06, 01, 11, 43, 33)
                },
                elements = new
                { }
            }
        };

        private static readonly object LinkedItemsForItemWithTwoReferencedContentItems = new
        {
            rt2 = Rt2,
            rt1 = Rt1
        };

        private static readonly object LinkedItemsForItemReferencingItself = new
        {
            rt3 = Rt3
        };

        private static IInlineContentItemsResolver<ContentItemWithSingleRte> ResolveItemWithSingleRte(InlineContentItemsResolverFactory factory)
            => factory.ResolveTo<ContentItemWithSingleRte>(item => item.Rt);

        private class ContentItemWithSingleRte
        {
            public string Rt { get; set; }
        }
    }
}
