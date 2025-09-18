using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FakeItEasy;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.Rx.Tests.Models.ContentTypes;
using Kontent.Ai.Urls.Delivery.QueryParameters;
using Kontent.Ai.Urls.Delivery.QueryParameters.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Rx.Tests;

public class DeliveryObservableProxyTests
{
    private const string BEVERAGES_IDENTIFIER = "coffee_beverages_explained";
    private const string ASSET_CODENAME = "asset_codename";
    readonly string _guid;
    readonly string _baseUrl;
    readonly MockHttpMessageHandler _mockHttp;

    public DeliveryObservableProxyTests()
    {
        _guid = Guid.NewGuid().ToString();
        _baseUrl = $"https://deliver.kontent.ai/{_guid}";
        _mockHttp = new MockHttpMessageHandler();
    }

    [Fact]
    public async Task TypedItemRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockItem)).GetItemObservable<Article>(BEVERAGES_IDENTIFIER, q => q.WithLanguage("es-ES"));
        var item = await observable.FirstOrDefaultAsync();

        Assert.NotNull(item);
        AssertArticlePropertiesNotNull(item);
    }

    // TODO: Add when runtime provision works.
    // [Fact]
    // public async Task RuntimeTypedItemRetrieved()
    // {
    //     var observable = new DeliveryObservableProxy(GetDeliveryClient(MockItem)).GetItemObservable<DynamicElements>(BEVERAGES_IDENTIFIER, q => q.WithLanguage("es-ES"));
    //     var item = await observable.FirstOrDefaultAsync();

    //     Assert.IsType<Article>(item);
    //     Assert.NotNull(item);
    //     AssertArticlePropertiesNotNull((Article)item);
    // }

    [Fact]
    public void TypedItemsRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockArticles)).GetItemsObservable<Article>(q => q.Filter(f => f.Contains(Elements.GetPath("personas"), "barista")));
        var items = observable.ToEnumerable().ToList();

        Assert.NotEmpty(items);
        Assert.Equal(6, items.Count);
        Assert.All(items, AssertArticlePropertiesNotNull);
    }

    [Fact]
    public void TypedItemsFeedRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockFeedArticles)).GetItemsFeedObservable<Article>(q => q.Filter(f => f.Contains(Elements.GetPath("personas"), "barista")));
        var items = observable.ToEnumerable().ToList();

        Assert.NotEmpty(items);
        Assert.Equal(6, items.Count);
        Assert.All(items, AssertArticlePropertiesNotNull);
    }

    [Fact]
    public void RuntimeTypedItemsRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockArticles)).GetItemsObservable<Article>(q => q.Filter(f => f.Contains(Elements.GetPath("personas"), "barista")));
        var articles = observable.ToEnumerable().ToList();

        Assert.NotEmpty(articles);
        Assert.All(articles, article => Assert.IsType<Article>(article));
        Assert.All(articles, AssertArticlePropertiesNotNull);
    }

    [Fact]
    public void RuntimeTypedItemsFeedRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockFeedArticles)).GetItemsFeedObservable<Article>(q => q.Filter(f => f.Contains(Elements.GetPath("personas"), "barista")));
        var articles = observable.ToEnumerable().ToList();

        Assert.NotEmpty(articles);
        Assert.All(articles, article => Assert.IsType<Article>(article));
        Assert.All(articles, AssertArticlePropertiesNotNull);
    }

    [Fact]
    public async Task TypeRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockType)).GetTypeObservable("article");
        var type = await observable.FirstOrDefaultAsync();

        Assert.Single(observable.ToEnumerable());
        Assert.NotNull(type.System);
        Assert.NotEmpty(type.Elements);
    }

    [Fact]
    public void TypesRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockTypes)).GetTypesObservable(q => q.Skip(2));
        var types = observable.ToEnumerable().ToList();

        Assert.NotEmpty(types);
        Assert.All(types, Assert.NotNull);
        Assert.All(types, type => Assert.NotEmpty(type.Elements));
    }

    [Fact]
    public async Task ElementRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockElement)).GetElementObservable("article", "title");
        var element = await observable.FirstOrDefaultAsync();

        Assert.NotNull(element);
        Assert.NotNull(element.Codename);
        Assert.NotNull(element.Name);
        Assert.NotNull(element.Type);
    }

    [Fact]
    public async Task TaxonomyElementRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockTaxonomyElement)).GetElementObservable("coffee", "processing");
        var element = await observable.FirstOrDefaultAsync();

        Assert.IsAssignableFrom<ITaxonomyElement>(element);
    }

    [Fact]
    public async Task MultipleChoiceElementRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockMultipleChoiceElement)).GetElementObservable("tweet", "theme");
        var element = await observable.FirstOrDefaultAsync();

        Assert.IsAssignableFrom<IMultipleChoiceElement>(element);
    }

    [Fact]
    public async Task TaxonomyRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockTaxonomy)).GetTaxonomyObservable("personas");
        var taxonomy = await observable.FirstOrDefaultAsync();

        Assert.NotNull(taxonomy);
        Assert.NotNull(taxonomy.System);
        Assert.NotNull(taxonomy.Terms);
    }

    [Fact]
    public void TaxonomiesRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockTaxonomies)).GetTaxonomiesObservable(q => q.Skip(1));
        var taxonomies = observable.ToEnumerable().ToList();

        Assert.NotEmpty(taxonomies);
        Assert.All(taxonomies, taxonomy => Assert.NotNull(taxonomy.System));
        Assert.All(taxonomies, taxonomy => Assert.NotNull(taxonomy.Terms));
    }

    [Fact]
    public void LanguagesRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockLanguages)).GetLanguagesObservable(q => q.Skip(1));
        var languages = observable.ToEnumerable().ToList();

        Assert.NotEmpty(languages);
        Assert.All(languages, language => Assert.NotNull(language.System));
    }

    [Fact]
    public void ItemUsedInRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockItemUsedIn)).GetItemUsedInObservable("article");
        var parents = observable.ToEnumerable().ToList();

        Assert.NotEmpty(parents);
        Assert.All(parents, item => Assert.NotNull(item.System));
    }

    [Fact]
    public void AssetUsedInRetrieved()
    {
        var observable = new DeliveryObservableProxy(GetDeliveryClient(MockAssetUsedIn)).GetAssetUsedInObservable(ASSET_CODENAME);
        var parents = observable.ToEnumerable().ToList();

        Assert.NotEmpty(parents);
        Assert.All(parents, item => Assert.NotNull(item.System));
    }

    public static IOptionsMonitor<DeliveryOptions> CreateMonitor(DeliveryOptions options)
    {
        var mock = A.Fake<IOptionsMonitor<DeliveryOptions>>();
        A.CallTo(() => mock.CurrentValue).Returns(options);
        return mock;
    }

    private IDeliveryClient GetDeliveryClient(Action mockAction)
    {
        mockAction();

        var services = new ServiceCollection();

        // Register the Delivery client configured to use the mock HTTP handler
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = _guid, EnableResilience = false },
            configureRefit: null,
            configureHttpClient: builder => builder.ConfigurePrimaryHttpMessageHandler(() => _mockHttp));

        // Provide minimal type mapping needed for post-processing linked items
        var typeProvider = A.Fake<ITypeProvider>();
        A.CallTo(() => typeProvider.GetType("article")).Returns(typeof(Article));
        A.CallTo(() => typeProvider.GetCodename(typeof(Article))).Returns("article");
        services.AddSingleton(typeProvider);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IDeliveryClient>();
    }

    private void MockItem()
    {
        _mockHttp.When($"{_baseUrl}/items/{BEVERAGES_IDENTIFIER}?language=es-ES")
            .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}coffee_beverages_explained.json")));
    }

    private void MockArticles()
    {
        _mockHttp.When($"{_baseUrl}/items")
            .WithQueryString(new[] { new KeyValuePair<string, string>("system.type", "article"), new KeyValuePair<string, string>("elements.personas[contains]", "barista") })
            .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}articles.json")));
    }

    private void MockFeedArticles()
    {
        _mockHttp.When($"{_baseUrl}/items-feed")
            .WithQueryString(new[] { new KeyValuePair<string, string>("system.type", "article"), new KeyValuePair<string, string>("elements.personas[contains]", "barista") })
            .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}articles.json")));
    }

    private void MockType()
    {
        _mockHttp.When($"{_baseUrl}/types/article")
            .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}article-type.json")));
    }

    private void MockTypes()
    {
        _mockHttp.When($"{_baseUrl}/types?skip=2")
            .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}types.json")));
    }

    private void MockElement()
    {
        _mockHttp.When($"{_baseUrl}/types/article/elements/title")
            .Respond("application/json", "{'type':'text','name':'Title','codename':'title'}");
    }
    private void MockTaxonomyElement()
    {
        _mockHttp
               .When($"{_baseUrl}/types/coffee/elements/processing")
               .Respond("application/json", "{'type':'taxonomy','name':'Processing','taxonomy_group':'processing','codename':'processing'}");
    }

    private void MockMultipleChoiceElement()
    {
        _mockHttp
            .When($"{_baseUrl}/types/tweet/elements/theme")
            .Respond("application/json", "{ 'type': 'multiple_choice', 'name': 'Theme', 'options': [ { 'name': 'Dark', 'codename': 'dark' }, { 'name': 'Light', 'codename': 'light' } ], 'codename': 'theme' }");
    }

    private void MockTaxonomy()
    {
        _mockHttp.When($"{_baseUrl}/taxonomies/personas")
            .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}taxonomies_personas.json")));
    }

    private void MockTaxonomies()
    {
        _mockHttp.When($"{_baseUrl}/taxonomies")
            .WithQueryString("skip=1")
            .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}taxonomies_multiple.json")));
    }

    private void MockLanguages()
    {
        _mockHttp.When($"{_baseUrl}/languages")
            .WithQueryString("skip=1")
            .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}languages.json")));
    }

    private void MockAssetUsedIn()
    {
        _mockHttp.When($"{_baseUrl}/assets/{ASSET_CODENAME}/used-in")
            .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}used_in.json")));
    }

    private void MockItemUsedIn()
    {
        _mockHttp.When($"{_baseUrl}/items/article/used-in")
            .Respond("application/json", File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}used_in.json")));
    }

    private static void AssertArticlePropertiesNotNull(Article item)
    {
        Assert.NotNull(item.Personas);
        Assert.NotNull(item.Title);
        Assert.NotNull(item.TeaserImage);
        Assert.NotNull(item.PostDate);
        Assert.NotNull(item.Summary);
        Assert.NotNull(item.BodyCopy);
        Assert.NotNull(item.RelatedArticles);
        Assert.NotNull(item.MetaKeywords);
        Assert.NotNull(item.MetaDescription);
        Assert.NotNull(item.UrlPattern);
    }
}
