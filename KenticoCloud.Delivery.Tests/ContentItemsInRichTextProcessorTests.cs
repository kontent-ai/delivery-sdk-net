using Xunit;
using System.Collections.Generic;
using KenticoKontent.Delivery.InlineContentItems;
using KenticoKontent.Delivery.Tests.Factories;

namespace KenticoKontent.Delivery.Tests
{
    public class ContentItemsInRichTextProcessorTests
    {
        private const string ContentItemType = "application/kenticocloud";
        private const string ContentItemDataType = "item";

        [Fact]
        public void ProcessedHtmlIsSameIfNoContentItemsAreIncluded()
        {
            var inputHtml = "<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory.Create();
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
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(factory => factory.ResolveToMessage<DummyItem>(string.Empty))
                .Build();
            var processedContentItems = new Dictionary<string, object> {{insertedContentName1, new DummyItem()}, {insertedContentName2, new DummyItem()} };

            var result = inlineContentItemsProcessor.Process(input, processedContentItems);

            Assert.Equal(plainHtml, result);
        }

        [Fact]
        public void NestedInlineContentItemIsProcessedByDummyProcessor()
        {
            var insertedContentName = "dummyCodename1";
            var callsForResolve = 0;
            string wrapperWithObject = WrapElementWithDivs(GetContentItemObjectElement(insertedContentName));
            var plainHtml = $"<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var input = plainHtml + wrapperWithObject;
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyItem()},
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(factory => factory.ResolveTo<DummyItem>(_ => { callsForResolve++; return string.Empty; }))
                .Build();

            var result = inlineContentItemsProcessor.Process(input, processedContentItems);

            Assert.Equal(plainHtml + WrapElementWithDivs(string.Empty), result);
            Assert.Equal(1, callsForResolve);

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
                {insertedContentName, new DummyItem {Value = insertedContentItemValue} }
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(factory => factory.ResolveTo<DummyItem>(item => item.Value ?? string.Empty))
                .Build();


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
                {insertedContentName, new DummyItem {Value = insertedContentItemValue}}
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(ResolveDummyItemToSpan)
                .Build();

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
                {insertedImage1CodeName, new DummyImageItem {Source = insertedImage1Source}},
                {insertedImage2CodeName, new DummyImageItem {Source = insertedImage2Source}},
                {insertedDummyItem1CodeName, new DummyItem {Value = insertedDummyItem1Value}},
                {insertedDummyItem2CodeName, new DummyItem {Value = insertedDummyItem2Value}},
                {insertedDummyItem3CodeName, new DummyItem {Value = insertedDummyItem3Value}},
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(ResolveDummyItemToSpan)
                .AndResolver(ResolveDummyImageToImg)
                .Build();

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
                {insertedImage1CodeName, new DummyImageItem {Source = insertedImage1Source}},
                {insertedImage2CodeName, new UnretrievedContentItem()},
                {insertedDummyItem1CodeName, new DummyItem {Value = insertedDummyItem1Value}},
                {insertedDummyItem2CodeName, new UnretrievedContentItem()},
                {insertedDummyItem3CodeName, new DummyItem {Value = insertedDummyItem3Value}},
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(ResolveDummyItemToSpan)
                .AndResolver(ResolveDummyImageToImg)
                .AndResolver(factory => factory.ResolveToMessage<UnretrievedContentItem>(unretrievedItemMessage))
                .Build();

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
                {insertedImage1CodeName, new DummyImageItem {Source = insertedImage1Source}},
                {insertedImage2CodeName, new UnretrievedContentItem()},
                {insertedDummyItem1CodeName, new DummyItem {Value = insertedDummyItem1Value}},
                {insertedDummyItem2CodeName, new UnretrievedContentItem()},
                {insertedDummyItem3CodeName, new DummyItem {Value = insertedDummyItem3Value}},
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(ResolveDummyItemToSpan)
                .AndResolver(factory => factory.ResolveByDefaultToMessage(defaultResolverMessage))
                .AndResolver(factory => factory.ResolveToMessage<UnretrievedContentItem>(unretrievedItemMessage))
                .Build();


            var result = inlineContentItemsProcessor.Process(htmlInput, processedContentItems);

            Assert.Equal(expectedOutput, result);
        }


        [Fact]
        public void UnretrievedContentItemIsResolvedByUnretrievedProcessor()
        {
            const string insertedContentName = "dummyCodename1";
            var insertedObject = GetContentItemObjectElement(insertedContentName);
            var wrapperWithObject = WrapElementWithDivs(insertedObject);
            var plainHtml = "<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var input = plainHtml + wrapperWithObject;
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new UnretrievedContentItem()}
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(factory => factory.ResolveToType<UnretrievedContentItem>())
                .Build();

            var result = inlineContentItemsProcessor.Process(input, processedContentItems);

            Assert.Equal(plainHtml + $"<div>{typeof(UnretrievedContentItem)}</div>", result);
        }

        [Fact]
        public void ContentItemWithoutModelIsResolvedByUnknownItemProcessor()
        {
            const string insertedContentName = "dummyCodename1";
            var insertedObject = GetContentItemObjectElement(insertedContentName);
            var wrapperWithObject = WrapElementWithDivs(insertedObject);
            var plainHtml = "<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var input = plainHtml + wrapperWithObject;
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, null}
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(factory => factory.ResolveToType<UnknownContentItem>(acceptNull: true))
                .Build();

            var result = inlineContentItemsProcessor.Process(input, processedContentItems);

            Assert.Equal(plainHtml + $"<div>{typeof(UnknownContentItem)}</div>", result);
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
                {insertedContentName, new DummyItem()}
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(factory => factory.ResolveByDefaultToMessage(message))
                .AndResolver(ResolveDummyImageToImg)
                .Build();

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
                {insertedContentName, new DummyItem {Value = insertedContentItemValue}}
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(factory => factory.ResolveTo<DummyItem>(item => $"Text text brackets ( &lt; [ <span>{item.Value}</span><div></div>&amp; Some more text"))
                .Build();

            var result = inlineContentItemsProcessor.Process(inputHtml, processedContentItems);

            var expectedResults = $"A hyper-hybrid socialization &amp; turbocharges adaptive Text text brackets ( &lt; [ <span>{insertedContentItemValue}</span><div></div>&amp; Some more text frameworks by thinking outside of the box, while the support structures influence the mediators.";

            Assert.Equal(expectedResults, result);
        }

        [Fact]
        public void ResolverReturningIncorrectHtmlReturnsErrorMessage()
        {
            const string insertedContentName = "dummyCodename1";
            var wrapperWithObject = GetContentItemObjectElement(insertedContentName);

            var inputHtml = 
                $"A hyper-hybrid socialization &amp; turbocharges adaptive {wrapperWithObject} frameworks"
                + " by thinking outside of the box, while the support structures influence the mediators.";
            const string insertedContentItemValue = "dummyValue";
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyItem {Value = insertedContentItemValue}}
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(factory => factory.ResolveTo<DummyItem>(_ => "<![CDATA[ test ]]>"))
                .Build();

            var result = inlineContentItemsProcessor.Process(inputHtml, processedContentItems);

            var expectedResults = 
                "A hyper-hybrid socialization &amp; turbocharges adaptive [Inline content item resolver provided an invalid HTML 5 fragment (1:3)."
                + $" Please check the output for a content item dummyCodename1 of type {typeof(DummyItem).FullName}.]"
                + " frameworks by thinking outside of the box, while the support structures influence the mediators.";

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
            var processor = InlineContentItemsProcessorFactory.Create();

            var result = processor.RemoveAll(htmlInput);

            Assert.Equal(expectedOutput, result);

        }

        [Fact]
        public void ContentItemWithMultipleResolversIsHandledByLastResolver()
        {
            const string insertedItemName = "dummyItem";
            const string insertedImageName = "dummyImage";
            var wrapperWithItem = WrapElementWithDivs(GetContentItemObjectElement(insertedItemName));
            var wrapperWithImage = WrapElementWithDivs(GetContentItemObjectElement(insertedImageName));
            var plainHtml = "<p>Lorem ipsum etc..<a>asdf</a>..</p>";
            var input = wrapperWithImage + plainHtml + wrapperWithItem;
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedItemName, new DummyItem()},
                {insertedImageName, new DummyImageItem()}
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(factory => factory.ResolveByDefaultToMessage("this should not appear for a loosely resolved item"))
                .AndResolver(factory => factory.ResolveByDefaultToType())
                .AndResolver(factory => factory.ResolveToMessage<DummyImageItem>("this should not appear a strongly resolved item"))
                .AndResolver(factory => factory.ResolveToType<DummyImageItem>())
                .Build();

            var result = inlineContentItemsProcessor.Process(input, processedContentItems);

            Assert.Equal($"<div>{typeof(DummyImageItem)}</div>" + plainHtml + $"<div>{typeof(DummyItem)}</div>", result);
        }

        /// <seealso href="https://github.com/Kentico/delivery-sdk-net/issues/153"/>
        [Fact]
        public void ContentItemWithDefaultResolverReturnsContentItemDirectlyInResolvedContentItemDataItemProperty()
        {
            const string insertedContentName = "dummyCodename1";
            var input = GetContentItemObjectElement(insertedContentName);
            var processedContentItems = new Dictionary<string, object>
            {
                {insertedContentName, new DummyItem()}
            };
            var inlineContentItemsProcessor = InlineContentItemsProcessorFactory
                .WithResolver(factory => factory.ResolveByDefaultToType())
                .Build();

            var result = inlineContentItemsProcessor.Process(input, processedContentItems);

            Assert.Equal(typeof(DummyItem).FullName, result);
        }

        private static IInlineContentItemsResolver<DummyImageItem> ResolveDummyImageToImg(InlineContentItemsResolverFactory factory)
            => factory.ResolveTo<DummyImageItem>(item => $"<img src=\"{item.Source}\" />");

        private static IInlineContentItemsResolver<DummyItem> ResolveDummyItemToSpan(InlineContentItemsResolverFactory factory)
            => factory.ResolveTo<DummyItem>(item => $"<span>{item.Value}</span>");

        private static string GetContentItemObjectElement(string insertedContentName)
            => $"<object type=\"{ContentItemType}\" data-type=\"{ContentItemDataType}\" data-codename=\"{insertedContentName}\"></object/>";

        private static string WrapElementWithDivs(string insertedObject)
            => "<div>" + insertedObject + "</div>";

        private class DummyItem
        {
            public string Value { get; set; }
        }

        private class DummyImageItem
        {
            public string Source { get; set; }
        }
    }
}