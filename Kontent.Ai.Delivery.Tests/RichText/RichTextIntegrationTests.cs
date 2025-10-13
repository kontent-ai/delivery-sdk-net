using System;
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
    public async Task IntegrationTest_CoffeeProcessingTechniques_ResolvesLinksCorrectly()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_processing_techniques.json");
        var urlResolver = new TestContentLinkUrlResolver(
            link => $"http://example.org/{link.UrlSlug}",
            () => "http://example.org/broken");

        var resolver = new HtmlResolverBuilder()
            .WithDefaultResolvers()
            .WithContentItemLinkResolver(DefaultResolvers.LegacyUrlResolver(urlResolver))
            .Build();

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(html);

        // Verify both links are resolved
        Assert.Contains("href=\"http://example.org/kenya-gakuyuni-aa\"", html);
        Assert.Contains("href=\"http://example.org/brazil-natural-barra-grande\"", html);

        // Verify link text is preserved
        Assert.Contains("Kenya Gakuyuni AA", html);
        Assert.Contains("Brazil Natural Barra Grande", html);

        // Verify data-item-id attributes are present
        Assert.Contains("data-item-id=\"80c7074b-3da1-4e1d-882b-c5716ebb4d25\"", html);
        Assert.Contains("data-item-id=\"0c9a11bb-6fc3-409c-b3cb-f0b797e15489\"", html);

        // Verify surrounding text is preserved
        Assert.Contains("If you are curious about the taste", html);
        Assert.Contains("Check out our", html);
    }

    [Fact]
    public async Task IntegrationTest_CoffeeProcessingTechniques_WithUrlPattern_GeneratesCorrectUrls()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_processing_techniques.json");

        var resolver = new HtmlResolverBuilder()
            .WithDefaultResolvers()
            .WithContentItemLinkResolver(DefaultResolvers.UrlPatternResolver("/products/{type}/{urlslug}"))
            .Build();

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.Contains("href=\"/products/coffee/kenya-gakuyuni-aa\"", html);
        Assert.Contains("href=\"/products/coffee/brazil-natural-barra-grande\"", html);
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

        var resolver = new HtmlResolverBuilder()
            .WithDefaultResolvers()
            .Build();

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
            .WithDefaultResolvers()
            .WithInlineContentItemResolver((block, context, _) =>
            {
                // Custom resolver for inline items
                if (block.ContentItem != null)
                {
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
            .WithDefaultResolvers()
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
            .WithDefaultResolvers()
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
    public async Task IntegrationTest_EmptyRichText_ReturnsEmptyString()
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
                        ""value"": """"
                    }
                }
            }
        }";

        mockHttp.When(url).Respond("application/json", emptyResponse);

        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = guid },
            configureHttpClient: builder => builder.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        services.AddSingleton<ITypeProvider, CustomTypeProvider>();

        var provider = services.BuildServiceProvider();
        var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

        var resolver = new HtmlResolverBuilder().WithDefaultResolvers().Build();

        // Act
        var result = await client.GetItem<Article>("empty_article").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(html);
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

        var resolver = new HtmlResolverBuilder()
            .WithDefaultResolvers()
            .Build();

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
    public async Task IntegrationTest_WithDefaultResolvers_RendersAllContent()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        var resolver = new HtmlResolverBuilder()
            .WithDefaultResolvers() // Use all default resolvers
            .Build();

        // Act
        var result = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(html);
        Assert.NotEmpty(html);

        // Verify content is rendered (inline items show as comments by default)
        Assert.Contains("<!--", html); // Default comment resolver for inline items
        Assert.Contains("Inline content item", html);
    }

    #endregion

    #region Performance and Scale Tests

    [Fact]
    public async Task IntegrationTest_LargeRichText_HandlesEfficiently()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_processing_techniques.json");

        var resolver = new HtmlResolverBuilder()
            .WithDefaultResolvers()
            .Build();

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();

        var startTime = DateTime.UtcNow;
        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.NotEmpty(html);
        Assert.True(duration.TotalMilliseconds < 1000, "Resolution should complete in under 1 second");
    }

    [Fact]
    public async Task IntegrationTest_MultipleResolutions_ProducesSameOutput()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_processing_techniques.json");

        var resolver = new HtmlResolverBuilder()
            .WithDefaultResolvers()
            .WithContentItemLinkResolver(DefaultResolvers.UrlPatternResolver("/articles/{urlslug}"))
            .Build();

        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();

        // Act
        var html1 = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);
        var html2 = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);
        var html3 = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert - All resolutions should produce identical output
        Assert.Equal(html1, html2);
        Assert.Equal(html2, html3);
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

    private class TestContentLinkUrlResolver : IContentLinkUrlResolver
    {
        private readonly Func<IContentLink, string> _linkUrl;
        private readonly Func<string> _brokenUrl;

        public TestContentLinkUrlResolver(Func<IContentLink, string> linkUrl, Func<string> brokenUrl)
        {
            _linkUrl = linkUrl;
            _brokenUrl = brokenUrl;
        }

        public Task<string> ResolveLinkUrlAsync(IContentLink link) => Task.FromResult(_linkUrl(link));
        public Task<string> ResolveBrokenLinkUrlAsync() => Task.FromResult(_brokenUrl());
    }

    #endregion
}
