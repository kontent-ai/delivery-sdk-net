using System.Text.Json;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentTypes.Element;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ContentTypes;

public class ContentElementDeserializationTests
{
    private readonly JsonSerializerOptions _options = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();

    #region Polymorphic Deserialization Tests

    [Fact]
    public void Deserialize_TaxonomyElement_ReturnsTaxonomyElementType()
    {
        // Arrange
        var json = """
        {
            "type": "taxonomy",
            "name": "Product status",
            "codename": "product_status",
            "taxonomy_group": "product_status"
        }
        """;

        // Act
        var element = JsonSerializer.Deserialize<ContentElement>(json, _options);

        // Assert
        Assert.NotNull(element);
        Assert.IsType<TaxonomyElement>(element);
        var taxonomyElement = (TaxonomyElement)element;
        Assert.Equal("taxonomy", taxonomyElement.Type);
        Assert.Equal("Product status", taxonomyElement.Name);
        Assert.Equal("product_status", taxonomyElement.Codename);
        Assert.Equal("product_status", taxonomyElement.TaxonomyGroup);
    }

    [Fact]
    public void Deserialize_MultipleChoiceElement_ReturnsMultipleChoiceElementType()
    {
        // Arrange
        var json = """
        {
            "type": "multiple_choice",
            "name": "Video host",
            "codename": "video_host",
            "options": [
                { "name": "YouTube", "codename": "youtube" },
                { "name": "Vimeo", "codename": "vimeo" }
            ]
        }
        """;

        // Act
        var element = JsonSerializer.Deserialize<ContentElement>(json, _options);

        // Assert
        Assert.NotNull(element);
        Assert.IsType<MultipleChoiceElement>(element);
        var mcElement = (MultipleChoiceElement)element;
        Assert.Equal("multiple_choice", mcElement.Type);
        Assert.Equal("Video host", mcElement.Name);
        Assert.Equal("video_host", mcElement.Codename);
        Assert.Equal(2, mcElement.Options.Count);
        Assert.Equal("YouTube", mcElement.Options[0].Name);
        Assert.Equal("youtube", mcElement.Options[0].Codename);
    }

    [Fact]
    public void Deserialize_TextElement_ReturnsBaseContentElementType()
    {
        // Arrange
        var json = """
        {
            "type": "text",
            "name": "Title",
            "codename": "title"
        }
        """;

        // Act
        var element = JsonSerializer.Deserialize<ContentElement>(json, _options);

        // Assert
        Assert.NotNull(element);
        Assert.IsType<ContentElement>(element);
        Assert.Equal("text", element.Type);
        Assert.Equal("Title", element.Name);
        Assert.Equal("title", element.Codename);
    }

    [Fact]
    public void Deserialize_UnknownElementType_ReturnsBaseContentElementType()
    {
        // Arrange - future element types should fall back to base type
        var json = """
        {
            "type": "custom_element",
            "name": "My Custom",
            "codename": "my_custom"
        }
        """;

        // Act
        var element = JsonSerializer.Deserialize<ContentElement>(json, _options);

        // Assert
        Assert.NotNull(element);
        Assert.IsType<ContentElement>(element);
        Assert.Equal("custom_element", element.Type);
    }

    #endregion

    #region Dictionary with Codename Hydration Tests

    [Fact]
    public void Deserialize_ElementsDictionary_HydratesCodenameFromKey()
    {
        // Arrange - simulates ContentType.Elements structure
        var json = """
        {
            "product_name": {
                "type": "text",
                "name": "Product name"
            },
            "price": {
                "type": "number",
                "name": "Price"
            }
        }
        """;

        // Act
        var elements = JsonSerializer.Deserialize<IReadOnlyDictionary<string, ContentElement>>(json, _options);

        // Assert
        Assert.NotNull(elements);
        Assert.Equal(2, elements.Count);

        Assert.True(elements.TryGetValue("product_name", out var productName));
        Assert.Equal("product_name", productName.Codename);
        Assert.Equal("text", productName.Type);
        Assert.Equal("Product name", productName.Name);

        Assert.True(elements.TryGetValue("price", out var price));
        Assert.Equal("price", price.Codename);
        Assert.Equal("number", price.Type);
    }

    [Fact]
    public void Deserialize_ElementsDictionary_WithTaxonomyElement_HydratesCodenameAndPreservesType()
    {
        // Arrange
        var json = """
        {
            "product_status": {
                "type": "taxonomy",
                "name": "Product status",
                "taxonomy_group": "product_status"
            }
        }
        """;

        // Act
        var elements = JsonSerializer.Deserialize<IReadOnlyDictionary<string, ContentElement>>(json, _options);

        // Assert
        Assert.NotNull(elements);
        Assert.Single(elements);

        Assert.True(elements.TryGetValue("product_status", out var element));
        Assert.IsType<TaxonomyElement>(element);
        Assert.Equal("product_status", element.Codename); // Hydrated from key

        var taxonomyElement = (TaxonomyElement)element;
        Assert.Equal("product_status", taxonomyElement.TaxonomyGroup);
    }

    [Fact]
    public void Deserialize_ElementsDictionary_WithMultipleChoiceElement_HydratesCodenameAndPreservesOptions()
    {
        // Arrange
        var json = """
        {
            "video_host": {
                "type": "multiple_choice",
                "name": "Video host",
                "options": [
                    { "name": "YouTube", "codename": "youtube" }
                ]
            }
        }
        """;

        // Act
        var elements = JsonSerializer.Deserialize<IReadOnlyDictionary<string, ContentElement>>(json, _options);

        // Assert
        Assert.NotNull(elements);
        Assert.True(elements.TryGetValue("video_host", out var element));
        Assert.IsType<MultipleChoiceElement>(element);
        Assert.Equal("video_host", element.Codename); // Hydrated from key

        var mcElement = (MultipleChoiceElement)element;
        Assert.Single(mcElement.Options);
        Assert.Equal("YouTube", mcElement.Options[0].Name);
    }

    [Fact]
    public void Deserialize_EmptyElementsDictionary_ReturnsEmptyDictionary()
    {
        // Arrange
        var json = "{}";

        // Act
        var elements = JsonSerializer.Deserialize<IReadOnlyDictionary<string, ContentElement>>(json, _options);

        // Assert
        Assert.NotNull(elements);
        Assert.Empty(elements);
    }

    #endregion

    #region Direct Element Query Tests (codename in JSON)

    [Fact]
    public void Deserialize_DirectElementQuery_CodenameFromJson()
    {
        // Arrange - simulates GetContentElement API response where codename IS in JSON
        var json = """
        {
            "type": "taxonomy",
            "name": "Personas",
            "codename": "personas",
            "taxonomy_group": "personas"
        }
        """;

        // Act
        var element = JsonSerializer.Deserialize<ContentElement>(json, _options);

        // Assert
        Assert.NotNull(element);
        Assert.IsType<TaxonomyElement>(element);
        Assert.Equal("personas", element.Codename); // From JSON, not dictionary key
    }

    #endregion
}
