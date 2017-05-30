using NUnit.Framework;
using System.Collections.Generic;
using KenticoCloud.Delivery.ContentItemsInRichText;

namespace KenticoCloud.Delivery.Tests
{
    [TestFixture]
    public class ContentItemsInRichTextProcessorTests
    {
        private const string ContentItemType = "application/kenticocloud";
        private const string ContentItemDataType = "item";

        [Test]
        public void ProcessedRichTextIsSameIfNoContentItemsAreIncluded()
        {
            var richText = $"<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var richTextProcessor = new ContentItemsInRichTextProcessor(null, null);
            var processedContentItems = new Dictionary<string, object>();

            var result = richTextProcessor.Process(richText, processedContentItems);

            Assert.AreEqual(richText, result);
        }

        [Test]
        public void ProcessedRichTextContentItemsAreProcessedByDummyProcessor()
        {
            var insertedContentName1 = "dummyCodename1";
            var insertedContentName2 = "dummyCodename2";
            var insertedObject1 =
                $"<object type=\"{ContentItemType}\" data-type=\"{ContentItemDataType}\" data-codename=\"{insertedContentName1}\"></object/>";
            var insertedObject2 =
                $"<object type=\"{ContentItemType}\" data-type=\"{ContentItemDataType}\" data-codename=\"{insertedContentName2}\"></object/>";
            var plainRichText = $"<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var input = insertedObject1 + plainRichText + insertedObject2;
            var contentItemResolver = new DummyResolver();
            var richTextProcessor = new ContentItemsInRichTextProcessor(null, null);
            richTextProcessor.RegisterTypeResolver(contentItemResolver);
            var processedContentItems = new Dictionary<string, object>() {{insertedContentName1, new DummyProcessedContentItem()}, {insertedContentName2, new DummyProcessedContentItem()} };

            var result = richTextProcessor.Process(input, processedContentItems);

            Assert.AreEqual(plainRichText, result);
        }

        [Test]
        public void NestedContentItemInRichTextIsProcessedByDummyProcessor()
        {
            var insertedContentName = "dummyCodename1";
            string insertedObject = GetContentItemObjectElement(insertedContentName);
            var wrapperWithObject =
                "<div>" + insertedObject + "</div>";
            var plainWrapper = "<div></div>";
            var plainRichText = $"<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var richTextWithWrapper = plainRichText + wrapperWithObject;
            var processedContentItems = new Dictionary<string, object>()
            {
                {insertedContentName, new DummyProcessedContentItem()}
            };
            var contentItemResolver = new DummyResolver();
            var richTextProcessor = new ContentItemsInRichTextProcessor(null, null);
            richTextProcessor.RegisterTypeResolver(contentItemResolver);

            var result = richTextProcessor.Process(richTextWithWrapper, processedContentItems);

            Assert.AreEqual(plainRichText + plainWrapper, result);
                
        }

        [Test]
        public void NestedContentItemInRichTextIsProcessedByValueProcessor()
        {
            var insertedContentName = "dummyCodename1";
            var insertedObject = GetContentItemObjectElement(insertedContentName);
            string wrapperWithObject = WrapElementWithDivs(insertedObject);
            const string insertedContentItemValue = "dummyValue";
            var plainRichText = $"<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var richTextWithWrapper = plainRichText + wrapperWithObject;
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyProcessedContentItem() {Value = insertedContentItemValue} }
            };
            var contentItemResolver = new ResolverReturningValue();
            var richTextProcessor = new ContentItemsInRichTextProcessor(null, null);
            richTextProcessor.RegisterTypeResolver(contentItemResolver);


            var result = richTextProcessor.Process(richTextWithWrapper, processedContentItems);

            Assert.AreEqual(plainRichText + WrapElementWithDivs(insertedContentItemValue), result);

        }

        [Test]
        public void NestedContentItemInRichTextIsProcessedByElementProcessor()
        {
            var insertedContentName = "dummyCodename1";
            var insertedObject = GetContentItemObjectElement(insertedContentName);
            var wrapperWithObject = WrapElementWithDivs(insertedObject);
            var plainRichText = $"<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var richTextWithWrapper = plainRichText + wrapperWithObject;
            const string insertedContentItemValue = "dummyValue";
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyProcessedContentItem() {Value = insertedContentItemValue}}
            };
            var contentItemResolver = new ResolverReturningElement();
            var richTextProcessor = new ContentItemsInRichTextProcessor(null, null);
            richTextProcessor.RegisterTypeResolver(contentItemResolver);

            var result = richTextProcessor.Process(richTextWithWrapper, processedContentItems);

            var expectedElement = $"<span>{insertedContentItemValue}</span>";
            Assert.AreEqual(plainRichText + WrapElementWithDivs(expectedElement), result);
        }

        [Test]
        public void DifferentContentTypesAreResolvedCorrectly()
        {
            const string insertedImage1CodeName = "image1";
            const string insertedImage1Source = "www.images.com/image1.png";
            const string insertedImage2CodeName = "image2";
            const string insertedImage2Source = "www.imagerepository.com/cat.jpg";
            const string insertedDummyItem1CodeName = "item1";
            const string insertedDummyItem1Value = "Leadership!";
            const string insertedDummyItem2CodeName = "item2";
            const string insertedDummyItem2Value = "Teamwork!";
            const string insertedDummyItem3CodeName = "item3";
            const string insertedDummyItem3Value = "Unity!";

            var insertedImage1 = WrapElementWithDivs(GetContentItemObjectElement(insertedImage1CodeName));
            var insertedImage2 = GetContentItemObjectElement(insertedImage2CodeName);
            var insertedDummyItem1 = GetContentItemObjectElement(insertedDummyItem1CodeName);
            var insertedDummyItem2 = WrapElementWithDivs(GetContentItemObjectElement(insertedDummyItem2CodeName));
            var insertedDummyItem3 = GetContentItemObjectElement(insertedDummyItem3CodeName);

            var richTextInput =
                $"Opting out of business line is not a choice. {insertedDummyItem2} A radical, unified, highly-curated and digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings. {insertedImage1} The project leader swiftly enhances market practices in the core. In the same time, an elite, siloed, breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge - based commitments.{insertedDummyItem3} The market thinker dramatically enforces our hands - on brainstorming spaces.Adaptability and skillset invigorate the game changers. {insertedDummyItem1} The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds.It's not about our targets. {insertedImage2} It's about infrastructures.";

            var expectedOutput =
                $"Opting out of business line is not a choice. <div><span>{insertedDummyItem2Value}</span></div> A radical, unified, highly-curated and digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings. <div><img src=\"{insertedImage1Source}\"></div> The project leader swiftly enhances market practices in the core. In the same time, an elite, siloed, breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge - based commitments.<span>{insertedDummyItem3Value}</span> The market thinker dramatically enforces our hands - on brainstorming spaces.Adaptability and skillset invigorate the game changers. <span>{insertedDummyItem1Value}</span> The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds.It's not about our targets. <img src=\"{insertedImage2Source}\"> It's about infrastructures.";


            var processedContentItems = new Dictionary<string, object>
            {
                {insertedImage1CodeName, new DummyImageContentItem() {Source = insertedImage1Source}},
                {insertedImage2CodeName, new DummyImageContentItem() {Source = insertedImage2Source}},
                {insertedDummyItem1CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem1Value}},
                {insertedDummyItem2CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem2Value}},
                {insertedDummyItem3CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem3Value}},
            };
            var richTextProcessor = new ContentItemsInRichTextProcessor(null, null);
            richTextProcessor.RegisterTypeResolver(new ResolverReturningElement());
            richTextProcessor.RegisterTypeResolver(new DummyImageResolver());

            var result = richTextProcessor.Process(richTextInput, processedContentItems);

            Assert.AreEqual(expectedOutput, result);
        }


        [Test]
        public void DifferentContentTypesAndUnretrievedAreResolvedCorrectly()
        {
            const string insertedImage1CodeName = "image1";
            const string insertedImage1Source = "www.images.com/image1.png";
            const string insertedImage2CodeName = "image2";
            const string insertedDummyItem1CodeName = "item1";
            const string insertedDummyItem1Value = "Leadership!";
            const string insertedDummyItem2CodeName = "item2";
            const string insertedDummyItem3CodeName = "item3";
            const string insertedDummyItem3Value = "Unity!";

            const string unretrievedItemMessage = "Unretrieved item detected!";

            var insertedImage1 = WrapElementWithDivs(GetContentItemObjectElement(insertedImage1CodeName));
            var insertedImage2 = GetContentItemObjectElement(insertedImage2CodeName);
            var insertedDummyItem1 = GetContentItemObjectElement(insertedDummyItem1CodeName);
            var insertedDummyItem2 = WrapElementWithDivs(GetContentItemObjectElement(insertedDummyItem2CodeName));
            var insertedDummyItem3 = GetContentItemObjectElement(insertedDummyItem3CodeName);

            var richTextInput =
                $"Opting out of business line is not a choice. {insertedDummyItem2} A radical, unified, highly-curated and digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings. {insertedImage1} The project leader swiftly enhances market practices in the core. In the same time, an elite, siloed, breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge - based commitments.{insertedDummyItem3} The market thinker dramatically enforces our hands - on brainstorming spaces.Adaptability and skillset invigorate the game changers. {insertedDummyItem1} The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds.It's not about our targets. {insertedImage2} It's about infrastructures.";

            var expectedOutput =
                $"Opting out of business line is not a choice. <div>{unretrievedItemMessage}</div> A radical, unified, highly-curated and digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings. <div><img src=\"{insertedImage1Source}\"></div> The project leader swiftly enhances market practices in the core. In the same time, an elite, siloed, breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge - based commitments.<span>{insertedDummyItem3Value}</span> The market thinker dramatically enforces our hands - on brainstorming spaces.Adaptability and skillset invigorate the game changers. <span>{insertedDummyItem1Value}</span> The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds.It's not about our targets. {unretrievedItemMessage} It's about infrastructures.";


            var processedContentItems = new Dictionary<string, object>
            {
                {insertedImage1CodeName, new DummyImageContentItem() {Source = insertedImage1Source}},
                {insertedImage2CodeName, new UnretrievedContentItem()},
                {insertedDummyItem1CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem1Value}},
                {insertedDummyItem2CodeName, new UnretrievedContentItem()},
                {insertedDummyItem3CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem3Value}},
            };
            var unretrievedContentItemsInRichTextResolver = new UnretrievedItemsMessageReturningResolver(unretrievedItemMessage);
            var richTextProcessor = new ContentItemsInRichTextProcessor(null, unretrievedContentItemsInRichTextResolver);
            richTextProcessor.RegisterTypeResolver(new ResolverReturningElement());
            richTextProcessor.RegisterTypeResolver(new DummyImageResolver());

            var result = richTextProcessor.Process(richTextInput, processedContentItems);

            Assert.AreEqual(expectedOutput, result);
        }

        [Test]
        public void DifferentContentTypesUnretrievedAndContentTypesWithoutResolverAreResolvedCorrectly()
        {
            const string insertedImage1CodeName = "image1";
            const string insertedImage1Source = "www.images.com/image1.png";
            const string insertedImage2CodeName = "image2";
            const string insertedDummyItem1CodeName = "item1";
            const string insertedDummyItem1Value = "Leadership!";
            const string insertedDummyItem2CodeName = "item2";
            const string insertedDummyItem3CodeName = "item3";
            const string insertedDummyItem3Value = "Unity!";

            const string unretrievedItemMessage = "Unretrieved item detected!";
            const string defaultResolverMessage = "Type witout resolver detected!";

            var insertedImage1 = WrapElementWithDivs(GetContentItemObjectElement(insertedImage1CodeName));
            var insertedImage2 = GetContentItemObjectElement(insertedImage2CodeName);
            var insertedDummyItem1 = GetContentItemObjectElement(insertedDummyItem1CodeName);
            var insertedDummyItem2 = WrapElementWithDivs(GetContentItemObjectElement(insertedDummyItem2CodeName));
            var insertedDummyItem3 = GetContentItemObjectElement(insertedDummyItem3CodeName);

            var richTextInput =
                $"Opting out of business line is not a choice. {insertedDummyItem2} A radical, unified, highly-curated and digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings. {insertedImage1} The project leader swiftly enhances market practices in the core. In the same time, an elite, siloed, breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge - based commitments.{insertedDummyItem3} The market thinker dramatically enforces our hands - on brainstorming spaces.Adaptability and skillset invigorate the game changers. {insertedDummyItem1} The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds.It's not about our targets. {insertedImage2} It's about infrastructures.";

            var expectedOutput =
                $"Opting out of business line is not a choice. <div>{unretrievedItemMessage}</div> A radical, unified, highly-curated and digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings. <div>{defaultResolverMessage}</div> The project leader swiftly enhances market practices in the core. In the same time, an elite, siloed, breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge - based commitments.<span>{insertedDummyItem3Value}</span> The market thinker dramatically enforces our hands - on brainstorming spaces.Adaptability and skillset invigorate the game changers. <span>{insertedDummyItem1Value}</span> The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds.It's not about our targets. {unretrievedItemMessage} It's about infrastructures.";


            var processedContentItems = new Dictionary<string, object>
            {
                {insertedImage1CodeName, new DummyImageContentItem() {Source = insertedImage1Source}},
                {insertedImage2CodeName, new UnretrievedContentItem()},
                {insertedDummyItem1CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem1Value}},
                {insertedDummyItem2CodeName, new UnretrievedContentItem()},
                {insertedDummyItem3CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem3Value}},
            };
            var unretrievedContentItemsInRichTextResolver = new UnretrievedItemsMessageReturningResolver(unretrievedItemMessage);
            var defaultResolver = new MessageReturningResolver(defaultResolverMessage);
            var richTextProcessor = new ContentItemsInRichTextProcessor(defaultResolver, unretrievedContentItemsInRichTextResolver);
            richTextProcessor.RegisterTypeResolver(new ResolverReturningElement());


            var result = richTextProcessor.Process(richTextInput, processedContentItems);

            Assert.AreEqual(expectedOutput, result);
        }


        [Test]
        public void UnretrievedContentItemIsResolvedByUnretrievedProcessor()
        {
            const string insertedContentName = "dummyCodename1";
            const string message = "Unretrieved item detected";
            var insertedObject = GetContentItemObjectElement(insertedContentName);
            var wrapperWithObject = WrapElementWithDivs(insertedObject);
            var plainRichText = "<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var richTextWithWrapper = plainRichText + wrapperWithObject;
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new UnretrievedContentItem()}
            };
            var unresolvedContentItemResolver = new UnretrievedItemsMessageReturningResolver(message);
            var richTextProcessor = new ContentItemsInRichTextProcessor(null, unresolvedContentItemResolver);

            var result = richTextProcessor.Process(richTextWithWrapper, processedContentItems);

            Assert.AreEqual(plainRichText + $"<div>{message}</div>", result);
        }



        [Test]
        public void ContentItemWithoutResolverIsHandledByDefaultResolver()
        {
            const string insertedContentName = "dummyCodename1";
            const string message = "Default handler";
            var wrapperWithObject = WrapElementWithDivs(GetContentItemObjectElement(insertedContentName));
            var plainRichText = "<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var richTextWithWrapper = plainRichText + wrapperWithObject;
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyProcessedContentItem()}
            };
            var differentResolver = new MessageReturningResolver("this should not appear");
            var defaultResolver = new MessageReturningResolver(message);
            var richTextProcessor = new ContentItemsInRichTextProcessor(defaultResolver, null);
            richTextProcessor.RegisterTypeResolver(differentResolver);

            var result = richTextProcessor.Process(richTextWithWrapper, processedContentItems);

            Assert.AreEqual(plainRichText + $"<div>{message}</div>", result);
        }

        [Test]
        public void ResolverReturningMixedElementsAndTextIsProcessedCorrectly()
        {
            const string insertedContentName = "dummyCodename1";
            var wrapperWithObject = GetContentItemObjectElement(insertedContentName);

            var inputRichText = $"A hyper-hybrid socialization &amp; turbocharges adaptive {wrapperWithObject} frameworks by thinking outside of the box, while the support structures influence the mediators.";
            const string insertedContentItemValue = "dummyValue";
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyProcessedContentItem() {Value = insertedContentItemValue}}
            };
            var contentItemResolver = new ResolverReturningTextAndElement();
            var richTextProcessor = new ContentItemsInRichTextProcessor(null, null);
            richTextProcessor.RegisterTypeResolver(contentItemResolver);

            var result = richTextProcessor.Process(inputRichText, processedContentItems);

            var expectedResults = $"A hyper-hybrid socialization &amp; turbocharges adaptive Text text brackets ( &lt; [ <span>{insertedContentItemValue}</span><div></div>&amp; Some more text frameworks by thinking outside of the box, while the support structures influence the mediators.";

            Assert.AreEqual(expectedResults, result);
        }

        [Test]
        public void ResolverReturningIncorrectHtmlReturnsErrorMessage()
        {
            const string insertedContentName = "dummyCodename1";
            var wrapperWithObject = GetContentItemObjectElement(insertedContentName);

            var inputRichText = $"A hyper-hybrid socialization &amp; turbocharges adaptive {wrapperWithObject} frameworks by thinking outside of the box, while the support structures influence the mediators.";
            const string insertedContentItemValue = "dummyValue";
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyProcessedContentItem() {Value = insertedContentItemValue}}
            };
            var contentItemResolver = new ResolverReturningIncorrectHtml();
            var richTextProcessor = new ContentItemsInRichTextProcessor(null, null);
            richTextProcessor.RegisterTypeResolver(contentItemResolver);

            var result = richTextProcessor.Process(inputRichText, processedContentItems);

            var expectedResults = "A hyper-hybrid socialization &amp; turbocharges adaptive Error while parsing resolvers output for content type KenticoCloud.Delivery.Tests.ContentItemsInRichTextProcessorTests+DummyProcessedContentItem, codename dummyCodename1 at line 1, column 24 frameworks by thinking outside of the box, while the support structures influence the mediators.";

            Assert.AreEqual(expectedResults, result);
        }


        private class DummyResolver : IContentItemsInRichTextResolver<DummyProcessedContentItem>
        {
            public string Resolve(ResolvedContentItemData<DummyProcessedContentItem> item)
            {
                return string.Empty;
            }
        }

        private class DummyProcessedContentItem
        {
            public string Value { get; set; }
        }

        private class DummyImageContentItem
        {
            public string Source { get; set; }
        }

        private class ResolverReturningValue : IContentItemsInRichTextResolver<DummyProcessedContentItem>
        {
            public string Resolve(ResolvedContentItemData<DummyProcessedContentItem> data)
            {
                return data.Item?.Value ?? string.Empty;
            }
        }

        private class DummyImageResolver : IContentItemsInRichTextResolver<DummyImageContentItem>
        {
            public string Resolve(ResolvedContentItemData<DummyImageContentItem> data)
            {
                return $"<img src=\"{data.Item.Source}\" />";
            }
        }

        private class ResolverReturningElement : IContentItemsInRichTextResolver<DummyProcessedContentItem>
        {
            public string Resolve(ResolvedContentItemData<DummyProcessedContentItem> data)
            {
                return $"<span>{data.Item.Value}</span>";
            }
        }

        private class ResolverReturningTextAndElement : IContentItemsInRichTextResolver<DummyProcessedContentItem>
        {
            public string Resolve(ResolvedContentItemData<DummyProcessedContentItem> data)
            {
                return $"Text text brackets ( &lt; [ <span>{data.Item.Value}</span><div></div>&amp; Some more text";
            }
        }

        private class ResolverReturningIncorrectHtml : IContentItemsInRichTextResolver<DummyProcessedContentItem>
        {
            public string Resolve(ResolvedContentItemData<DummyProcessedContentItem> data)
            {
                return $"<span>Unclosed span tag";
            }
        }

        private class MessageReturningResolver : IContentItemsInRichTextResolver<object>
        {
            private readonly string _message;

            public MessageReturningResolver(string message)
            {
                _message = message;
            }
            public string Resolve(ResolvedContentItemData<object> item)
            {
                return _message;
            }
        }

        private class UnretrievedItemsMessageReturningResolver : IContentItemsInRichTextResolver<UnretrievedContentItem>
        {
            private readonly string _message;

            public UnretrievedItemsMessageReturningResolver(string message)
            {
                _message = message;
            }
            public string Resolve(ResolvedContentItemData<UnretrievedContentItem> item)
            {
                return _message;
            }
        }

        private static string GetContentItemObjectElement(string insertedContentName)
        {
            return $"<object type=\"{ContentItemType}\" data-type=\"{ContentItemDataType}\" data-codename=\"{insertedContentName}\"></object/>";
        }

        private static string WrapElementWithDivs(string insertedObject)
        {
            return "<div>" + insertedObject + "</div>";
        }
    }
}