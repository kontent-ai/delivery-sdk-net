using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;
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

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.False(string.IsNullOrEmpty(result.RequestUrl));
        Assert.False(string.IsNullOrEmpty(result.Value.Elements.Title));
        Assert.NotNull(result.Value.Elements.TeaserImage); // TODO: fix this and extend tests to all other elements
        Assert.NotNull(result.Value.Elements.Personas);
        Assert.True(result.Value.Elements.Personas.Any());
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
        Assert.NotEmpty(result.Value);
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
        Assert.NotEmpty(result.Value);
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
        Assert.NotEmpty(result.Value);
    }

    [Fact]
    public async Task ItemsFeed_EnumerateAll_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/items-feed")
            .WithQueryString("system.type%5Beq%5D=article")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed.json")));

        var client = CreateClient(mock);
        var items = await client.GetItemsFeed<Article>().Filter(f => f.Equals(ItemSystemPath.Type, "article")).EnumerateAllAsync();

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task ItemUsedIn_EnumerateAll_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/items/coffee_beverages_explained/used-in")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}used_in.json")));

        var client = CreateClient(mock);
        var used = await client.GetItemUsedIn("coffee_beverages_explained").EnumerateAllAsync();

        Assert.NotEmpty(used);
    }

    [Fact]
    public async Task AssetUsedIn_EnumerateAll_Succeeds()
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/assets/asset_codename/used-in")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}used_in.json")));

        var client = CreateClient(mock);
        var used = await client.GetAssetUsedIn("asset_codename").EnumerateAllAsync();

        Assert.NotEmpty(used);
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

        var result = await client.GetItems<IElementsModel>().ExecuteAsync();
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

        var filter = new Filter(ItemSystemPath.Type, FilterOperator.Equals, StringValue.From("article"));
        var result = await client.GetItems<IElementsModel>()
            .Where(filter)
            .ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
    }

    [Fact]
    public async Task StaleContentHeader_IsSurfaced()
    {
        var mock = new MockHttpMessageHandler();
        var headers = new[] { new System.Collections.Generic.KeyValuePair<string, string>("X-Stale-Content", "1") };
        mock.When($"{BaseUrl}/items/coffee_beverages_explained")
            .Respond(headers, "application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));

        var client = CreateClient(mock);
        var result = await client.GetItem<IElementsModel>("coffee_beverages_explained").ExecuteAsync();

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

        var result = await client.GetItems<IElementsModel>().ExecuteAsync();

        Assert.True(result.IsSuccess);
        mock.VerifyNoOutstandingExpectation();
    }
}