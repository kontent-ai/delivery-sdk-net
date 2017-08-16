using System;
using Xunit;
using System.Collections.Generic;
using KenticoCloud.Delivery.InlineContentItems;

namespace KenticoCloud.Delivery.Tests
{
    public class ContentItemsInRichTextProcessorTests
    {
        private const string ContentItemType = "application/kenticocloud";
        private const string ContentItemDataType = "item";

        [Fact]
        public void ProcessedHtmlIsSameIfNoContentItemsAreIncluded()
        {
            var inputHtml = $"<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var inlineContentItemsProcessor = new InlineContentItemsProcessor(null, null);
            var processedContentItems = new Dictionary<string, object>();

            var result = inlineContentItemsProcessor.Process(inputHtml, processedContentItems);

            Assert.Equal(inputHtml, result);
        }

        [Fact]
        public void InlineContentItemsAreProcessedByDummyProcessor()
        {
            var insertedContentName1 = "dummyCodename1";
            var insertedContentName2 = "dummyCodename2";
            var insertedObject1 = GetContentItemObjectElement(insertedContentName1);
            var insertedObject2 = GetContentItemObjectElement(insertedContentName2);
            var plainHtml = $"<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var input = insertedObject1 + plainHtml + insertedObject2;
            var contentItemResolver = new DummyResolver();
            var inlineContentItemsProcessor = new InlineContentItemsProcessor(null, null);
            inlineContentItemsProcessor.RegisterTypeResolver(contentItemResolver);
            var processedContentItems = new Dictionary<string, object>() {{insertedContentName1, new DummyProcessedContentItem()}, {insertedContentName2, new DummyProcessedContentItem()} };

            var result = inlineContentItemsProcessor.Process(input, processedContentItems);

            Assert.Equal(plainHtml, result);
        }

        [Fact]
        public void NestedInlineContentItemIsProcessedByDummyProcessor()
        {
            var insertedContentName = "dummyCodename1";
            string wrapperWithObject = WrapElementWithDivs(GetContentItemObjectElement(insertedContentName));
            var plainHtml = $"<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var input = plainHtml + wrapperWithObject;
            var processedContentItems = new Dictionary<string, object>()
            {
                {insertedContentName, new DummyProcessedContentItem()}
            };
            var contentItemResolver = new DummyResolver();
            var inlineContentItemsProcessor = new InlineContentItemsProcessor(null, null);
            inlineContentItemsProcessor.RegisterTypeResolver(contentItemResolver);

            var result = inlineContentItemsProcessor.Process(input, processedContentItems);

            Assert.Equal(plainHtml + WrapElementWithDivs(string.Empty), result);
            Assert.Equal(1, contentItemResolver.callsForResolve);

        }

        [Fact]
        public void NestedInlineContentItemIsProcessedByValueProcessor()
        {
            var insertedContentName = "dummyCodename1";
            string wrapperWithObject = WrapElementWithDivs(GetContentItemObjectElement(insertedContentName));
            const string insertedContentItemValue = "dummyValue";
            var plainHtml = $"<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var input = plainHtml + wrapperWithObject;
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyProcessedContentItem() {Value = insertedContentItemValue} }
            };
            var contentItemResolver = new ResolverReturningValue();
            var inlineContentItemsProcessor = new InlineContentItemsProcessor(null, null);
            inlineContentItemsProcessor.RegisterTypeResolver(contentItemResolver);


            var result = inlineContentItemsProcessor.Process(input, processedContentItems);

            Assert.Equal(plainHtml + WrapElementWithDivs(insertedContentItemValue), result);

        }

        [Fact]
        public void NestedInlineContentItemIsProcessedByElementProcessor()
        {
            var insertedContentName = "dummyCodename1";
            var wrapperWithObject = WrapElementWithDivs(GetContentItemObjectElement(insertedContentName));
            var plainHtml = $"<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var input = plainHtml + wrapperWithObject;
            const string insertedContentItemValue = "dummyValue";
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyProcessedContentItem() {Value = insertedContentItemValue}}
            };
            var contentItemResolver = new ResolverReturningElement();
            var inlineContentItemsProcessor = new InlineContentItemsProcessor(null, null);
            inlineContentItemsProcessor.RegisterTypeResolver(contentItemResolver);

            var result = inlineContentItemsProcessor.Process(input, processedContentItems);

            var expectedElement = $"<span>{insertedContentItemValue}</span>";
            Assert.Equal(plainHtml + WrapElementWithDivs(expectedElement), result);
        }

        [Fact]
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

            var htmlInput =
                $"Opting out of business line is not a choice. {insertedDummyItem2} A radical, unified, highly-curated and" +
                $" digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned" +
                $" brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings." +
                $" {insertedImage1} The project leader swiftly enhances market practices in the core. In the same time," +
                $" an elite, siloed, breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge" +
                $" - based commitments.{insertedDummyItem3} The market thinker dramatically enforces our hands" +
                $" - on brainstorming spaces.Adaptability and skillset invigorate the game changers. {insertedDummyItem1}" +
                $" The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds.It's not about" +
                $" our targets. {insertedImage2} It's about infrastructures.";

            var expectedOutput =
                $"Opting out of business line is not a choice. <div><span>{insertedDummyItem2Value}</span></div> A radical, unified, highly-curated and" +
                $" digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned" +
                $" brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings." +
                $" <div><img src=\"{insertedImage1Source}\"></div> The project leader swiftly enhances market practices in the core. In the same time," +
                $" an elite, siloed, breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge" +
                $" - based commitments.<span>{insertedDummyItem3Value}</span> The market thinker dramatically enforces our hands" +
                $" - on brainstorming spaces.Adaptability and skillset invigorate the game changers. <span>{insertedDummyItem1Value}</span>" +
                $" The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds.It's not about" +
                $" our targets. <img src=\"{insertedImage2Source}\"> It's about infrastructures.";


            var processedContentItems = new Dictionary<string, object>
            {
                {insertedImage1CodeName, new DummyImageContentItem() {Source = insertedImage1Source}},
                {insertedImage2CodeName, new DummyImageContentItem() {Source = insertedImage2Source}},
                {insertedDummyItem1CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem1Value}},
                {insertedDummyItem2CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem2Value}},
                {insertedDummyItem3CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem3Value}},
            };
            var inlineContentItemsProcessor = new InlineContentItemsProcessor(null, null);
            inlineContentItemsProcessor.RegisterTypeResolver(new ResolverReturningElement());
            inlineContentItemsProcessor.RegisterTypeResolver(new DummyImageResolver());

            var result = inlineContentItemsProcessor.Process(htmlInput, processedContentItems);

            Assert.Equal(expectedOutput, result);
        }


        [Fact]
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

            var htmlInput =
                $"Opting out of business line is not a choice. {insertedDummyItem2} A radical, unified, highly-curated and" +
                $" digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned" +
                $" brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings." +
                $" {insertedImage1} The project leader swiftly enhances market practices in the core. In the same time," +
                $" an elite, siloed, breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge" +
                $" - based commitments.{insertedDummyItem3} The market thinker dramatically enforces our hands" +
                $" - on brainstorming spaces.Adaptability and skillset invigorate the game changers. {insertedDummyItem1}" +
                $" The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds." +
                $"It's not about our targets. {insertedImage2} It's about infrastructures.";

            var expectedOutput =
                $"Opting out of business line is not a choice. <div>{unretrievedItemMessage}</div> A radical, unified, highly-curated and" +
                $" digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned" +
                $" brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings." +
                $" <div><img src=\"{insertedImage1Source}\"></div> The project leader swiftly enhances market practices in the core. In the same time," +
                $" an elite, siloed, breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge" +
                $" - based commitments.<span>{insertedDummyItem3Value}</span> The market thinker dramatically enforces our hands" +
                $" - on brainstorming spaces.Adaptability and skillset invigorate the game changers. <span>{insertedDummyItem1Value}</span>" +
                $" The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds." +
                $"It's not about our targets. {unretrievedItemMessage} It's about infrastructures.";


            var processedContentItems = new Dictionary<string, object>
            {
                {insertedImage1CodeName, new DummyImageContentItem() {Source = insertedImage1Source}},
                {insertedImage2CodeName, new UnretrievedContentItem()},
                {insertedDummyItem1CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem1Value}},
                {insertedDummyItem2CodeName, new UnretrievedContentItem()},
                {insertedDummyItem3CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem3Value}},
            };
            var unretrievedInlineContentItemsResolver = new UnretrievedItemsMessageReturningResolver(unretrievedItemMessage);
            var inlineContentItemsProcessor = new InlineContentItemsProcessor(null, unretrievedInlineContentItemsResolver);
            inlineContentItemsProcessor.RegisterTypeResolver(new ResolverReturningElement());
            inlineContentItemsProcessor.RegisterTypeResolver(new DummyImageResolver());

            var result = inlineContentItemsProcessor.Process(htmlInput, processedContentItems);

            Assert.Equal(expectedOutput, result);
        }

        [Fact]
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

            var htmlInput =
                $"Opting out of business line is not a choice. {insertedDummyItem2} A radical, unified, highly-curated and" +
                $" digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned" +
                $" brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings. {insertedImage1}" +
                $" The project leader swiftly enhances market practices in the core. In the same time, an elite, siloed," +
                $" breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge" +
                $" - based commitments.{insertedDummyItem3} The market thinker dramatically enforces our hands" +
                $" - on brainstorming spaces.Adaptability and skillset invigorate the game changers. {insertedDummyItem1}" +
                $" The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds." +
                $"It's not about our targets. {insertedImage2} It's about infrastructures.";

            var expectedOutput =
                $"Opting out of business line is not a choice. <div>{unretrievedItemMessage}</div> A radical, unified, highly-curated and" +
                $" digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned" +
                $" brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings. <div>{defaultResolverMessage}</div>" +
                $" The project leader swiftly enhances market practices in the core. In the same time, an elite, siloed," +
                $" breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge" +
                $" - based commitments.<span>{insertedDummyItem3Value}</span> The market thinker dramatically enforces our hands" +
                $" - on brainstorming spaces.Adaptability and skillset invigorate the game changers. <span>{insertedDummyItem1Value}</span>" +
                $" The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds." +
                $"It's not about our targets. {unretrievedItemMessage} It's about infrastructures.";


            var processedContentItems = new Dictionary<string, object>
            {
                {insertedImage1CodeName, new DummyImageContentItem() {Source = insertedImage1Source}},
                {insertedImage2CodeName, new UnretrievedContentItem()},
                {insertedDummyItem1CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem1Value}},
                {insertedDummyItem2CodeName, new UnretrievedContentItem()},
                {insertedDummyItem3CodeName, new DummyProcessedContentItem() {Value = insertedDummyItem3Value}},
            };
            var unretrievedInlineContentItemsResolver = new UnretrievedItemsMessageReturningResolver(unretrievedItemMessage);
            var defaultResolver = new MessageReturningResolver(defaultResolverMessage);
            var inlineContentItemsProcessor = new InlineContentItemsProcessor(defaultResolver, unretrievedInlineContentItemsResolver);
            inlineContentItemsProcessor.RegisterTypeResolver(new ResolverReturningElement());


            var result = inlineContentItemsProcessor.Process(htmlInput, processedContentItems);

            Assert.Equal(expectedOutput, result);
        }


        [Fact]
        public void UnretrievedContentItemIsResolvedByUnretrievedProcessor()
        {
            const string insertedContentName = "dummyCodename1";
            const string message = "Unretrieved item detected";
            var insertedObject = GetContentItemObjectElement(insertedContentName);
            var wrapperWithObject = WrapElementWithDivs(insertedObject);
            var plainHtml = "<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var input = plainHtml + wrapperWithObject;
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new UnretrievedContentItem()}
            };
            var unresolvedContentItemResolver = new UnretrievedItemsMessageReturningResolver(message);
            var inlineContentItemsProcessor = new InlineContentItemsProcessor(null, unresolvedContentItemResolver);

            var result = inlineContentItemsProcessor.Process(input, processedContentItems);

            Assert.Equal(plainHtml + $"<div>{message}</div>", result);
        }



        [Fact]
        public void ContentItemWithoutResolverIsHandledByDefaultResolver()
        {
            const string insertedContentName = "dummyCodename1";
            const string message = "Default handler";
            var wrapperWithObject = WrapElementWithDivs(GetContentItemObjectElement(insertedContentName));
            var plainHtml = "<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var input = plainHtml + wrapperWithObject;
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyProcessedContentItem()}
            };
            var differentResolver = new MessageReturningResolver("this should not appear");
            var defaultResolver = new MessageReturningResolver(message);
            var inlineContentItemsProcessor = new InlineContentItemsProcessor(defaultResolver, null);
            inlineContentItemsProcessor.RegisterTypeResolver(differentResolver);

            var result = inlineContentItemsProcessor.Process(input, processedContentItems);

            Assert.Equal(plainHtml + $"<div>{message}</div>", result);
        }

        [Fact]
        public void ResolverReturningMixedElementsAndTextIsProcessedCorrectly()
        {
            const string insertedContentName = "dummyCodename1";
            var wrapperWithObject = GetContentItemObjectElement(insertedContentName);

            var inputHtml = $"A hyper-hybrid socialization &amp; turbocharges adaptive {wrapperWithObject} frameworks by thinking outside of the box, while the support structures influence the mediators.";
            const string insertedContentItemValue = "dummyValue";
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyProcessedContentItem() {Value = insertedContentItemValue}}
            };
            var contentItemResolver = new ResolverReturningTextAndElement();
            var inlineContentItemsProcessor = new InlineContentItemsProcessor(null, null);
            inlineContentItemsProcessor.RegisterTypeResolver(contentItemResolver);

            var result = inlineContentItemsProcessor.Process(inputHtml, processedContentItems);

            var expectedResults = $"A hyper-hybrid socialization &amp; turbocharges adaptive Text text brackets ( &lt; [ <span>{insertedContentItemValue}</span><div></div>&amp; Some more text frameworks by thinking outside of the box, while the support structures influence the mediators.";

            Assert.Equal(expectedResults, result);
        }

        [Fact]
        public void ResolverReturningIncorrectHtmlReturnsErrorMessage()
        {
            const string insertedContentName = "dummyCodename1";
            var wrapperWithObject = GetContentItemObjectElement(insertedContentName);

            var inputHtml = $"A hyper-hybrid socialization &amp; turbocharges adaptive {wrapperWithObject} frameworks by thinking outside of the box, while the support structures influence the mediators.";
            const string insertedContentItemValue = "dummyValue";
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyProcessedContentItem() {Value = insertedContentItemValue}}
            };
            var contentItemResolver = new ResolverReturningIncorrectHtml();
            var inlineContentItemsProcessor = new InlineContentItemsProcessor(null, null);
            inlineContentItemsProcessor.RegisterTypeResolver(contentItemResolver);

            var result = inlineContentItemsProcessor.Process(inputHtml, processedContentItems);

            var expectedResults = "A hyper-hybrid socialization &amp; turbocharges adaptive [Inline content item resolver provided an invalid HTML 5 fragment (1:3). Please check the output for a content item dummyCodename1 of type KenticoCloud.Delivery.Tests.ContentItemsInRichTextProcessorTests+DummyProcessedContentItem.] frameworks by thinking outside of the box, while the support structures influence the mediators.";

            Assert.Equal(expectedResults, result);
        }

        [Fact]
        public void ProcessorRemoveAllRemovesAllInlineContentItems()
        {
            const string insertedImage1CodeName = "image1";
            const string insertedImage2CodeName = "image2";
            const string insertedDummyItem1CodeName = "item1";
            const string insertedDummyItem2CodeName = "item2";
            const string insertedDummyItem3CodeName = "item3";

            var insertedImage1 = WrapElementWithDivs(GetContentItemObjectElement(insertedImage1CodeName));
            var insertedImage2 = GetContentItemObjectElement(insertedImage2CodeName);
            var insertedDummyItem1 = GetContentItemObjectElement(insertedDummyItem1CodeName);
            var insertedDummyItem2 = WrapElementWithDivs(GetContentItemObjectElement(insertedDummyItem2CodeName));
            var insertedDummyItem3 = GetContentItemObjectElement(insertedDummyItem3CodeName);

            var htmlInput =
                $"Opting out of business line is not a choice. {insertedDummyItem2} A radical, unified, highly-curated and" +
                $" digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned" +
                $" brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings. {insertedImage1}" +
                $" The project leader swiftly enhances market practices in the core. In the same time, an elite, siloed," +
                $" breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge" +
                $" - based commitments.{insertedDummyItem3} The market thinker dramatically enforces our hands" +
                $" - on brainstorming spaces.Adaptability and skillset invigorate the game changers. {insertedDummyItem1}" +
                $" The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds." +
                $"It's not about our targets. {insertedImage2} It's about infrastructures.";
            var expectedOutput = 
                $"Opting out of business line is not a choice. {WrapElementWithDivs(string.Empty)} A radical, unified, highly-curated and" +
                $" digitized realignment transfers a touchpoint. As a result, the attackers empower our well-planned" +
                $" brainstorming spaces. It's not about our evidence-based customer centricity. It's about brandings. {WrapElementWithDivs(string.Empty)}" +
                $" The project leader swiftly enhances market practices in the core. In the same time, an elite, siloed," +
                $" breakthrough generates our value-added cross fertilization.\n" +
                $"Our pre-plan prioritizes the group.Our top-level, service - oriented, ingenuity leverages knowledge" +
                $" - based commitments. The market thinker dramatically enforces our hands" +
                $" - on brainstorming spaces.Adaptability and skillset invigorate the game changers. " +
                $" The thought leaders target a teamwork-oriented silo.\n" +
                $"A documented high quality enables our unique, outside -in and customer-centric tailwinds." +
                $"It's not about our targets.  It's about infrastructures.";
            var processor = new InlineContentItemsProcessor(null, null);

            var result = processor.RemoveAll(htmlInput);

            Assert.Equal(expectedOutput, result);

        }



        private class DummyResolver : IInlineContentItemsResolver<DummyProcessedContentItem>
        {
            public int callsForResolve;
            public string Resolve(ResolvedContentItemData<DummyProcessedContentItem> item)
            {
                callsForResolve++;
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

        private class ResolverReturningValue : IInlineContentItemsResolver<DummyProcessedContentItem>
        {
            public string Resolve(ResolvedContentItemData<DummyProcessedContentItem> data)
            {
                return data.Item?.Value ?? string.Empty;
            }
        }

        private class DummyImageResolver : IInlineContentItemsResolver<DummyImageContentItem>
        {
            public string Resolve(ResolvedContentItemData<DummyImageContentItem> data)
            {
                return $"<img src=\"{data.Item.Source}\" />";
            }
        }

        private class ResolverReturningElement : IInlineContentItemsResolver<DummyProcessedContentItem>
        {
            public string Resolve(ResolvedContentItemData<DummyProcessedContentItem> data)
            {
                return $"<span>{data.Item.Value}</span>";
            }
        }

        private class ResolverReturningTextAndElement : IInlineContentItemsResolver<DummyProcessedContentItem>
        {
            public string Resolve(ResolvedContentItemData<DummyProcessedContentItem> data)
            {
                return $"Text text brackets ( &lt; [ <span>{data.Item.Value}</span><div></div>&amp; Some more text";
            }
        }

        private class ResolverReturningIncorrectHtml : IInlineContentItemsResolver<DummyProcessedContentItem>
        {
            public string Resolve(ResolvedContentItemData<DummyProcessedContentItem> data)
            {
                return $"<![CDATA[ test ]]>";
            }
        }

        private class MessageReturningResolver : IInlineContentItemsResolver<object>
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

        private class UnretrievedItemsMessageReturningResolver : IInlineContentItemsResolver<UnretrievedContentItem>
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