using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.RichText.Resolution;
using Kontent.Ai.Delivery.Generated;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.RichText;

public class StronglyTypedEmbeddedContentTests
{
    private const string FixturesPath = "Fixtures/ContentLinkResolver";
    private const string ArticleWithEmbeddedTweetsCodename = "coffee_beverages_explained";

    [Fact]
    public async Task RichText_WithTypedResolver_UsesGenericResolver()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        var resolver = new HtmlResolverBuilder()
            .WithContentResolver<Tweet>(tweet =>
                $"<div class=\"typed-tweet\" data-codename=\"{tweet.System.Codename}\">{tweet.Elements.DisplayOptions?.FirstOrDefault()?.Name ?? "No options"}</div>")
            .Build();

        // Act
        var response = await client.GetItem<Article>(ArticleWithEmbeddedTweetsCodename).ExecuteAsync();
        var html = await response.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.Contains("typed-tweet", html);
        Assert.Contains("data-codename", html);
    }

    [Fact]
    public async Task RichText_WithTypedResolver_TakesPrecedenceOverCodenameResolver()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        var resolver = new HtmlResolverBuilder()
            // Type-based resolver (should win)
            .WithContentResolver<Tweet>(tweet =>
                $"<div class=\"type-based\">Type-based resolver</div>")
            // Codename-based resolver (should be ignored for tweets)
            .WithContentResolver("tweet", content =>
                $"<div class=\"codename-based\">Codename-based resolver</div>")
            .Build();

        // Act
        var response = await client.GetItem<Article>(ArticleWithEmbeddedTweetsCodename).ExecuteAsync();
        var html = await response.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.Contains("type-based", html);
        Assert.Contains("Type-based resolver", html);
        Assert.DoesNotContain("codename-based", html);
    }

    [Fact]
    public async Task RichText_GetEmbeddedContent_ReturnsTypedEmbeddedContent()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        // Act
        var response = await client.GetItem<Article>(ArticleWithEmbeddedTweetsCodename).ExecuteAsync();
        var tweets = response.Value?.Elements?.BodyCopy.GetEmbeddedContent<Tweet>().ToList();

        // Assert
        Assert.NotNull(tweets);
        Assert.NotEmpty(tweets);
        foreach (var tweet in tweets)
        {
            Assert.NotNull(tweet.Elements);
            Assert.NotNull(tweet.Elements.DisplayOptions);
            Assert.IsType<IEmbeddedContent<Tweet>>(tweet, exactMatch: false);
        }
    }

    [Fact]
    public async Task RichText_GetEmbeddedElements_ReturnsTypedElements()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        // Act
        var response = await client.GetItem<Article>(ArticleWithEmbeddedTweetsCodename).ExecuteAsync();
        var tweetElements = response.Value.Elements.BodyCopy.GetEmbeddedElements<Tweet>().ToList();

        // Assert
        Assert.NotEmpty(tweetElements);
        foreach (var tweetElement in tweetElements)
        {
            Assert.NotNull(tweetElement);
            Assert.NotNull(tweetElement.DisplayOptions);
            Assert.IsType<Tweet>(tweetElement);
        }
    }

    [Fact]
    public async Task RichText_PatternMatching_WorksWithTypedEmbeddedContent()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        // Act
        var response = await client.GetItem<Article>(ArticleWithEmbeddedTweetsCodename).ExecuteAsync();
        var tweetCount = 0;

        foreach (var block in response.Value.Elements.BodyCopy)
        {
            switch (block)
            {
                case IEmbeddedContent<Tweet> tweet:
                    tweetCount++;
                    Assert.NotNull(tweet.Elements);
                    Assert.NotNull(tweet.Elements.DisplayOptions);
                    break;
            }
        }

        // Assert
        Assert.True(tweetCount > 0, "Expected to find at least one typed tweet in rich text");
    }

    [Fact]
    public async Task RichText_WithAsyncTypedResolver_WorksCorrectly()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        var resolver = new HtmlResolverBuilder()
            .WithContentResolver<Tweet>(async tweet =>
            {
                await Task.Delay(1); // Simulate async work
                return $"<div class=\"async-tweet\">{tweet.Elements.DisplayOptions?.FirstOrDefault()?.Name ?? "Async tweet"}</div>";
            })
            .Build();

        // Act
        var response = await client.GetItem<Article>(ArticleWithEmbeddedTweetsCodename).ExecuteAsync();
        var html = await response.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.Contains("async-tweet", html);
    }

    [Fact]
    public async Task RichText_WithDictionaryTypedResolvers_WorksCorrectly()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        var resolvers = new Dictionary<Type, Func<IEmbeddedContent, string>>
        {
            [typeof(Tweet)] = content =>
            {
                if (content is IEmbeddedContent<Tweet> tweet)
                {
                    return $"<div class=\"dict-tweet\">{tweet.Elements.DisplayOptions?.FirstOrDefault()?.Name ?? "Dict tweet"}</div>";
                }
                return string.Empty;
            }
        };

        var resolver = new HtmlResolverBuilder()
            .WithContentResolvers(resolvers)
            .Build();

        // Act
        var response = await client.GetItem<Article>(ArticleWithEmbeddedTweetsCodename).ExecuteAsync();
        var html = await response.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.Contains("dict-tweet", html);
    }

    [Fact]
    public async Task RichText_WithoutTypedResolver_FallsBackToCodename()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        var resolver = new HtmlResolverBuilder()
            // Only codename-based resolver, no type-based
            .WithContentResolver("tweet", content =>
                $"<div class=\"codename-fallback\">Codename fallback</div>")
            .Build();

        // Act
        var response = await client.GetItem<Article>(ArticleWithEmbeddedTweetsCodename).ExecuteAsync();
        var html = await response.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.Contains("codename-fallback", html);
    }

    [Fact]
    public async Task RichText_EmbeddedContent_HasCorrectMetadata()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        // Act
        var response = await client.GetItem<Article>(ArticleWithEmbeddedTweetsCodename).ExecuteAsync();
        var tweets = response.Value.Elements.BodyCopy.GetEmbeddedContent<Tweet>().ToList();

        // Assert
        Assert.NotEmpty(tweets);
        foreach (var tweet in tweets)
        {
            Assert.Equal("tweet", tweet.System.Type);
            Assert.NotEmpty(tweet.System.Codename);
            Assert.NotEqual(Guid.Empty, tweet.System.Id);
        }
    }

    [Fact]
    public async Task RichText_WithTupleTypedResolvers_WorksCorrectly()
    {
        // Arrange
        var client = await CreateDeliveryClientAsync("coffee_beverages_explained.json");

        var resolver = new HtmlResolverBuilder()
            .WithContentResolvers(
                (typeof(Tweet), content =>
                    content is IEmbeddedContent<Tweet> tweet
                        ? $"<div class=\"tuple-tweet\">{tweet.Elements.DisplayOptions?.FirstOrDefault()?.Name ?? "Tuple tweet"}</div>"
                        : string.Empty),
                (typeof(HostedVideo), content =>
                    content is IEmbeddedContent<HostedVideo> video
                        ? $"<div class=\"tuple-video\" video-id=\"{video.Elements.VideoId}\">Video</div>"
                        : string.Empty)
            )
            .Build();

        // Act
        var response = await client.GetItem<Article>(ArticleWithEmbeddedTweetsCodename).ExecuteAsync();
        var html = await response.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Assert
        Assert.Contains("tuple-tweet", html);
    }

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

        services.AddSingleton<ITypeProvider, GeneratedTypeProvider>();

        var provider = services.BuildServiceProvider();
        return (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();
    }
}
