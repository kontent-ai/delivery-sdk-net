using System.Net;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests;

public class DeliveryClientTests
{
    private readonly Guid _guid = Guid.NewGuid();
    private string BaseUrl => $"https://deliver.kontent.ai/{_guid}";

    private IDeliveryClient CreateClient(MockHttpMessageHandler mockHttp, DeliveryOptions? options = null)
    {
        var services = new ServiceCollection();
        var opts = options ?? new DeliveryOptions { EnvironmentId = _guid.ToString() };
        services.AddDeliveryClient(opts, configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    [Fact]
    public async Task GetItem_StronglyTyped_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/items/coffee_beverages_explained")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

        var client = CreateClient(mock);

        var result = await client.GetItem<Article>("coffee_beverages_explained").ExecuteAsync();

        // Result metadata
        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(string.IsNullOrEmpty(result.RequestUrl));

        // System info
        Assert.Equal("coffee_beverages_explained", result.Value.System.Codename);
        Assert.Equal("en-US", result.Value.System.Language);
        Assert.Equal("article", result.Value.System.Type);

        var elements = result.Value.Elements;

        // Text elements
        Assert.Equal("Coffee Beverages Explained", elements.Title);
        Assert.False(string.IsNullOrEmpty(elements.Summary));
        Assert.False(string.IsNullOrEmpty(elements.MetaDescription));
        Assert.False(string.IsNullOrEmpty(elements.MetaKeywords));

        // URL slug element
        Assert.Equal("coffee-beverages-explained", elements.UrlPattern);

        // Date/time element
        Assert.NotNull(elements.PostDate);
        Assert.Equal(new DateTime(2014, 11, 18, 0, 0, 0, DateTimeKind.Utc), elements.PostDate.Value);

        // Asset element
        Assert.NotNull(elements.TeaserImage);
        var asset = elements.TeaserImage.First();
        Assert.Equal("coffee-beverages-explained-1080px.jpg", asset.Name);
        Assert.Equal("image/jpeg", asset.Type);
        Assert.Equal(800, asset.Width);
        Assert.Equal(600, asset.Height);
        Assert.False(string.IsNullOrEmpty(asset.Url));

        // Taxonomy element
        Assert.NotNull(elements.Personas);
        var taxonomyTerm = elements.Personas.First();
        Assert.Equal("coffee_lover", taxonomyTerm.Codename);
        Assert.Equal("Coffee lover", taxonomyTerm.Name);

        // Rich text element - RichTextContent is a List<IRichTextBlock>
        Assert.NotNull(elements.BodyCopy);
        Assert.NotEmpty(elements.BodyCopy);

        // Modular content (linked items) element - empty in this fixture
        Assert.NotNull(elements.RelatedArticles);
    }

    [Fact]
    public async Task GetType_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/types/article")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}article.json")));

        var client = CreateClient(mock);
        var result = await client.GetType("article").ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("Article", result.Value.System.Name);
    }

    [Fact]
    public async Task GetTypes_WithSkip_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/types?skip=1")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json")));

        var client = CreateClient(mock);
        var result = await client.GetTypes().Skip(1).ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Types);
    }

    [Fact]
    public async Task GetContentElement_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/types/article/elements/title")
            .Respond("application/json", "{\"type\":\"text\",\"name\":\"Title\",\"codename\":\"title\"}");

        var client = CreateClient(mock);
        var result = await client.GetContentElement("article", "title").ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("title", result.Value.Codename);
    }

    [Fact]
    public async Task GetTaxonomy_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/taxonomies/personas")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json")));

        var client = CreateClient(mock);
        var result = await client.GetTaxonomy("personas").ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("personas", result.Value.System.Codename);
    }

    [Fact]
    public async Task GetTaxonomies_Skip_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/taxonomies?skip=1")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json")));

        var client = CreateClient(mock);
        var result = await client.GetTaxonomies().Skip(1).ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Taxonomies);
    }

    [Fact]
    public async Task GetLanguages_Skip_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/languages?skip=1")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}languages.json")));

        var client = CreateClient(mock);
        var result = await client.GetLanguages().Skip(1).ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Languages);
    }

    [Fact]
    public async Task ItemsFeed_EnumerateItemsAsync_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/items-feed")
            .WithQueryString("system.type%5Beq%5D=article")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed.json")));

        var client = CreateClient(mock);
        var items = new List<IContentItem<Article>>();
        await foreach (var item in client.GetItemsFeed<Article>().Where(f => f.System("type").IsEqualTo("article")).EnumerateItemsAsync())
        {
            items.Add(item);
        }

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task ItemUsedIn_EnumerateItemsAsync_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/items/coffee_beverages_explained/used-in")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}used_in.json")));

        var client = CreateClient(mock);
        var items = new List<IUsedInItem>();
        await foreach (var item in client.GetItemUsedIn("coffee_beverages_explained").EnumerateItemsAsync())
        {
            items.Add(item);
        }

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task AssetUsedIn_EnumerateItemsAsync_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/assets/asset_codename/used-in")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}used_in.json")));

        var client = CreateClient(mock);
        var items = new List<IUsedInItem>();
        await foreach (var item in client.GetAssetUsedIn("asset_codename").EnumerateItemsAsync())
        {
            items.Add(item);
        }

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task PreviewEndpoint_Used_WhenEnabled()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"https://preview-deliver.kontent.ai/{_guid}/items")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

        var client = CreateClient(mock, new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            UsePreviewApi = true,
            PreviewApiKey = "abc.def.ghi"
        });

        var result = await client.GetItems<IDynamicElements>().ExecuteAsync();
        Assert.True(result.IsSuccess);
    }
    [Fact]
    public async Task GetItems_Filter_ComposesQuery()
    {
        var mock = new MockHttpMessageHandler();
        var expectedUrl = $"{BaseUrl}/items?system.type%5Beq%5D=article";
        mock.When(expectedUrl)
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json")));

        var client = CreateClient(mock);

        var result = await client.GetItems<IDynamicElements>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Items);
    }

    [Fact]
    public async Task StaleContentHeader_IsSurfaced()
    {
        var mock = new MockHttpMessageHandler();
        var headers = new[] { new KeyValuePair<string, string>("X-Stale-Content", "1") };
        mock.When($"{BaseUrl}/items/coffee_beverages_explained")
            .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

        var client = CreateClient(mock);
        var result = await client.GetItem<IDynamicElements>("coffee_beverages_explained").ExecuteAsync();

        Assert.True(result.HasStaleContent);
    }

    [Fact]
    public async Task SecureAccess_AddsAuthorizationHeader()
    {
        var mock = new MockHttpMessageHandler();
        var key = "abc.def.ghi";
        mock.Expect($"{BaseUrl}/items")
            .WithHeaders("Authorization", $"Bearer {key}")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

        var client = CreateClient(mock, new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            UseSecureAccess = true,
            SecureAccessApiKey = key
        });

        var result = await client.GetItems<IDynamicElements>().ExecuteAsync();

        Assert.True(result.IsSuccess);
        mock.VerifyNoOutstandingExpectation();
    }
}