using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ContentItems.Processing;

/// <summary>
/// Tests for <see cref="ContentDependencyExtractor"/> and <see cref="NullContentDependencyExtractor"/>.
/// Verifies correct extraction of cache dependencies from content item elements.
/// </summary>
public class ContentDependencyExtractorTests
{
    private readonly ContentDependencyExtractor _extractor = new();

    #region Rich Text Element Extraction Tests

    [Fact]
    public void ExtractFromRichTextElement_WithImages_TracksAssetDependencies()
    {
        // Arrange
        var imageId1 = Guid.NewGuid();
        var imageId2 = Guid.NewGuid();

        var element = new MockRichTextElement();
        element.Images.Add(imageId1, new MockInlineImage());
        element.Images.Add(imageId2, new MockInlineImage());

        var context = new DependencyTrackingContext();

        // Act
        _extractor.ExtractFromRichTextElement(element, context);

        // Assert
        var dependencies = context.Dependencies.ToList();
        Assert.Contains($"asset_{imageId1}", dependencies);
        Assert.Contains($"asset_{imageId2}", dependencies);
    }

    [Fact]
    public void ExtractFromRichTextElement_WithLinks_TracksItemDependencies()
    {
        // Arrange
        var linkId1 = Guid.NewGuid();
        var linkId2 = Guid.NewGuid();

        var element = new MockRichTextElement();
        element.Links.Add(linkId1, new MockContentLink { Codename = "article_1" });
        element.Links.Add(linkId2, new MockContentLink { Codename = "article_2" });

        var context = new DependencyTrackingContext();

        // Act
        _extractor.ExtractFromRichTextElement(element, context);

        // Assert
        var dependencies = context.Dependencies.ToList();
        Assert.Contains("item_article_1", dependencies);
        Assert.Contains("item_article_2", dependencies);
    }

    [Fact]
    public void ExtractFromRichTextElement_WithModularContent_TracksItemDependencies()
    {
        // Arrange
        var element = new MockRichTextElement();
        element.ModularContent.AddRange(["hero_section", "testimonial", "cta_button"]);

        var context = new DependencyTrackingContext();

        // Act
        _extractor.ExtractFromRichTextElement(element, context);

        // Assert
        var dependencies = context.Dependencies.ToList();
        Assert.Contains("item_hero_section", dependencies);
        Assert.Contains("item_testimonial", dependencies);
        Assert.Contains("item_cta_button", dependencies);
    }

    [Fact]
    public void ExtractFromRichTextElement_WithAllDependencyTypes_TracksAll()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var linkId = Guid.NewGuid();

        var element = new MockRichTextElement();
        element.Images.Add(imageId, new MockInlineImage());
        element.Links.Add(linkId, new MockContentLink { Codename = "linked_article" });
        element.ModularContent.Add("inline_component");

        var context = new DependencyTrackingContext();

        // Act
        _extractor.ExtractFromRichTextElement(element, context);

        // Assert
        var dependencies = context.Dependencies.ToList();
        Assert.Contains($"asset_{imageId}", dependencies);
        Assert.Contains("item_linked_article", dependencies);
        Assert.Contains("item_inline_component", dependencies);
        Assert.Equal(3, dependencies.Count);
    }

    [Fact]
    public void ExtractFromRichTextElement_WithNullContext_DoesNotThrow()
    {
        // Arrange
        var element = new MockRichTextElement();
        element.Images.Add(Guid.NewGuid(), new MockInlineImage());

        // Act & Assert - should not throw
        _extractor.ExtractFromRichTextElement(element, null);
    }

    [Fact]
    public void ExtractFromRichTextElement_WithNullImages_DoesNotThrow()
    {
        // Arrange
        var element = new MockRichTextElement
        {
            Images = null!  // Test null handling
        };
        element.ModularContent.Add("item1");

        var context = new DependencyTrackingContext();

        // Act & Assert - should not throw
        _extractor.ExtractFromRichTextElement(element, context);

        var dependencies = context.Dependencies.ToList();
        Assert.Contains("item_item1", dependencies);
        Assert.Single(dependencies);
    }

    [Fact]
    public void ExtractFromRichTextElement_WithNullLinks_DoesNotThrow()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var element = new MockRichTextElement();
        element.Images.Add(imageId, new MockInlineImage());
        element.Links = null!;  // Test null handling
        element.ModularContent.Add("item1");

        var context = new DependencyTrackingContext();

        // Act & Assert - should not throw
        _extractor.ExtractFromRichTextElement(element, context);

        var dependencies = context.Dependencies.ToList();
        Assert.Contains($"asset_{imageId}", dependencies);
        Assert.Contains("item_item1", dependencies);
        Assert.Equal(2, dependencies.Count);
    }

    [Fact]
    public void ExtractFromRichTextElement_WithNullModularContent_DoesNotThrow()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var linkId = Guid.NewGuid();

        var element = new MockRichTextElement();
        element.Images.Add(imageId, new MockInlineImage());
        element.Links.Add(linkId, new MockContentLink { Codename = "article" });
        element.ModularContent = null!;  // Test null handling

        var context = new DependencyTrackingContext();

        // Act & Assert - should not throw
        _extractor.ExtractFromRichTextElement(element, context);

        var dependencies = context.Dependencies.ToList();
        Assert.Contains($"asset_{imageId}", dependencies);
        Assert.Contains("item_article", dependencies);
        Assert.Equal(2, dependencies.Count);
    }

    [Fact]
    public void ExtractFromRichTextElement_WithEmptyCollections_TracksNothing()
    {
        // Arrange
        var element = new MockRichTextElement();  // All collections are empty by default

        var context = new DependencyTrackingContext();

        // Act
        _extractor.ExtractFromRichTextElement(element, context);

        // Assert
        Assert.Empty(context.Dependencies);
    }

    #endregion

    #region Taxonomy Element Extraction Tests

    [Fact]
    public void ExtractFromTaxonomyElement_WithValidTaxonomyGroup_TracksDependency()
    {
        // Arrange
        var json = """
        {
            "taxonomy_group": "categories",
            "value": []
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;
        var context = new DependencyTrackingContext();

        // Act
        _extractor.ExtractFromTaxonomyElement(element, context);

        // Assert
        var dependencies = context.Dependencies.ToList();
        Assert.Contains("taxonomy_categories", dependencies);
        Assert.Single(dependencies);
    }

    [Fact]
    public void ExtractFromTaxonomyElement_WithNullContext_DoesNotThrow()
    {
        // Arrange
        var json = """
        {
            "taxonomy_group": "tags",
            "value": []
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act & Assert - should not throw
        _extractor.ExtractFromTaxonomyElement(element, null);
    }

    [Fact]
    public void ExtractFromTaxonomyElement_WithoutTaxonomyGroup_TracksNothing()
    {
        // Arrange
        var json = """
        {
            "value": []
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;
        var context = new DependencyTrackingContext();

        // Act
        _extractor.ExtractFromTaxonomyElement(element, context);

        // Assert
        Assert.Empty(context.Dependencies);
    }

    [Fact]
    public void ExtractFromTaxonomyElement_WithNullTaxonomyGroup_TracksNothing()
    {
        // Arrange
        var json = """
        {
            "taxonomy_group": null,
            "value": []
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;
        var context = new DependencyTrackingContext();

        // Act
        _extractor.ExtractFromTaxonomyElement(element, context);

        // Assert
        Assert.Empty(context.Dependencies);
    }

    [Fact]
    public void ExtractFromTaxonomyElement_WithEmptyObject_TracksNothing()
    {
        // Arrange
        var json = "{}";
        var element = JsonDocument.Parse(json).RootElement;
        var context = new DependencyTrackingContext();

        // Act
        _extractor.ExtractFromTaxonomyElement(element, context);

        // Assert
        Assert.Empty(context.Dependencies);
    }

    #endregion

    #region NullContentDependencyExtractor Tests

    [Fact]
    public void NullExtractor_Instance_IsSingleton()
    {
        // Act
        var instance1 = NullContentDependencyExtractor.Instance;
        var instance2 = NullContentDependencyExtractor.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void NullExtractor_ExtractFromRichTextElement_DoesNothing()
    {
        // Arrange
        var extractor = NullContentDependencyExtractor.Instance;
        var imageId = Guid.NewGuid();

        var element = new MockRichTextElement();
        element.Images.Add(imageId, new MockInlineImage());
        element.Links.Add(Guid.NewGuid(), new MockContentLink { Codename = "article" });
        element.ModularContent.Add("component");

        var context = new DependencyTrackingContext();

        // Act
        extractor.ExtractFromRichTextElement(element, context);

        // Assert
        Assert.Empty(context.Dependencies);
    }

    [Fact]
    public void NullExtractor_ExtractFromTaxonomyElement_DoesNothing()
    {
        // Arrange
        var extractor = NullContentDependencyExtractor.Instance;
        var json = """
        {
            "taxonomy_group": "categories",
            "value": []
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;
        var context = new DependencyTrackingContext();

        // Act
        extractor.ExtractFromTaxonomyElement(element, context);

        // Assert
        Assert.Empty(context.Dependencies);
    }

    [Fact]
    public void NullExtractor_ExtractFromRichTextElement_WithNullContext_DoesNotThrow()
    {
        // Arrange
        var extractor = NullContentDependencyExtractor.Instance;
        var element = new MockRichTextElement();
        element.Images.Add(Guid.NewGuid(), new MockInlineImage());

        // Act & Assert - should not throw
        extractor.ExtractFromRichTextElement(element, null);
    }

    [Fact]
    public void NullExtractor_ExtractFromTaxonomyElement_WithNullContext_DoesNotThrow()
    {
        // Arrange
        var extractor = NullContentDependencyExtractor.Instance;
        var json = """
        {
            "taxonomy_group": "tags",
            "value": []
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act & Assert - should not throw
        extractor.ExtractFromTaxonomyElement(element, null);
    }

    #endregion

    #region Mock Classes

    private class MockRichTextElement : IRichTextElementValue
    {
        public string Value { get; set; } = "<p>Mock content</p>";
        public string Codename { get; set; } = "mock_element";
        public string Name { get; set; } = "Mock Element";
        public string Type { get; set; } = "rich_text";
        public IDictionary<Guid, IInlineImage> Images { get; set; } = new Dictionary<Guid, IInlineImage>();
        public IDictionary<Guid, IContentLink> Links { get; set; } = new Dictionary<Guid, IContentLink>();
        public List<string> ModularContent { get; set; } = [];
    }

    private class MockInlineImage : IInlineImage
    {
        public string Description { get; set; } = "Mock image";
        public int Height { get; set; } = 100;
        public Guid ImageId { get; set; } = Guid.NewGuid();
        public string Url { get; set; } = "https://example.com/image.jpg";
        public int Width { get; set; } = 100;
    }

    private class MockContentLink : IContentLink
    {
        public required string Codename { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ContentTypeCodename { get; set; } = "article";
        public string UrlSlug { get; set; } = string.Empty;
    }

    #endregion
}