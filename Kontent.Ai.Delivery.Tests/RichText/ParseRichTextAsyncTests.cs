using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.RichText;

/// <summary>
/// Tests for the <see cref="RichTextExtensions.ParseRichTextAsync"/> extension method,
/// which enables rich text processing for dynamic mode content items.
/// </summary>
public class ParseRichTextAsyncTests
{
    #region Basic Parsing Tests

    [Fact]
    public async Task ParseRichTextAsync_SimpleHtml_ReturnsRichTextContent()
    {
        // Arrange
        var json = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<p>Hello world</p>",
                "images": {},
                "links": {},
                "modular_content": []
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = await element.ParseRichTextAsync();

        // Assert
        Assert.NotNull(result);
        var html = await result.ToHtmlAsync();
        Assert.Equal("<p>Hello world</p>", html);
    }

    [Fact]
    public async Task ParseRichTextAsync_WithNestedTags_PreservesStructure()
    {
        // Arrange
        var json = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<p>This is <strong>bold</strong> and <em>italic</em> text.</p>",
                "images": {},
                "links": {},
                "modular_content": []
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = await element.ParseRichTextAsync();

        // Assert
        Assert.NotNull(result);
        var html = await result.ToHtmlAsync();
        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<em>italic</em>", html);
    }

    [Fact]
    public async Task ParseRichTextAsync_WithListElements_ParsesCorrectly()
    {
        // Arrange
        var json = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<ul><li>Item 1</li><li>Item 2</li></ul>",
                "images": {},
                "links": {},
                "modular_content": []
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = await element.ParseRichTextAsync();

        // Assert
        Assert.NotNull(result);
        var html = await result.ToHtmlAsync();
        Assert.Contains("<ul>", html);
        Assert.Contains("<li>Item 1</li>", html);
        Assert.Contains("<li>Item 2</li>", html);
    }

    #endregion

    #region Image Parsing Tests

    [Fact]
    public async Task ParseRichTextAsync_WithInlineImages_ParsesImageMetadata()
    {
        // Arrange
        var json = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<figure data-asset-id=\"bf509e0d-d9ed-4925-968e-29a1f6a561c4\" data-image-id=\"bf509e0d-d9ed-4925-968e-29a1f6a561c4\"><img src=\"https://example.com/image.jpg\" data-asset-id=\"bf509e0d-d9ed-4925-968e-29a1f6a561c4\" data-image-id=\"bf509e0d-d9ed-4925-968e-29a1f6a561c4\" alt=\"Test image\"></figure>",
                "images": {
                    "bf509e0d-d9ed-4925-968e-29a1f6a561c4": {
                        "image_id": "bf509e0d-d9ed-4925-968e-29a1f6a561c4",
                        "description": "A test image",
                        "url": "https://example.com/image.jpg",
                        "width": 800,
                        "height": 600
                    }
                },
                "links": {},
                "modular_content": []
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = await element.ParseRichTextAsync();

        // Assert
        Assert.NotNull(result);
        var images = result.GetInlineImages().ToList();
        Assert.Single(images);

        var image = images[0];
        Assert.Equal("https://example.com/image.jpg", image.Url);
        Assert.Equal("A test image", image.Description);
        Assert.Equal(800, image.Width);
        Assert.Equal(600, image.Height);
    }

    [Fact]
    public async Task ParseRichTextAsync_WithMultipleImages_ParsesAllImages()
    {
        // Arrange
        var json = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<p>First image:</p><figure><img src=\"https://example.com/image1.jpg\" data-asset-id=\"11111111-1111-1111-1111-111111111111\"></figure><p>Second image:</p><figure><img src=\"https://example.com/image2.jpg\" data-asset-id=\"22222222-2222-2222-2222-222222222222\"></figure>",
                "images": {
                    "11111111-1111-1111-1111-111111111111": {
                        "image_id": "11111111-1111-1111-1111-111111111111",
                        "description": "Image 1",
                        "url": "https://example.com/image1.jpg",
                        "width": 100,
                        "height": 100
                    },
                    "22222222-2222-2222-2222-222222222222": {
                        "image_id": "22222222-2222-2222-2222-222222222222",
                        "description": "Image 2",
                        "url": "https://example.com/image2.jpg",
                        "width": 200,
                        "height": 200
                    }
                },
                "links": {},
                "modular_content": []
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = await element.ParseRichTextAsync();

        // Assert
        Assert.NotNull(result);
        var images = result.GetInlineImages().ToList();
        Assert.Equal(2, images.Count);
    }

    #endregion

    #region Content Link Parsing Tests

    [Fact]
    public async Task ParseRichTextAsync_WithContentLinks_ParsesLinkMetadata()
    {
        // Arrange
        var json = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<p>Check out this <a data-item-id=\"80c7074b-3da1-4e1d-882b-c5716ebb4d25\" href=\"\">coffee product</a>.</p>",
                "images": {},
                "links": {
                    "80c7074b-3da1-4e1d-882b-c5716ebb4d25": {
                        "codename": "kenya_gakuyuni_aa",
                        "type": "coffee",
                        "url_slug": "kenya-gakuyuni-aa"
                    }
                },
                "modular_content": []
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = await element.ParseRichTextAsync();

        // Assert
        Assert.NotNull(result);
        var links = result.GetContentItemLinks().ToList();
        Assert.Single(links);

        var link = links[0];
        Assert.NotNull(link.Metadata);
        Assert.Equal("kenya_gakuyuni_aa", link.Metadata.Codename);
        Assert.Equal("coffee", link.Metadata.ContentTypeCodename);
        Assert.Equal("kenya-gakuyuni-aa", link.Metadata.UrlSlug);
    }

    #endregion

    #region Embedded Content Tests

    [Fact]
    public async Task ParseRichTextAsync_WithModularContent_ResolvesEmbeddedItems()
    {
        // Arrange
        var richTextJson = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<p>Here is an embedded tweet:</p><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"n508d3c3b_f884_0286_6369_c92bd5ca1874\"></object>",
                "images": {},
                "links": {},
                "modular_content": ["n508d3c3b_f884_0286_6369_c92bd5ca1874"]
            }
            """;
        var richTextElement = JsonDocument.Parse(richTextJson).RootElement;

        // Create modular content with the embedded item
        var modularContentJson = """
            {
                "n508d3c3b_f884_0286_6369_c92bd5ca1874": {
                    "system": {
                        "id": "508d3c3b-f884-0286-6369-c92bd5ca1874",
                        "name": "Sample Tweet",
                        "codename": "n508d3c3b_f884_0286_6369_c92bd5ca1874",
                        "language": "en-US",
                        "type": "tweet",
                        "collection": "default",
                        "sitemap_locations": [],
                        "last_modified": "2021-01-01T00:00:00Z",
                        "workflow": "default",
                        "workflow_step": "published"
                    },
                    "elements": {
                        "tweet_link": {
                            "type": "text",
                            "name": "Tweet Link",
                            "value": "https://twitter.com/user/status/123"
                        }
                    }
                }
            }
            """;
        var modularContentDoc = JsonDocument.Parse(modularContentJson);
        var modularContent = modularContentDoc.RootElement
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());

        // Act
        var result = await richTextElement.ParseRichTextAsync(modularContent);

        // Assert
        Assert.NotNull(result);
        var embedded = result.GetEmbeddedContent().ToList();
        Assert.Single(embedded);

        var item = embedded[0];
        Assert.Equal("tweet", item.System.Type);
        Assert.Equal("n508d3c3b_f884_0286_6369_c92bd5ca1874", item.System.Codename);
    }

    [Fact]
    public async Task ParseRichTextAsync_WithMultipleEmbeddedItems_ResolvesAll()
    {
        // Arrange
        var richTextJson = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<p>Tweet:</p><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"tweet1\"></object><p>Video:</p><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"video1\"></object>",
                "images": {},
                "links": {},
                "modular_content": ["tweet1", "video1"]
            }
            """;
        var richTextElement = JsonDocument.Parse(richTextJson).RootElement;

        var modularContentJson = """
            {
                "tweet1": {
                    "system": {
                        "id": "11111111-1111-1111-1111-111111111111",
                        "name": "Tweet 1",
                        "codename": "tweet1",
                        "language": "en-US",
                        "type": "tweet",
                        "collection": "default",
                        "sitemap_locations": [],
                        "last_modified": "2021-01-01T00:00:00Z",
                        "workflow": "default",
                        "workflow_step": "published"
                    },
                    "elements": {}
                },
                "video1": {
                    "system": {
                        "id": "22222222-2222-2222-2222-222222222222",
                        "name": "Video 1",
                        "codename": "video1",
                        "language": "en-US",
                        "type": "hosted_video",
                        "collection": "default",
                        "sitemap_locations": [],
                        "last_modified": "2021-01-01T00:00:00Z",
                        "workflow": "default",
                        "workflow_step": "published"
                    },
                    "elements": {}
                }
            }
            """;
        var modularContentDoc = JsonDocument.Parse(modularContentJson);
        var modularContent = modularContentDoc.RootElement
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());

        // Act
        var result = await richTextElement.ParseRichTextAsync(modularContent);

        // Assert
        Assert.NotNull(result);
        var embedded = result.GetEmbeddedContent().ToList();
        Assert.Equal(2, embedded.Count);

        Assert.Contains(embedded, e => e.System.Type == "tweet");
        Assert.Contains(embedded, e => e.System.Type == "hosted_video");
    }

    [Fact]
    public async Task ParseRichTextAsync_EmbeddedContentHasDynamicElements()
    {
        // Arrange
        var richTextJson = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"embedded_item\"></object>",
                "images": {},
                "links": {},
                "modular_content": ["embedded_item"]
            }
            """;
        var richTextElement = JsonDocument.Parse(richTextJson).RootElement;

        var modularContentJson = """
            {
                "embedded_item": {
                    "system": {
                        "id": "11111111-1111-1111-1111-111111111111",
                        "name": "Embedded Item",
                        "codename": "embedded_item",
                        "language": "en-US",
                        "type": "custom_type",
                        "collection": "default",
                        "sitemap_locations": [],
                        "last_modified": "2021-01-01T00:00:00Z",
                        "workflow": "default",
                        "workflow_step": "published"
                    },
                    "elements": {
                        "title": {
                            "type": "text",
                            "name": "Title",
                            "value": "My Title"
                        },
                        "description": {
                            "type": "text",
                            "name": "Description",
                            "value": "My Description"
                        }
                    }
                }
            }
            """;
        var modularContentDoc = JsonDocument.Parse(modularContentJson);
        var modularContent = modularContentDoc.RootElement
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());

        // Act
        var result = await richTextElement.ParseRichTextAsync(modularContent);

        // Assert
        Assert.NotNull(result);
        var embedded = result.GetEmbeddedContent().ToList();
        Assert.Single(embedded);

        var item = embedded[0];

        // The embedded item should have IDynamicElements
        var dynamicEmbedded = result.GetEmbeddedContent<IDynamicElements>().ToList();
        Assert.Single(dynamicEmbedded);

        // Access elements as JsonElement
        var elements = dynamicEmbedded[0].Elements;
        Assert.True(elements.TryGetValue("title", out var titleElement));
        Assert.Equal("text", titleElement.GetProperty("type").GetString());
        Assert.Equal("My Title", titleElement.GetProperty("value").GetString());
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task ParseRichTextAsync_InvalidJsonKind_ReturnsNull()
    {
        // Arrange - a string, not an object
        var json = "\"not an object\"";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = await element.ParseRichTextAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ParseRichTextAsync_NonRichTextType_ReturnsNull()
    {
        // Arrange - a text element, not rich_text
        var json = """
            {
                "type": "text",
                "name": "Title",
                "value": "Hello world"
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = await element.ParseRichTextAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ParseRichTextAsync_EmptyValue_ReturnsEmptyContent()
    {
        // Arrange
        var json = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "",
                "images": {},
                "links": {},
                "modular_content": []
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = await element.ParseRichTextAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseRichTextAsync_MissingModularContentItem_SkipsGracefully()
    {
        // Arrange - modular_content references an item that's not in the dictionary
        var richTextJson = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<p>Before</p><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"missing_item\"></object><p>After</p>",
                "images": {},
                "links": {},
                "modular_content": ["missing_item"]
            }
            """;
        var richTextElement = JsonDocument.Parse(richTextJson).RootElement;

        // Empty modular content - item is not available
        var modularContent = new Dictionary<string, JsonElement>();

        // Act
        var result = await richTextElement.ParseRichTextAsync(modularContent);

        // Assert
        Assert.NotNull(result);
        var embedded = result.GetEmbeddedContent().ToList();
        Assert.Empty(embedded); // Missing item is skipped

        // But HTML content is still parsed
        var html = await result.ToHtmlAsync();
        Assert.Contains("Before", html);
        Assert.Contains("After", html);
    }

    [Fact]
    public async Task ParseRichTextAsync_NullModularContent_HandlesGracefully()
    {
        // Arrange
        var json = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<p>Hello world</p><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"some_item\"></object>",
                "images": {},
                "links": {},
                "modular_content": ["some_item"]
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;

        // Act - pass null for modular content
        var result = await element.ParseRichTextAsync(modularContent: null);

        // Assert
        Assert.NotNull(result);
        var embedded = result.GetEmbeddedContent().ToList();
        Assert.Empty(embedded); // Can't resolve without modular content
    }

    #endregion

    #region ToHtmlAsync Integration Tests

    [Fact]
    public async Task ParseRichTextAsync_ToHtmlAsync_WorksWithResolver()
    {
        // Arrange
        var richTextJson = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<p>Tweet:</p><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"tweet1\"></object>",
                "images": {},
                "links": {},
                "modular_content": ["tweet1"]
            }
            """;
        var richTextElement = JsonDocument.Parse(richTextJson).RootElement;

        var modularContentJson = """
            {
                "tweet1": {
                    "system": {
                        "id": "11111111-1111-1111-1111-111111111111",
                        "name": "Sample Tweet",
                        "codename": "tweet1",
                        "language": "en-US",
                        "type": "tweet",
                        "collection": "default",
                        "sitemap_locations": [],
                        "last_modified": "2021-01-01T00:00:00Z",
                        "workflow": "default",
                        "workflow_step": "published"
                    },
                    "elements": {
                        "tweet_link": {
                            "type": "text",
                            "name": "Tweet Link",
                            "value": "https://twitter.com/example/status/123"
                        }
                    }
                }
            }
            """;
        var modularContentDoc = JsonDocument.Parse(modularContentJson);
        var modularContent = modularContentDoc.RootElement
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());

        // Act
        var richText = await richTextElement.ParseRichTextAsync(modularContent);

        // Create a resolver that handles embedded content
        var resolver = new Kontent.Ai.Delivery.ContentItems.RichText.Resolution.HtmlResolverBuilder()
            .WithContentResolver("tweet", content => "<div class=\"tweet-embed\">[Tweet]</div>")
            .Build();

        var html = await richText!.ToHtmlAsync(resolver);

        // Assert
        Assert.Contains("<p>Tweet:</p>", html);
        Assert.Contains("<div class=\"tweet-embed\">[Tweet]</div>", html);
    }

    /// <summary>
    /// End-to-end test demonstrating full dynamic mode workflow:
    /// 1. Parse rich text from JsonElement (simulating dynamic API response)
    /// 2. Resolve embedded content using modular_content dictionary
    /// 3. Use codename-based resolvers that access IDynamicElements
    /// 4. Extract actual element values from the untyped JsonElement dictionary
    ///
    /// This is the recommended pattern for using rich text resolution with dynamic mode.
    /// </summary>
    [Fact]
    public async Task ParseRichTextAsync_EndToEnd_DynamicModeWithResolversAccessingElementData()
    {
        // Arrange - Simulating a dynamic API response with rich text containing multiple embedded items
        var richTextJson = """
            {
                "type": "rich_text",
                "name": "Article Body",
                "value": "<p>Check out this tweet:</p><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"featured_tweet\"></object><p>And watch this video:</p><object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"intro_video\"></object>",
                "images": {},
                "links": {},
                "modular_content": ["featured_tweet", "intro_video"]
            }
            """;

        var modularContentJson = """
            {
                "featured_tweet": {
                    "system": {
                        "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                        "name": "Featured Tweet",
                        "codename": "featured_tweet",
                        "language": "en-US",
                        "type": "tweet",
                        "collection": "default",
                        "sitemap_locations": [],
                        "last_modified": "2024-01-15T10:30:00Z",
                        "workflow": "default",
                        "workflow_step": "published"
                    },
                    "elements": {
                        "tweet_link": {
                            "type": "text",
                            "name": "Tweet Link",
                            "value": "https://twitter.com/kikirai/status/123456789"
                        },
                        "theme": {
                            "type": "multiple_choice",
                            "name": "Theme",
                            "value": [{ "codename": "dark" }]
                        }
                    }
                },
                "intro_video": {
                    "system": {
                        "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                        "name": "Introduction Video",
                        "codename": "intro_video",
                        "language": "en-US",
                        "type": "hosted_video",
                        "collection": "default",
                        "sitemap_locations": [],
                        "last_modified": "2024-01-10T08:00:00Z",
                        "workflow": "default",
                        "workflow_step": "published"
                    },
                    "elements": {
                        "video_id": {
                            "type": "text",
                            "name": "Video ID",
                            "value": "dQw4w9WgXcQ"
                        },
                        "video_host": {
                            "type": "multiple_choice",
                            "name": "Video Host",
                            "value": [{ "codename": "youtube" }]
                        }
                    }
                }
            }
            """;

        using var richTextDoc = JsonDocument.Parse(richTextJson);
        var richTextElement = richTextDoc.RootElement.Clone();

        using var modularContentDoc = JsonDocument.Parse(modularContentJson);
        var modularContent = modularContentDoc.RootElement
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());

        // Act - Parse rich text with modular content
        var richText = await richTextElement.ParseRichTextAsync(modularContent);
        Assert.NotNull(richText);

        // Create resolvers that access actual element data from IDynamicElements
        // This demonstrates the pattern for dynamic mode: use codename-based resolvers
        // and cast to IEmbeddedContent<IDynamicElements> to access element values
        var resolver = new Kontent.Ai.Delivery.ContentItems.RichText.Resolution.HtmlResolverBuilder()
            .WithContentResolver("tweet", content =>
            {
                // Pattern for dynamic mode: cast to generic interface to access elements
                var dynamicContent = (IEmbeddedContent<IDynamicElements>)content;
                var elements = dynamicContent.Elements;

                // Access element values from the JsonElement dictionary
                var tweetLink = elements.TryGetValue("tweet_link", out var linkEl)
                    ? linkEl.GetProperty("value").GetString()
                    : "#";

                var theme = "light"; // default
                if (elements.TryGetValue("theme", out var themeEl) &&
                    themeEl.TryGetProperty("value", out var themeValues) &&
                    themeValues.GetArrayLength() > 0)
                {
                    theme = themeValues[0].GetProperty("codename").GetString() ?? "light";
                }

                // Return rendered HTML using the extracted data
                return $"""<blockquote class="twitter-tweet" data-theme="{theme}"><a href="{tweetLink}">View Tweet</a></blockquote>""";
            })
            .WithContentResolver("hosted_video", content =>
            {
                // Same pattern for video content type
                var dynamicContent = (IEmbeddedContent<IDynamicElements>)content;
                var elements = dynamicContent.Elements;

                var videoId = elements.TryGetValue("video_id", out var idEl)
                    ? idEl.GetProperty("value").GetString()
                    : "";

                var host = "youtube"; // default
                if (elements.TryGetValue("video_host", out var hostEl) &&
                    hostEl.TryGetProperty("value", out var hostValues) &&
                    hostValues.GetArrayLength() > 0)
                {
                    host = hostValues[0].GetProperty("codename").GetString() ?? "youtube";
                }

                return host switch
                {
                    "youtube" => $"""<iframe src="https://www.youtube.com/embed/{videoId}" allowfullscreen></iframe>""",
                    "vimeo" => $"""<iframe src="https://player.vimeo.com/video/{videoId}" allowfullscreen></iframe>""",
                    _ => $"""<a href="{videoId}">Watch Video</a>"""
                };
            })
            .Build();

        var html = await richText.ToHtmlAsync(resolver);

        // Assert - Verify the resolved HTML contains correct data from elements
        Assert.Contains("<p>Check out this tweet:</p>", html);
        Assert.Contains("<p>And watch this video:</p>", html);

        // Tweet resolver extracted the URL and theme from elements
        Assert.Contains("https://twitter.com/kikirai/status/123456789", html);
        Assert.Contains("""data-theme="dark""", html);

        // Video resolver extracted the video ID and rendered YouTube embed
        Assert.Contains("https://www.youtube.com/embed/dQw4w9WgXcQ", html);
        Assert.Contains("<iframe", html);
    }

    /// <summary>
    /// Demonstrates that System metadata is accessible on embedded content in dynamic mode,
    /// allowing resolvers to make decisions based on content type, codename, etc.
    /// </summary>
    [Fact]
    public async Task ParseRichTextAsync_DynamicMode_SystemMetadataAccessibleInResolvers()
    {
        // Arrange
        var richTextJson = """
            {
                "type": "rich_text",
                "name": "Body",
                "value": "<object type=\"application/kenticocloud\" data-type=\"item\" data-codename=\"my_component\"></object>",
                "images": {},
                "links": {},
                "modular_content": ["my_component"]
            }
            """;

        var modularContentJson = """
            {
                "my_component": {
                    "system": {
                        "id": "12345678-1234-1234-1234-123456789012",
                        "name": "My Custom Component",
                        "codename": "my_component",
                        "language": "en-US",
                        "type": "custom_widget",
                        "collection": "widgets",
                        "sitemap_locations": [],
                        "last_modified": "2024-06-01T12:00:00Z",
                        "workflow": "default",
                        "workflow_step": "published"
                    },
                    "elements": {}
                }
            }
            """;

        using var richTextDoc = JsonDocument.Parse(richTextJson);
        using var modularContentDoc = JsonDocument.Parse(modularContentJson);
        var modularContent = modularContentDoc.RootElement
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());

        // Act
        var richText = await richTextDoc.RootElement.Clone().ParseRichTextAsync(modularContent);

        // Capture system info in resolver for verification
        IContentItemSystemAttributes? capturedSystem = null;

        var resolver = new Kontent.Ai.Delivery.ContentItems.RichText.Resolution.HtmlResolverBuilder()
            .WithContentResolver("custom_widget", content =>
            {
                // System metadata is always available on IEmbeddedContent (non-generic)
                capturedSystem = content.System;

                // Demonstrate using system info in rendering decisions
                return $"""<div class="widget" data-id="{content.System.Id}" data-collection="{content.System.Collection}">{content.System.Name}</div>""";
            })
            .Build();

        var html = await richText!.ToHtmlAsync(resolver);

        // Assert - Verify system metadata was accessible
        Assert.NotNull(capturedSystem);
        Assert.Equal("12345678-1234-1234-1234-123456789012", capturedSystem.Id.ToString());
        Assert.Equal("my_component", capturedSystem.Codename);
        Assert.Equal("custom_widget", capturedSystem.Type);
        Assert.Equal("widgets", capturedSystem.Collection);
        Assert.Equal("My Custom Component", capturedSystem.Name);

        // Verify rendered HTML includes system data
        Assert.Contains("""data-id="12345678-1234-1234-1234-123456789012""", html);
        Assert.Contains("""data-collection="widgets""", html);
        Assert.Contains("My Custom Component", html);
    }

    #endregion
}
