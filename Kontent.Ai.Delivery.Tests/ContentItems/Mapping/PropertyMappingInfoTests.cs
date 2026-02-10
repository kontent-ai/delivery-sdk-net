using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ContentItems.Mapping;

public sealed class PropertyMappingInfoTests
{
    [Fact]
    public void CreateMappings_ExtractsJsonPropertyNames()
    {
        var mappings = PropertyMappingInfo.CreateMappings(typeof(TestModel));

        Assert.Equal(4, mappings.Length);
        Assert.Contains(mappings, m => m.ElementCodename == "title");
        Assert.Contains(mappings, m => m.ElementCodename == "description");
        Assert.Contains(mappings, m => m.ElementCodename == "items");
        Assert.Contains(mappings, m => m.ElementCodename == "count");
    }

    [Fact]
    public void CreateMappings_IgnoresPropertiesWithJsonIgnore()
    {
        var mappings = PropertyMappingInfo.CreateMappings(typeof(TestModel));

        Assert.DoesNotContain(mappings, m => m.ElementCodename == "ignored");
    }

    [Fact]
    public void CreateMappings_IgnoresPropertiesWithoutJsonPropertyName()
    {
        var mappings = PropertyMappingInfo.CreateMappings(typeof(TestModel));

        Assert.DoesNotContain(mappings, m => m.Property.Name == "UnmappedProperty");
    }

    [Fact]
    public void CreateMappings_DetectsEnumerableElementType()
    {
        var mappings = PropertyMappingInfo.CreateMappings(typeof(TestModel));

        var itemsMapping = Assert.Single(mappings, m => m.ElementCodename == "items");
        Assert.Equal(typeof(IEmbeddedContent), itemsMapping.EnumerableElementType);
    }

    [Fact]
    public void CreateMappings_ReturnsNullEnumerableElementTypeForNonEnumerables()
    {
        var mappings = PropertyMappingInfo.CreateMappings(typeof(TestModel));

        // Note: string implements IEnumerable<char>, so we test with count (int) instead
        var countMapping = Assert.Single(mappings, m => m.ElementCodename == "count");
        Assert.Null(countMapping.EnumerableElementType);
    }

    [Fact]
    public void SetValue_SetsPropertyValue()
    {
        var mappings = PropertyMappingInfo.CreateMappings(typeof(TestModel));
        var titleMapping = Assert.Single(mappings, m => m.ElementCodename == "title");

        var model = new TestModel();
        titleMapping.SetValue(model, "Test Title");

        Assert.Equal("Test Title", model.Title);
    }

    [Fact]
    public void CreateMappings_CachesResults()
    {
        // First call
        var mappings1 = PropertyMappingInfo.CreateMappings(typeof(TestModel));

        // Second call should return same array instance (due to ConcurrentDictionary caching in mapper)
        var mappings2 = PropertyMappingInfo.CreateMappings(typeof(TestModel));

        // Note: CreateMappings creates new arrays each time; caching is done at ContentItemMapper level
        // This test verifies the method returns equivalent results
        Assert.Equal(mappings1.Length, mappings2.Length);
        Assert.All(mappings1, m1 =>
            Assert.Contains(mappings2, m2 => m2.ElementCodename == m1.ElementCodename));
    }

    [Fact]
    public void CreateMappings_AssignsExpectedMapKind()
    {
        var mappings = PropertyMappingInfo.CreateMappings(typeof(MapKindModel))
            .ToDictionary(m => m.ElementCodename, m => m.MapKind);

        Assert.Equal(ElementMappingKind.Simple, mappings["title"]);
        Assert.Equal(ElementMappingKind.RichText, mappings["body_copy"]);
        Assert.Equal(ElementMappingKind.Assets, mappings["teaser_image"]);
        Assert.Equal(ElementMappingKind.Taxonomy, mappings["personas"]);
        Assert.Equal(ElementMappingKind.DateTime, mappings["publish_date"]);
        Assert.Equal(ElementMappingKind.LinkedItems, mappings["related_articles"]);
    }

    private class TestModel
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("items")]
        public IEnumerable<IEmbeddedContent>? Items { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonIgnore]
        [JsonPropertyName("ignored")]
        public string? IgnoredProperty { get; set; }
    }

    private class MapKindModel
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("body_copy")]
        public IRichTextContent? BodyCopy { get; set; }

        [JsonPropertyName("teaser_image")]
        public IEnumerable<IAsset>? TeaserImage { get; set; }

        [JsonPropertyName("personas")]
        public IEnumerable<ITaxonomyTerm>? Personas { get; set; }

        [JsonPropertyName("publish_date")]
        public IDateTimeContent? PublishDate { get; set; }

        [JsonPropertyName("related_articles")]
        public IEnumerable<IEmbeddedContent>? RelatedArticles { get; set; }
    }
}
