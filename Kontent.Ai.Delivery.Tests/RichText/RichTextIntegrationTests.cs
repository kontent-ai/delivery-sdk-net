using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems.RichText.Resolution;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.Tests.Factories;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.RichText;

/// <summary>
/// Integration tests for rich text resolution using real fixtures and end-to-end scenarios.
/// Tests the complete pipeline from JSON deserialization through parsing to HTML resolution.
/// </summary>
public class RichTextIntegrationTests
{
    private const string FixturesPath = "Fixtures/ContentLinkResolver";

    #region Content Item Link Resolution Tests

    [Fact]
    public async Task IntegrationTest_CoffeeProcessingTechniques_WithTypeSpecificPatterns_GeneratesCorrectUrls()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_processing_techniques.json");

        var resolver = new HtmlResolverBuilder()
            .WithContentItemLinkResolver(DefaultResolvers.UrlPatternResolver(
                new Dictionary<string, string>
                {
                    ["coffee"] = "/shop/products/{urlslug}",
                    ["article"] = "/articles/{urlslug}",
                    ["author"] = "/about/authors/{codename}"
                },
                fallbackPattern: "/content/{id}"))
            .Build();

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        // Verify coffee type links use the shop pattern
        Assert.Contains("href=\"/shop/products/kenya-gakuyuni-aa\"", html);
        Assert.Contains("href=\"/shop/products/brazil-natural-barra-grande\"", html);

        // Verify data-item-id attributes are present
        Assert.Contains("data-item-id=\"80c7074b-3da1-4e1d-882b-c5716ebb4d25\"", html);
        Assert.Contains("data-item-id=\"0c9a11bb-6fc3-409c-b3cb-f0b797e15489\"", html);
    }

    [Fact]
    public async Task IntegrationTest_GetContentItemLinks_ExtractsAllLinks()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_processing_techniques.json");

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();
        var links = result.Value.Elements.BodyCopy.GetContentItemLinks().ToList();

        // Assert
        Assert.Equal(2, links.Count);

        var kenyaLink = links.FirstOrDefault(l => l.Metadata?.Codename == "kenya_gakuyuni_aa");
        Assert.NotNull(kenyaLink);
        Assert.Equal("kenya-gakuyuni-aa", kenyaLink.Metadata?.UrlSlug);
        Assert.Equal("coffee", kenyaLink.Metadata?.ContentTypeCodename);

        var brazilLink = links.FirstOrDefault(l => l.Metadata?.Codename == "brazil_natural_barra_grande");
        Assert.NotNull(brazilLink);
        Assert.Equal("brazil-natural-barra-grande", brazilLink.Metadata?.UrlSlug);
        Assert.Equal("coffee", brazilLink.Metadata?.ContentTypeCodename);
    }

    [Fact]
    public async Task IntegrationTest_OnRoasts_WithNoLinks_ReturnsHtmlWithoutAnchors()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("on_roasts.json");

        var resolver = new HtmlResolverBuilder().Build();

        // Act
        var result = await client.GetItem<Article>("on_roasts").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(html);

        // Verify content is present
        Assert.Contains("Light Roasts", html);
        Assert.Contains("Medium roast", html);
        Assert.Contains("Dark Roasts", html);

        // Verify structure
        Assert.Contains("<h3>", html);
        Assert.Contains("<ul>", html);
        Assert.Contains("<li>", html);

        // Verify no content item links (no data-item-id)
        Assert.DoesNotContain("data-item-id", html);
    }

    #endregion

    #region Inline Content Item Resolution Tests

    [Fact]
    public async Task IntegrationTest_CoffeeBeveragesExplained_WithInlineContentItems()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        var resolver = new HtmlResolverBuilder()
            .WithInlineContentItemResolver((block, context, _) =>
            {
                // Custom resolver for inline items
                if (block.ContentItem != null)
                {
                    var allProp = block.ContentItem.GetType().GetProperties();
                    var systemProp = block.ContentItem.GetType().GetProperty("System");
                    if (systemProp != null)
                    {
                        var system = systemProp.GetValue(block.ContentItem);
                        var typeCodename = system?.GetType().GetProperty("Type")?.GetValue(system) as string;

                        if (typeCodename == "tweet")
                        {
                            return ValueTask.FromResult("<div class=\"tweet-embed\">[Tweet content]</div>");
                        }
                        else if (typeCodename == "hosted_video")
                        {
                            return ValueTask.FromResult("<div class=\"video-embed\">[Video content]</div>");
                        }
                    }
                }
                return ValueTask.FromResult("<!-- Inline item -->");
            })
            .Build();

        // Act
        var result = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(html);

        // Verify inline items are rendered
        Assert.Contains("<div class=\"tweet-embed\">", html);
        Assert.Contains("<div class=\"video-embed\">", html);
    }

    [Fact]
    public async Task IntegrationTest_GetInlineContentItems_ExtractsAllItems()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        // Act
        var result = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();
        var inlineItems = result.Value.Elements.BodyCopy.GetInlineContentItems().ToList();

        // Assert
        Assert.Equal(2, inlineItems.Count); // americano and how_to_make_a_cappuccino

        // Verify items have content
        Assert.All(inlineItems, item => Assert.NotNull(item.ContentItem));
    }

    #endregion

    #region Custom Resolver Scenarios

    [Fact]
    public async Task IntegrationTest_CustomLinkResolver_WithMetadataAccess()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_processing_techniques.json");

        var resolver = new HtmlResolverBuilder()
            .WithContentItemLinkResolver(async (link, context, resolveChildren) =>
            {
                var innerHtml = await resolveChildren(link.Children);

                // Use link metadata to create custom HTML
                if (link.Metadata != null)
                {
                    return $"<a href=\"/{link.Metadata.UrlSlug}\" class=\"coffee-link\" data-type=\"{link.Metadata.ContentTypeCodename}\">{innerHtml}</a>";
                }

                return $"<a href=\"#\">{innerHtml}</a>";
            })
            .Build();

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.Contains("class=\"coffee-link\"", html);
        Assert.Contains("data-type=\"coffee\"", html);
    }

    [Fact]
    public async Task IntegrationTest_CustomLinkResolver_WithNestedFormatting()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_processing_techniques.json");

        var resolver = new HtmlResolverBuilder()
            .WithContentItemLinkResolver(async (link, context, resolveChildren) =>
            {
                // Resolve children (which may contain <strong>, <em>, etc.)
                var innerHtml = await resolveChildren(link.Children);

                return $"<a href=\"/{link.Metadata?.UrlSlug}\" class=\"custom-link\">{innerHtml}</a>";
            })
            .Build();

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        // Verify nested formatting is preserved in link text
        Assert.Contains("<a href=", html);
        Assert.Contains("class=\"custom-link\"", html);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task IntegrationTest_EmptyRichText_ReturnsParagraphWithLineBreak()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var guid = Guid.NewGuid().ToString();
        var url = $"https://deliver.kontent.ai/{guid}/items/empty_article";

        var emptyResponse = @"{
            ""item"": {
                ""system"": {
                    ""id"": ""12345678-1234-1234-1234-123456789012"",
                    ""name"": ""Empty Article"",
                    ""codename"": ""empty_article"",
                    ""language"": ""en-US"",
                    ""type"": ""article""
                },
                ""elements"": {
                    ""body_copy"": {
                        ""type"": ""rich_text"",
                        ""name"": ""Body Copy"",
                        ""images"": {},
                        ""links"": {},
                        ""modular_content"": [],
                        ""value"": ""<p><br></p>""
                    }
                }
            },
            ""modular_content"": {}
        }";

        mockHttp.When(url).Respond("application/json", emptyResponse);

        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = guid },
            configureHttpClient: builder => builder.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        services.AddSingleton<ITypeProvider, CustomTypeProvider>();

        var provider = services.BuildServiceProvider();
        var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

        var resolver = new HtmlResolverBuilder().Build();

        // Act
        var result = await client.GetItem<Article>("empty_article").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("<p><br></p>", html);
    }

    [Fact]
    public async Task IntegrationTest_RichTextMetadata_IsPopulatedCorrectly()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_processing_techniques.json");

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();
        var richText = result.Value.Elements.BodyCopy;

        // Assert
        // Rich text should have metadata about links
        var links = richText.GetContentItemLinks().ToList();
        Assert.Equal(2, links.Count);

        // Verify each link has complete metadata
        Assert.All(links, link =>
        {
            Assert.NotNull(link.Metadata);
            Assert.NotEmpty(link.Metadata.Codename);
            Assert.NotEmpty(link.Metadata.UrlSlug);
            Assert.NotEmpty(link.Metadata.ContentTypeCodename);
        });
    }

    #endregion

    #region Complex HTML Structure Tests

    [Fact]
    public async Task IntegrationTest_ComplexHtmlStructure_ParsesCorrectly()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("on_roasts.json");

        var resolver = new HtmlResolverBuilder().Build();

        // Act
        var result = await client.GetItem<Article>("on_roasts").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        // Verify various HTML elements are preserved
        Assert.Contains("<p>", html);
        Assert.Contains("</p>", html);
        Assert.Contains("<h3>", html);
        Assert.Contains("</h3>", html);
        Assert.Contains("<ul>", html);
        Assert.Contains("</ul>", html);
        Assert.Contains("<li>", html);
        Assert.Contains("</li>", html);

        // Verify list items are present
        Assert.Contains("Caffeine level decreases", html);
        Assert.Contains("Darker roasts exempt less acidity", html);
    }

    [Fact]
    public async Task IntegrationTest_AutomaticDefaultResolvers_RenderInlineItemsAsComments()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        // No explicit resolvers configured - defaults are automatic
        var resolver = new HtmlResolverBuilder().Build();

        // Act
        var result = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(html);
        Assert.NotEmpty(html);

        // Verify content is rendered (inline items show as comments by default)
        Assert.Contains("<!--", html); // Default comment resolver for inline items
        Assert.Contains("Missing resolver for items or components of type", html);
    }

    [Fact]
    public async Task IntegrationTest_AutomaticDefaultResolvers_RenderItemLinksAsComments()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("on_roasts.json");

        // No explicit resolvers configured - defaults are automatic
        var resolver = new HtmlResolverBuilder().Build();

        // Act
        var result = await client.GetItem<Article>("on_roasts").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(html);
        Assert.NotEmpty(html);

        // Verify content is rendered (inline items show as comments by default)
        Assert.Contains("<!--", html); // Default comment resolver for inline items
        Assert.Contains("Missing resolver for link to a content type", html);
    }

    #endregion

    #region Performance and Scale Tests

    [Fact]
    public async Task IntegrationTest_LargeRichText_HandlesEfficiently()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_processing_techniques.json");

        var resolver = new HtmlResolverBuilder().Build();

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();

        var startTime = DateTime.UtcNow;
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.NotEmpty(html);
        Assert.True(duration.TotalMilliseconds < 1000, "Resolution should complete in under 1 second");
    }

    #endregion

    #region HTML Entity Encoding Tests

    [Fact]
    public async Task IntegrationTest_HtmlEntities_AreProperlyEncoded()
    {
        // Arrange - API returns HTML with entities already encoded
        var mockHttp = new MockHttpMessageHandler();
        var guid = Guid.NewGuid().ToString();
        var url = $"https://deliver.kontent.ai/{guid}/items/html_entities_test";

        var response = @"{
            ""item"": {
                ""system"": {
                    ""id"": ""12345678-1234-1234-1234-123456789012"",
                    ""name"": ""HTML Entities Test"",
                    ""codename"": ""html_entities_test"",
                    ""language"": ""en-US"",
                    ""type"": ""article""
                },
                ""elements"": {
                    ""body_copy"": {
                        ""type"": ""rich_text"",
                        ""name"": ""Body Copy"",
                        ""images"": {},
                        ""links"": {},
                        ""modular_content"": [],
                        ""value"": ""<p>Use the &lt;template&gt; tag in HTML</p><p>Compare: 5 &lt; 10 &amp; 10 &gt; 5</p><p>Quote: &quot;Hello World&quot;</p>""
                    }
                }
            },
            ""modular_content"": {}
        }";

        mockHttp.When(url).Respond("application/json", response);

        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = guid },
            configureHttpClient: builder => builder.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
        services.AddSingleton<ITypeProvider, CustomTypeProvider>();

        var provider = services.BuildServiceProvider();
        var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

        var resolver = new HtmlResolverBuilder().Build();

        // Act
        var result = await client.GetItem<Article>("html_entities_test").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify that entities from API are decoded by AngleSharp parser, then re-encoded for output
        // So &lt; in API becomes < during parsing, then &lt; again in output
        Assert.Contains("&lt;template&gt;", html);
        Assert.Contains("5 &lt; 10 &amp; 10 &gt; 5", html);
        Assert.Contains("&quot;Hello World&quot;", html);

        // Verify the structure is preserved
        Assert.Contains("<p>", html);
        Assert.Contains("</p>", html);
    }

    [Fact]
    public async Task IntegrationTest_HtmlEntities_OutputVerification()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var guid = Guid.NewGuid().ToString();
        var url = $"https://deliver.kontent.ai/{guid}/items/output_test";

        var response = @"{
            ""item"": {
                ""system"": {
                    ""id"": ""12345678-1234-1234-1234-123456789012"",
                    ""name"": ""Output Test"",
                    ""codename"": ""output_test"",
                    ""language"": ""en-US"",
                    ""type"": ""article""
                },
                ""elements"": {
                    ""body_copy"": {
                        ""type"": ""rich_text"",
                        ""name"": ""Body Copy"",
                        ""images"": {},
                        ""links"": {},
                        ""modular_content"": [],
                        ""value"": ""<p>API sends: &lt;tag&gt; and &amp;</p>""
                    }
                }
            },
            ""modular_content"": {}
        }";

        mockHttp.When(url).Respond("application/json", response);

        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = guid },
            configureHttpClient: builder => builder.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
        services.AddSingleton<ITypeProvider, CustomTypeProvider>();

        var provider = services.BuildServiceProvider();
        var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

        var resolver = new HtmlResolverBuilder().Build();

        // Act
        var result = await client.GetItem<Article>("output_test").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.True(result.IsSuccess);

        // The output should be: <p>API sends: &lt;tag&gt; and &amp;</p>
        // This demonstrates that:
        // 1. API returns: &lt;tag&gt; and &amp;
        // 2. AngleSharp decodes to: <tag> and &
        // 3. SDK re-encodes to: &lt;tag&gt; and &amp;
        Assert.Equal("<p>API sends: &lt;tag&gt; and &amp;</p>", html);
    }

    [Fact]
    public async Task IntegrationTest_PlainText_WithoutSpecialChars_IsUnchanged()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var guid = Guid.NewGuid().ToString();
        var url = $"https://deliver.kontent.ai/{guid}/items/plain_text_test";

        var response = @"{
            ""item"": {
                ""system"": {
                    ""id"": ""12345678-1234-1234-1234-123456789012"",
                    ""name"": ""Plain Text Test"",
                    ""codename"": ""plain_text_test"",
                    ""language"": ""en-US"",
                    ""type"": ""article""
                },
                ""elements"": {
                    ""body_copy"": {
                        ""type"": ""rich_text"",
                        ""name"": ""Body Copy"",
                        ""images"": {},
                        ""links"": {},
                        ""modular_content"": [],
                        ""value"": ""<p>This is plain text without any special characters.</p>""
                    }
                }
            },
            ""modular_content"": {}
        }";

        mockHttp.When(url).Respond("application/json", response);

        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = guid },
            configureHttpClient: builder => builder.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
        services.AddSingleton<ITypeProvider, CustomTypeProvider>();

        var provider = services.BuildServiceProvider();
        var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

        var resolver = new HtmlResolverBuilder().Build();

        // Act
        var result = await client.GetItem<Article>("plain_text_test").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("<p>This is plain text without any special characters.</p>", html);
    }

    #endregion

    #region Missing Resolver Diagnostics Tests

    [Fact]
    public async Task IntegrationTest_MissingContentItemLinkResolver_RendersDiagnosticWithContext()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_processing_techniques.json");

        // Build resolver WITHOUT content item link resolver
        // Note: No need to call WithDefaultResolvers() - defaults are automatic!
        var resolver = new HtmlResolverBuilder().Build();

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify diagnostic comment includes context (item ID and codename)
        Assert.Contains("<!-- [Kontent.ai SDK] Missing resolver for link to a content type: \"coffee\"", html);
        Assert.Contains("80c7074b-3da1-4e1d-882b-c5716ebb4d25", html); // Item ID

        // Verify text and HTML elements still render correctly with built-in defaults
        Assert.Contains("<p>", html);
        Assert.Contains("</p>", html);
    }

    #endregion

    #region Helper Methods

    private async Task<IDeliveryClient> CreateDeliveryClientAsync(string fixtureFileName)
    {
        var mockHttp = new MockHttpMessageHandler();
        var guid = Guid.NewGuid().ToString();

        var codename = Path.GetFileNameWithoutExtension(fixtureFileName);
        var url = $"https://deliver.kontent.ai/{guid}/items/{codename}";

        var fixturePath = Path.Combine(Environment.CurrentDirectory, FixturesPath, fixtureFileName);
        var fixtureContent = await File.ReadAllTextAsync(fixturePath);

        mockHttp.When(url).Respond("application/json", fixtureContent);

        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = guid },
            configureHttpClient: builder => builder.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        services.AddSingleton<ITypeProvider, CustomTypeProvider>();

        var provider = services.BuildServiceProvider();
        return (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();
    }

    #endregion
}
