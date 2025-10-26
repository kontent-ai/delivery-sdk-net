using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests;

/// <summary>
/// Tests for verifying that complex elements (taxonomy, datetime, assets, rich text)
/// are properly hydrated when querying with strongly-typed models.
/// </summary>
public class ComplexElementsHydrationTests
{
    private readonly Guid _guid = Guid.NewGuid();
    private string BaseUrl => $"https://deliver.kontent.ai/{_guid}";

    private IDeliveryClient CreateClient(MockHttpMessageHandler mockHttp)
    {
        var services = new ServiceCollection();
        var options = new DeliveryOptions { EnvironmentId = _guid.ToString() };
        services.AddDeliveryClient(options, configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    [Fact]
    public async Task ComplexElements_CoffeeProcessingTechniques_AllHydratedCorrectly()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}coffee_processing_techniques.json"));

        mock.When($"{BaseUrl}/items/coffee_processing_techniques")
            .Respond("application/json", fixtureContent);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();

        // Assert - Basic response
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Value.Elements);

        // Assert - String element (baseline)
        Assert.Equal("Coffee processing techniques", result.Value.Elements.Title);

        // Assert - Taxonomy elements (Personas)
        Assert.NotNull(result.Value.Elements.Personas);
        var personas = result.Value.Elements.Personas.ToList();
        Assert.Equal(2, personas.Count);
        Assert.Contains(personas, p => p.Name == "Coffee blogger" && p.Codename == "coffee_blogger");
        Assert.Contains(personas, p => p.Name == "Coffee lover" && p.Codename == "coffee_lover");

        // Assert - DateTime element (PostDate)
        Assert.NotNull(result.Value.Elements.PostDate);
        Assert.Equal(new DateTime(2014, 11, 2, 0, 0, 0, DateTimeKind.Utc), result.Value.Elements.PostDate.Value);

        // Assert - Asset elements (TeaserImage)
        Assert.NotNull(result.Value.Elements.TeaserImage);
        var assets = result.Value.Elements.TeaserImage.ToList();
        Assert.Single(assets);

        var teaserImage = assets[0];
        Assert.Equal("coffee-processing-techniques-1080px.jpg", teaserImage.Name);
        Assert.Equal("image/jpeg", teaserImage.Type);
        Assert.Equal(108409, teaserImage.Size);
        Assert.Equal("Dry process (also known as unwashed or natural coffee)", teaserImage.Description);
        Assert.Contains("coffee-processing-techniques-1080px.jpg", teaserImage.Url);
    }

    [Fact]
    public async Task ComplexElements_CoffeeBeveragesExplained_AllHydratedCorrectly()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json"));

        mock.When($"{BaseUrl}/items/coffee_beverages_explained")
            .Respond("application/json", fixtureContent);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();

        // Assert - Basic response
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Value.Elements);

        // Assert - String element (baseline)
        Assert.Equal("Coffee Beverages Explained", result.Value.Elements.Title);

        // Assert - Taxonomy elements (Personas)
        Assert.NotNull(result.Value.Elements.Personas);
        var personas = result.Value.Elements.Personas.ToList();
        Assert.Single(personas);
        Assert.Equal("Coffee lover", personas[0].Name);
        Assert.Equal("coffee_lover", personas[0].Codename);

        // Assert - DateTime element (PostDate)
        Assert.NotNull(result.Value.Elements.PostDate);
        Assert.Equal(new DateTime(2014, 11, 18, 0, 0, 0, DateTimeKind.Utc), result.Value.Elements.PostDate.Value);

        // Assert - Asset elements with renditions (TeaserImage)
        Assert.NotNull(result.Value.Elements.TeaserImage);
        var assets = result.Value.Elements.TeaserImage.ToList();
        Assert.Single(assets);

        var teaserImage = assets[0];
        Assert.Equal("coffee-beverages-explained-1080px.jpg", teaserImage.Name);
        Assert.Equal("image/jpeg", teaserImage.Type);
        Assert.Equal(90895, teaserImage.Size);
        Assert.Equal("Professional Espresso Machine", teaserImage.Description);
        Assert.Equal(800, teaserImage.Width);
        Assert.Equal(600, teaserImage.Height);
        Assert.Contains("coffee-beverages-explained-1080px.jpg", teaserImage.Url);

        // Assert - Asset renditions
        Assert.NotNull(teaserImage.Renditions);
        Assert.Single(teaserImage.Renditions);
        var defaultRendition = teaserImage.Renditions["default"];
        Assert.NotNull(defaultRendition);
        Assert.Equal(200, defaultRendition.Width);
        Assert.Equal(150, defaultRendition.Height);
        Assert.Equal("w=200&h=150&fit=clip&rect=7,23,300,200", defaultRendition.Query);
    }

    [Fact]
    public async Task TaxonomyElement_MultipleTerms_AllDeserialized()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}coffee_processing_techniques.json"));

        mock.When($"{BaseUrl}/items/coffee_processing_techniques")
            .Respond("application/json", fixtureContent);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();

        // Assert - Verify taxonomy terms are in correct order
        var personas = result.Value.Elements.Personas!.ToList();
        Assert.Equal(2, personas.Count);
        Assert.Equal("Coffee blogger", personas[0].Name);
        Assert.Equal("coffee_blogger", personas[0].Codename);
        Assert.Equal("Coffee lover", personas[1].Name);
        Assert.Equal("coffee_lover", personas[1].Codename);
    }

    [Fact]
    public async Task DateTimeElement_DeserializesToCorrectUtcTime()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json"));

        mock.When($"{BaseUrl}/items/coffee_beverages_explained")
            .Respond("application/json", fixtureContent);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();

        // Assert - Verify DateTime is properly deserialized
        Assert.NotNull(result.Value.Elements.PostDate);
        var postDate = result.Value.Elements.PostDate.Value;
        Assert.Equal(DateTimeKind.Utc, postDate.Kind);
        Assert.Equal(2014, postDate.Year);
        Assert.Equal(11, postDate.Month);
        Assert.Equal(18, postDate.Day);
        Assert.Equal(0, postDate.Hour);
        Assert.Equal(0, postDate.Minute);
        Assert.Equal(0, postDate.Second);
    }

    [Fact]
    public async Task AssetElement_WithRenditions_AllPropertiesDeserialized()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json"));

        mock.When($"{BaseUrl}/items/coffee_beverages_explained")
            .Respond("application/json", fixtureContent);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();

        // Assert - Verify all asset properties
        var asset = result.Value.Elements.TeaserImage!.First();

        // Basic properties
        Assert.False(string.IsNullOrEmpty(asset.Name));
        Assert.False(string.IsNullOrEmpty(asset.Type));
        Assert.True(asset.Size > 0);
        Assert.False(string.IsNullOrEmpty(asset.Description));
        Assert.False(string.IsNullOrEmpty(asset.Url));

        // Dimensions
        Assert.True(asset.Width > 0);
        Assert.True(asset.Height > 0);

        // Renditions
        Assert.NotNull(asset.Renditions);
        Assert.NotEmpty(asset.Renditions);

        foreach (var rendition in asset.Renditions.Values)
        {
            Assert.True(rendition.Width > 0);
            Assert.True(rendition.Height > 0);
            Assert.False(string.IsNullOrEmpty(rendition.Query));
        }
    }

    [Fact]
    public async Task AssetElement_WithoutRenditions_BasicPropertiesDeserialized()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}coffee_processing_techniques.json"));

        mock.When($"{BaseUrl}/items/coffee_processing_techniques")
            .Respond("application/json", fixtureContent);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();

        // Assert - Verify asset without renditions
        var asset = result.Value.Elements.TeaserImage!.First();

        Assert.Equal("coffee-processing-techniques-1080px.jpg", asset.Name);
        Assert.Equal("image/jpeg", asset.Type);
        Assert.Equal(108409, asset.Size);
        Assert.False(string.IsNullOrEmpty(asset.Url));

        // This fixture doesn't have renditions
        Assert.True(asset.Renditions == null || !asset.Renditions.Any());
    }

    [Fact]
    public async Task RichTextElement_WithLinksAndModularContent_Hydrated()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}coffee_processing_techniques.json"));

        mock.When($"{BaseUrl}/items/coffee_processing_techniques")
            .Respond("application/json", fixtureContent);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();

        // Assert - Verify rich text is hydrated
        Assert.NotNull(result.Value.Elements.BodyCopy);

        var bodyContent = result.Value.Elements.BodyCopy;

        // Rich text is a list of blocks (structured representation)
        Assert.NotEmpty(bodyContent); // RichTextContent inherits from List<IRichTextBlock>

        // Verify links are accessible via extension method
        var links = bodyContent.GetContentItemLinks();
        Assert.NotNull(links);
        Assert.NotEmpty(links);
        Assert.Equal(2, links.Count()); // coffee_processing_techniques has 2 content links
    }
}