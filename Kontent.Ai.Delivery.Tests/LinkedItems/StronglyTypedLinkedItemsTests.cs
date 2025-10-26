using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.LinkedItems;

public class StronglyTypedLinkedItemsTests
{
    private const string FixturesPath = "Fixtures/ContentLinkResolver";

    [Fact]
    public async Task LinkedItems_WithRuntimeTyping_HydratesStronglyTypedContent()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("on_roasts.json");

        // Act
        var response = await client.GetItem<Article>("on_roasts").ExecuteAsync();

        // Assert
        Assert.NotNull(response.Value);
        Assert.NotNull(response.Value.Elements.RelatedArticles);

        var relatedArticles = response.Value.Elements.RelatedArticles!.ToList();
        Assert.Equal(2, relatedArticles.Count);

        // Verify runtime typing - each item should be IEmbeddedContent
        foreach (var article in relatedArticles)
        {
            Assert.NotNull(article);
            Assert.IsAssignableFrom<IEmbeddedContent>(article);
            Assert.Equal("article", article.ContentTypeCodename);
            Assert.NotEmpty(article.Codename);
        }
    }

    [Fact]
    public async Task LinkedItems_PatternMatching_WorksWithTypedContent()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("on_roasts.json");

        // Act
        var response = await client.GetItem<Article>("on_roasts").ExecuteAsync();
        var articleCount = 0;

        foreach (var item in response.Value.Elements.RelatedArticles!)
        {
            // Pattern matching with strongly-typed embedded content
            if (item is IEmbeddedContent<Article> typedArticle)
            {
                articleCount++;
                Assert.NotNull(typedArticle.Elements);
                Assert.NotNull(typedArticle.Elements.Title);
            }
        }

        // Assert
        Assert.Equal(2, articleCount);
    }

    [Fact]
    public async Task LinkedItems_GetEmbeddedContent_FiltersCorrectly()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("on_roasts.json");

        // Act
        var response = await client.GetItem<Article>("on_roasts").ExecuteAsync();

        // Use extension method to filter typed content
        var typedArticles = response.Value.Elements.RelatedArticles!
            .OfType<IEmbeddedContent<Article>>()
            .ToList();

        // Assert
        Assert.Equal(2, typedArticles.Count);
        foreach (var article in typedArticles)
        {
            Assert.NotNull(article.Elements);
            Assert.NotNull(article.Elements.Title);
        }
    }

    [Fact]
    public async Task LinkedItems_GetEmbeddedElements_ExtractsModels()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("on_roasts.json");

        // Act
        var response = await client.GetItem<Article>("on_roasts").ExecuteAsync();

        // Extract element models directly
        var articleElements = response.Value.Elements.RelatedArticles!
            .OfType<IEmbeddedContent<Article>>()
            .Select(a => a.Elements)
            .ToList();

        // Assert
        Assert.Equal(2, articleElements.Count);
        foreach (var article in articleElements)
        {
            Assert.NotNull(article.Title);
        }
    }

    [Fact]
    public async Task LinkedItems_HasCorrectMetadata()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("on_roasts.json");

        // Act
        var response = await client.GetItem<Article>("on_roasts").ExecuteAsync();
        var relatedArticles = response.Value.Elements.RelatedArticles!.ToList();

        // Assert
        Assert.Equal(2, relatedArticles.Count);

        var expectedCodenames = new[] { "coffee_processing_techniques", "origins_of_arabica_bourbon" };
        var actualCodenames = relatedArticles.Select(a => a.Codename).ToList();

        Assert.Equal(expectedCodenames, actualCodenames);

        foreach (var article in relatedArticles)
        {
            Assert.Equal("article", article.ContentTypeCodename);
            Assert.NotEmpty(article.Codename);
            Assert.NotEqual(Guid.Empty, article.Id);
        }
    }

    [Fact]
    public async Task LinkedItems_EmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        // Act
        var response = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();

        // related_articles is empty in this fixture
        var relatedArticles = response.Value.Elements.RelatedArticles?.ToList();

        // Assert - should not throw, just be empty
        Assert.NotNull(relatedArticles);
        Assert.Empty(relatedArticles);
    }

    [Fact]
    public async Task LinkedItems_VerifySpecificArticleContent()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("on_roasts.json");

        // Act
        var response = await client.GetItem<Article>("on_roasts").ExecuteAsync();
        var relatedArticles = response.Value.Elements.RelatedArticles!.ToList();

        // Assert - verify specific article data
        var processingTechniques = relatedArticles
            .OfType<IEmbeddedContent<Article>>()
            .FirstOrDefault(a => a.Codename == "coffee_processing_techniques");

        Assert.NotNull(processingTechniques);
        Assert.Equal("Coffee processing techniques", processingTechniques.Elements.Title);
        Assert.NotNull(processingTechniques.Elements.Summary);
        Assert.Equal(new Guid("117cdfae-52cf-4885-b271-66aef6825612"), processingTechniques.Id);
    }

    [Fact]
    public async Task LinkedItems_NestedInRichText_BothWorkCorrectly()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        // Act
        var response = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();

        // Assert - both rich text embedded content and linked items should work
        Assert.NotNull(response.Value);

        // Rich text embedded content - coffee_beverages_explained has embedded items in body_copy
        var embeddedInRichText = response.Value.Elements.BodyCopy!
            .OfType<IEmbeddedContent>()
            .ToList();

        Assert.NotEmpty(embeddedInRichText);
        Assert.Equal(2, embeddedInRichText.Count);

        // Verify embedded content types
        var codenames = embeddedInRichText.Select(e => e.Codename).ToList();
        Assert.Contains("americano", codenames);
        Assert.Contains("how_to_make_a_cappuccino", codenames);

        // Linked items - related_articles is empty for this article
        var linkedItems = response.Value.Elements.RelatedArticles!.ToList();
        Assert.Empty(linkedItems);
    }

    [Fact]
    public async Task LinkedItems_MultipleArticles_AllHydrated()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("on_roasts.json");

        // Act
        var response = await client.GetItem<Article>("on_roasts").ExecuteAsync();
        var relatedArticles = response.Value.Elements.RelatedArticles!.ToList();

        // Assert - verify both linked articles are hydrated with their full data
        Assert.Equal(2, relatedArticles.Count);

        var processingTechniques = relatedArticles
            .OfType<IEmbeddedContent<Article>>()
            .FirstOrDefault(a => a.Codename == "coffee_processing_techniques");

        var arabicaBourbon = relatedArticles
            .OfType<IEmbeddedContent<Article>>()
            .FirstOrDefault(a => a.Codename == "origins_of_arabica_bourbon");

        // Verify both articles are fully hydrated
        Assert.NotNull(processingTechniques);
        Assert.Equal("Coffee processing techniques", processingTechniques.Elements.Title);
        Assert.NotNull(processingTechniques.Elements.Summary);

        Assert.NotNull(arabicaBourbon);
        Assert.Equal("Origins of Arabica Bourbon", arabicaBourbon.Elements.Title);
        Assert.NotNull(arabicaBourbon.Elements.Summary);
    }

    private async Task<IDeliveryClient> CreateDeliveryClientAsync(string fixtureFileName)
    {
        var mockHttp = new MockHttpMessageHandler();
        var guid = Guid.NewGuid().ToString();

        var codename = Path.GetFileNameWithoutExtension(fixtureFileName);
        var url = $"https://deliver.kontent.ai/{guid}/items/{codename}";

        var fixturePath = Path.Combine(Environment.CurrentDirectory, FixturesPath, fixtureFileName);

        // Check if fixture exists
        if (!File.Exists(fixturePath))
        {
            // If fixture doesn't exist, these tests will be skipped or fail gracefully
            var fixtureContent = "{}";
            mockHttp.When(url).Respond("application/json", fixtureContent);
        }
        else
        {
            var fixtureContent = await File.ReadAllTextAsync(fixturePath);
            mockHttp.When(url).Respond("application/json", fixtureContent);
        }

        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = guid },
            configureHttpClient: builder => builder.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        services.AddSingleton<ITypeProvider, CustomTypeProvider>();

        var provider = services.BuildServiceProvider();
        return (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();
    }
}
