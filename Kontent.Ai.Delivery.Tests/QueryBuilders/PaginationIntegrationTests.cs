using System.Net;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryBuilders;

/// <summary>
/// Comprehensive pagination tests to ensure FetchNextPageAsync works correctly
/// and produces identical results to fetching all at once.
/// </summary>
public sealed class PaginationIntegrationTests
{
    #region ItemListing (GetItems) Tests

    [Fact]
    public async Task ItemListing_FetchAllAtOnce_Vs_FetchPageByPage_ProducesIdenticalResults()
    {
        // Arrange: 5 items, fetch all at once vs limit=1 with FetchNextPage
        var allCodenames = new[] { "item_a", "item_b", "item_c", "item_d", "item_e" };

        // Mock for "fetch all" - single request, no pagination
        var envAll = Guid.NewGuid().ToString();
        var mockForAll = new MockHttpMessageHandler();
        mockForAll.When($"https://deliver.kontent.ai/{envAll}/items")
            .Respond("application/json", BuildItemsListingJson(
                skip: 0,
                limit: 100,
                totalCount: 5,
                codenames: allCodenames));

        var clientAll = BuildClient(envAll, mockForAll);

        // Mock for "fetch page by page" - limit=1, using FetchNextPageAsync
        var envPaged = Guid.NewGuid().ToString();
        var itemsUrl = $"https://deliver.kontent.ai/{envPaged}/items";
        var mockForPaged = new MockHttpMessageHandler();

        // Use Expect for ordered assertions
        mockForPaged.Expect(itemsUrl)
            .WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 0, limit: 1, totalCount: 5, codenames: [allCodenames[0]], hasNextPage: true));

        for (var i = 1; i < 5; i++)
        {
            mockForPaged.Expect(itemsUrl)
                .WithQueryString("skip", i.ToString())
                .WithQueryString("limit", "1")
                .Respond("application/json", BuildItemsListingJson(skip: i, limit: 1, totalCount: 5, codenames: [allCodenames[i]], hasNextPage: i < 4));
        }

        var clientPaged = BuildClient(envPaged, mockForPaged);

        // Act: Fetch all at once
        var allResult = await clientAll.GetItems<TestArticle>().ExecuteAsync();
        Assert.True(allResult.IsSuccess, $"Fetch all failed: {allResult.Error?.Message}");
        var allItems = allResult.Value.Items;

        // Act: Fetch page by page
        var pagedItems = new List<IContentItem<TestArticle>>();
        var firstPage = await clientPaged.GetItems<TestArticle>().Limit(1).ExecuteAsync();
        Assert.True(firstPage.IsSuccess, $"First page failed: {firstPage.Error?.Message}");

        pagedItems.AddRange(firstPage.Value.Items);
        var currentPage = firstPage.Value;

        while (currentPage.HasNextPage)
        {
            var nextResult = await currentPage.FetchNextPageAsync();
            Assert.NotNull(nextResult);
            Assert.True(nextResult.IsSuccess, $"Page fetch failed: {nextResult.Error?.Message}");
            pagedItems.AddRange(nextResult.Value.Items);
            currentPage = nextResult.Value;
        }

        // Assert: Both approaches produce identical results
        Assert.Equal(allItems.Count, pagedItems.Count);
        Assert.Equal(
            [..allItems.Select(i => i.System.Codename)],
            [.. pagedItems.Select(i => i.System.Codename)]);

        // Verify elements were deserialized correctly
        for (var i = 0; i < allItems.Count; i++)
        {
            Assert.Equal(allItems[i].Elements.Title, pagedItems[i].Elements.Title);
        }

        mockForPaged.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ItemListing_DynamicQuery_FetchPageByPage_WorksCorrectly()
    {
        var env = Guid.NewGuid().ToString();
        var itemsUrl = $"https://deliver.kontent.ai/{env}/items";
        var allCodenames = new[] { "dyn_1", "dyn_2", "dyn_3", "dyn_4" };

        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(itemsUrl)
            .WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 0, limit: 1, totalCount: 4, codenames: [allCodenames[0]], hasNextPage: true));

        for (var i = 1; i < 4; i++)
        {
            mockHttp.Expect(itemsUrl)
                .WithQueryString("skip", i.ToString())
                .WithQueryString("limit", "1")
                .Respond("application/json", BuildItemsListingJson(skip: i, limit: 1, totalCount: 4, codenames: [allCodenames[i]], hasNextPage: i < 3));
        }

        var client = BuildClient(env, mockHttp);

        var accumulatedItems = new List<IContentItem>();
        var firstPage = await client.GetItems().Limit(1).ExecuteAsync();
        Assert.True(firstPage.IsSuccess);

        accumulatedItems.AddRange(firstPage.Value.Items);
        var currentPage = firstPage.Value;

        while (currentPage.HasNextPage)
        {
            var nextResult = await currentPage.FetchNextPageAsync();
            Assert.NotNull(nextResult);
            Assert.True(nextResult.IsSuccess);
            accumulatedItems.AddRange(nextResult.Value.Items);
            currentPage = nextResult.Value;
        }

        Assert.Equal(4, accumulatedItems.Count);
        Assert.Equal(allCodenames, accumulatedItems.Select(i => i.System.Codename).ToArray());
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ItemListing_SingleItem_HasNextPage_IsFalse()
    {
        var env = Guid.NewGuid().ToString();
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"https://deliver.kontent.ai/{env}/items")
            .Respond("application/json", BuildItemsListingJson(skip: 0, limit: 100, totalCount: 1, codenames: ["only_one"], hasNextPage: false));

        var client = BuildClient(env, mockHttp);
        var result = await client.GetItems<TestArticle>().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.False(result.Value.HasNextPage);
        Assert.Null(await result.Value.FetchNextPageAsync());
    }

    [Fact]
    public async Task ItemListing_EmptyResult_HasNextPage_IsFalse()
    {
        var env = Guid.NewGuid().ToString();
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"https://deliver.kontent.ai/{env}/items")
            .Respond("application/json", BuildItemsListingJson(skip: 0, limit: 100, totalCount: 0, codenames: [], hasNextPage: false));

        var client = BuildClient(env, mockHttp);
        var result = await client.GetItems<TestArticle>().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.False(result.Value.HasNextPage);
        Assert.Null(await result.Value.FetchNextPageAsync());
    }

    [Fact]
    public async Task ItemListing_WithFilters_PreservedAcrossPages()
    {
        var env = Guid.NewGuid().ToString();
        var itemsUrl = $"https://deliver.kontent.ai/{env}/items";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(itemsUrl)
            .WithQueryString("limit", "1")
            .WithQueryString("system.type[eq]", "article")
            .Respond("application/json", BuildItemsListingJson(skip: 0, limit: 1, totalCount: 2, codenames: ["a1"], hasNextPage: true));

        mockHttp.Expect(itemsUrl)
            .WithQueryString("skip", "1")
            .WithQueryString("limit", "1")
            .WithQueryString("system.type[eq]", "article")
            .Respond("application/json", BuildItemsListingJson(skip: 1, limit: 1, totalCount: 2, codenames: ["a2"], hasNextPage: false));

        var client = BuildClient(env, mockHttp);

        var firstPage = await client.GetItems<TestArticle>()
            .Limit(1)
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        Assert.True(firstPage.IsSuccess);
        Assert.True(firstPage.Value.HasNextPage);

        var secondPage = await firstPage.Value.FetchNextPageAsync();
        Assert.NotNull(secondPage);
        Assert.True(secondPage.IsSuccess);
        Assert.False(secondPage.Value.HasNextPage);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    #endregion

    #region ItemsFeed (GetItemsFeed) Tests

    [Fact]
    public async Task ItemsFeed_EnumerateItemsAsync_Vs_FetchPageByPage_ProducesIdenticalResults()
    {
        var allCodenames = new[] { "async_1", "async_2", "async_3" };

        var envAsync = Guid.NewGuid().ToString();
        var feedUrlAsync = $"https://deliver.kontent.ai/{envAsync}/items-feed";
        var mockForAsync = new MockHttpMessageHandler();
        mockForAsync.Expect(feedUrlAsync).Respond(req => CreateFeedResponse([allCodenames[0]], "t1"));
        mockForAsync.Expect(feedUrlAsync).WithHeaders("X-Continuation", "t1").Respond(req => CreateFeedResponse([allCodenames[1]], "t2"));
        mockForAsync.Expect(feedUrlAsync).WithHeaders("X-Continuation", "t2").Respond(req => CreateFeedResponse([allCodenames[2]], null));

        var clientAsync = BuildClient(envAsync, mockForAsync);

        var envManual = Guid.NewGuid().ToString();
        var feedUrlManual = $"https://deliver.kontent.ai/{envManual}/items-feed";
        var mockForManual = new MockHttpMessageHandler();
        mockForManual.Expect(feedUrlManual).Respond(req => CreateFeedResponse([allCodenames[0]], "t1"));
        mockForManual.Expect(feedUrlManual).WithHeaders("X-Continuation", "t1").Respond(req => CreateFeedResponse([allCodenames[1]], "t2"));
        mockForManual.Expect(feedUrlManual).WithHeaders("X-Continuation", "t2").Respond(req => CreateFeedResponse([allCodenames[2]], null));

        var clientManual = BuildClient(envManual, mockForManual);

        var asyncItems = new List<IContentItem<TestArticle>>();
        await foreach (var item in clientAsync.GetItemsFeed<TestArticle>().EnumerateItemsAsync())
            asyncItems.Add(item);

        var manualItems = new List<IContentItem<TestArticle>>();
        var page = await clientManual.GetItemsFeed<TestArticle>().ExecuteAsync();
        Assert.True(page.IsSuccess);
        manualItems.AddRange(page.Value.Items);

        while (page.Value.HasNextPage)
        {
            var next = await page.Value.FetchNextPageAsync();
            Assert.NotNull(next);
            Assert.True(next.IsSuccess);
            manualItems.AddRange(next.Value.Items);
            page = next;
        }

        Assert.Equal(asyncItems.Count, manualItems.Count);
        Assert.Equal(asyncItems.Select(i => i.System.Codename).ToArray(), manualItems.Select(i => i.System.Codename).ToArray());

        mockForAsync.VerifyNoOutstandingExpectation();
        mockForManual.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ItemsFeed_DynamicQuery_FetchPageByPage_WorksCorrectly()
    {
        var env = Guid.NewGuid().ToString();
        var feedUrl = $"https://deliver.kontent.ai/{env}/items-feed";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(feedUrl).Respond(req => CreateFeedResponse(["d1", "d2"], "cont"));
        mockHttp.Expect(feedUrl).WithHeaders("X-Continuation", "cont").Respond(req => CreateFeedResponse(["d3"], null));

        var client = BuildClient(env, mockHttp);

        var items = new List<IContentItem>();
        var page = await client.GetItemsFeed().ExecuteAsync();
        Assert.True(page.IsSuccess);
        items.AddRange(page.Value.Items);

        while (page.Value.HasNextPage)
        {
            var next = await page.Value.FetchNextPageAsync();
            Assert.NotNull(next);
            Assert.True(next.IsSuccess);
            items.AddRange(next.Value.Items);
            page = next;
        }

        Assert.Equal(3, items.Count);
        Assert.Equal(["d1", "d2", "d3"], items.Select(i => i.System.Codename).ToArray());
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ItemsFeed_SinglePage_HasNextPage_IsFalse()
    {
        var env = Guid.NewGuid().ToString();
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"https://deliver.kontent.ai/{env}/items-feed").Respond(req => CreateFeedResponse(["single"], null));

        var client = BuildClient(env, mockHttp);
        var result = await client.GetItemsFeed<TestArticle>().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.False(result.Value.HasNextPage);
        Assert.Null(await result.Value.FetchNextPageAsync());
    }

    [Fact]
    public async Task ItemsFeed_EmptyResult_HasNextPage_IsFalse()
    {
        var env = Guid.NewGuid().ToString();
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"https://deliver.kontent.ai/{env}/items-feed").Respond(req => CreateFeedResponse([], null));

        var client = BuildClient(env, mockHttp);
        var result = await client.GetItemsFeed<TestArticle>().ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.False(result.Value.HasNextPage);
        Assert.Null(await result.Value.FetchNextPageAsync());
    }

    [Fact]
    public async Task ItemsFeed_WithFilters_PreservedAcrossPages()
    {
        var env = Guid.NewGuid().ToString();
        var feedUrl = $"https://deliver.kontent.ai/{env}/items-feed";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(feedUrl).WithQueryString("system.type[eq]", "article").Respond(req => CreateFeedResponse(["f1"], "token"));
        mockHttp.Expect(feedUrl).WithQueryString("system.type[eq]", "article").WithHeaders("X-Continuation", "token").Respond(req => CreateFeedResponse(["f2"], null));

        var client = BuildClient(env, mockHttp);

        var firstPage = await client.GetItemsFeed<TestArticle>().Where(f => f.System("type").IsEqualTo("article")).ExecuteAsync();
        Assert.True(firstPage.IsSuccess);
        Assert.True(firstPage.Value.HasNextPage);

        var secondPage = await firstPage.Value.FetchNextPageAsync();
        Assert.NotNull(secondPage);
        Assert.True(secondPage.IsSuccess);
        Assert.False(secondPage.Value.HasNextPage);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    #endregion

    #region Large Dataset Tests

    [Fact]
    public async Task ItemListing_LargeDataset_100Items_PagedCorrectly()
    {
        var env = Guid.NewGuid().ToString();
        var itemsUrl = $"https://deliver.kontent.ai/{env}/items";
        const int totalItems = 100;
        const int pageSize = 10;
        var allCodenames = Enumerable.Range(0, totalItems).Select(i => $"item_{i:D3}").ToArray();

        var mockHttp = new MockHttpMessageHandler();

        // First page
        mockHttp.Expect(itemsUrl)
            .WithQueryString("limit", pageSize.ToString())
            .Respond("application/json", BuildItemsListingJson(skip: 0, limit: pageSize, totalCount: totalItems, codenames: allCodenames[..pageSize], hasNextPage: true));

        // Subsequent pages
        for (var page = 1; page < totalItems / pageSize; page++)
        {
            var skip = page * pageSize;
            var pageCodenames = allCodenames.Skip(skip).Take(pageSize).ToArray();
            var hasNext = skip + pageSize < totalItems;

            mockHttp.Expect(itemsUrl)
                .WithQueryString("skip", skip.ToString())
                .WithQueryString("limit", pageSize.ToString())
                .Respond("application/json", BuildItemsListingJson(skip: skip, limit: pageSize, totalCount: totalItems, codenames: pageCodenames, hasNextPage: hasNext));
        }

        var client = BuildClient(env, mockHttp);

        var items = new List<IContentItem<TestArticle>>();
        var firstPage = await client.GetItems<TestArticle>().Limit(pageSize).ExecuteAsync();
        Assert.True(firstPage.IsSuccess);
        items.AddRange(firstPage.Value.Items);

        var currentPage = firstPage.Value;
        var pageCount = 1;

        while (currentPage.HasNextPage)
        {
            var next = await currentPage.FetchNextPageAsync();
            Assert.NotNull(next);
            Assert.True(next.IsSuccess);
            items.AddRange(next.Value.Items);
            currentPage = next.Value;
            pageCount++;
        }

        Assert.Equal(totalItems, items.Count);
        Assert.Equal(totalItems / pageSize, pageCount);
        Assert.Equal(allCodenames, items.Select(i => i.System.Codename).ToArray());
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ItemsFeed_LargeDataset_50Items_PagedCorrectly()
    {
        var env = Guid.NewGuid().ToString();
        var feedUrl = $"https://deliver.kontent.ai/{env}/items-feed";
        const int totalItems = 50;
        const int itemsPerPage = 5;
        var allCodenames = Enumerable.Range(0, totalItems).Select(i => $"feed_{i:D2}").ToArray();

        var mockHttp = new MockHttpMessageHandler();

        // First page
        mockHttp.Expect(feedUrl).Respond(req => CreateFeedResponse(allCodenames[..itemsPerPage], "token_1"));

        // Subsequent pages
        for (var page = 1; page < totalItems / itemsPerPage; page++)
        {
            var pageCodenames = allCodenames.Skip(page * itemsPerPage).Take(itemsPerPage).ToArray();
            var token = page < (totalItems / itemsPerPage) - 1 ? $"token_{page + 1}" : null;
            mockHttp.Expect(feedUrl).WithHeaders("X-Continuation", $"token_{page}").Respond(req => CreateFeedResponse(pageCodenames, token));
        }

        var client = BuildClient(env, mockHttp);

        var items = new List<IContentItem<TestArticle>>();
        var firstPage = await client.GetItemsFeed<TestArticle>().ExecuteAsync();
        Assert.True(firstPage.IsSuccess);
        items.AddRange(firstPage.Value.Items);

        var currentPage = firstPage.Value;
        var pageCount = 1;

        while (currentPage.HasNextPage)
        {
            var next = await currentPage.FetchNextPageAsync();
            Assert.NotNull(next);
            Assert.True(next.IsSuccess);
            items.AddRange(next.Value.Items);
            currentPage = next.Value;
            pageCount++;
        }

        Assert.Equal(totalItems, items.Count);
        Assert.Equal(totalItems / itemsPerPage, pageCount);
        Assert.Equal(allCodenames, items.Select(i => i.System.Codename).ToArray());
        mockHttp.VerifyNoOutstandingExpectation();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ItemListing_ErrorOnSecondPage_ReturnsFailure()
    {
        var env = Guid.NewGuid().ToString();
        var itemsUrl = $"https://deliver.kontent.ai/{env}/items";
        var mockHttp = new MockHttpMessageHandler();

        // First request (no skip) returns success with next page
        mockHttp.When(itemsUrl)
            .With(req => !req.RequestUri!.Query.Contains("skip="))
            .Respond("application/json", BuildItemsListingJson(skip: 0, limit: 1, totalCount: 3, codenames: ["a"], hasNextPage: true));

        // Second request (with skip) returns error
        mockHttp.When(itemsUrl)
            .With(req => req.RequestUri!.Query.Contains("skip="))
            .Respond(HttpStatusCode.InternalServerError, "text/plain", "Server error");

        var client = BuildClient(env, mockHttp);

        var firstPage = await client.GetItems<TestArticle>().Limit(1).ExecuteAsync();
        Assert.True(firstPage.IsSuccess);
        Assert.True(firstPage.Value.HasNextPage);

        var secondPage = await firstPage.Value.FetchNextPageAsync();
        Assert.NotNull(secondPage);
        Assert.False(secondPage.IsSuccess);
        Assert.Equal(HttpStatusCode.InternalServerError, secondPage.StatusCode);
    }

    [Fact]
    public async Task ItemsFeed_ErrorOnSecondPage_ReturnsFailure()
    {
        var env = Guid.NewGuid().ToString();
        var feedUrl = $"https://deliver.kontent.ai/{env}/items-feed";
        var mockHttp = new MockHttpMessageHandler();

        // First request (no continuation header) returns success with continuation token
        mockHttp.When(feedUrl)
            .With(req => !req.Headers.Contains("X-Continuation"))
            .Respond(req => CreateFeedResponse(["a"], "token"));

        // Second request (with continuation header) returns error
        mockHttp.When(feedUrl)
            .With(req => req.Headers.Contains("X-Continuation"))
            .Respond(HttpStatusCode.ServiceUnavailable);

        var client = BuildClient(env, mockHttp);

        var firstPage = await client.GetItemsFeed<TestArticle>().ExecuteAsync();
        Assert.True(firstPage.IsSuccess);
        Assert.True(firstPage.Value.HasNextPage);

        var secondPage = await firstPage.Value.FetchNextPageAsync();
        Assert.NotNull(secondPage);
        Assert.False(secondPage.IsSuccess);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, secondPage.StatusCode);
    }

    #endregion

    #region Types Pagination Tests

    [Fact]
    public async Task TypesListing_FetchPageByPage_WorksCorrectly()
    {
        var env = Guid.NewGuid().ToString();
        var typesUrl = $"https://deliver.kontent.ai/{env}/types";
        var allCodenames = new[] { "article", "author", "category" };

        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(typesUrl)
            .WithQueryString("limit", "1")
            .Respond("application/json", BuildTypesListingJson(skip: 0, limit: 1, totalCount: 3, codenames: [allCodenames[0]], hasNextPage: true));

        for (var i = 1; i < 3; i++)
        {
            mockHttp.Expect(typesUrl)
                .WithQueryString("skip", i.ToString())
                .WithQueryString("limit", "1")
                .Respond("application/json", BuildTypesListingJson(skip: i, limit: 1, totalCount: 3, codenames: [allCodenames[i]], hasNextPage: i < 2));
        }

        var client = BuildClient(env, mockHttp);

        var types = new List<IContentType>();
        var firstPage = await client.GetTypes().Limit(1).ExecuteAsync();
        Assert.True(firstPage.IsSuccess);
        types.AddRange(firstPage.Value.Types);

        var currentPage = firstPage.Value;
        while (currentPage.HasNextPage)
        {
            var next = await currentPage.FetchNextPageAsync();
            Assert.NotNull(next);
            Assert.True(next.IsSuccess);
            types.AddRange(next.Value.Types);
            currentPage = next.Value;
        }

        Assert.Equal(3, types.Count);
        Assert.Equal(allCodenames, types.Select(t => t.System.Codename).ToArray());
        mockHttp.VerifyNoOutstandingExpectation();
    }

    #endregion

    #region Taxonomies Pagination Tests

    [Fact]
    public async Task TaxonomiesListing_FetchPageByPage_WorksCorrectly()
    {
        var env = Guid.NewGuid().ToString();
        var taxonomiesUrl = $"https://deliver.kontent.ai/{env}/taxonomies";
        var allCodenames = new[] { "personas", "categories", "tags" };

        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(taxonomiesUrl)
            .WithQueryString("limit", "1")
            .Respond("application/json", BuildTaxonomiesListingJson(skip: 0, limit: 1, totalCount: 3, codenames: [allCodenames[0]], hasNextPage: true));

        for (var i = 1; i < 3; i++)
        {
            mockHttp.Expect(taxonomiesUrl)
                .WithQueryString("skip", i.ToString())
                .WithQueryString("limit", "1")
                .Respond("application/json", BuildTaxonomiesListingJson(skip: i, limit: 1, totalCount: 3, codenames: [allCodenames[i]], hasNextPage: i < 2));
        }

        var client = BuildClient(env, mockHttp);

        var taxonomies = new List<ITaxonomyGroup>();
        var firstPage = await client.GetTaxonomies().Limit(1).ExecuteAsync();
        Assert.True(firstPage.IsSuccess);
        taxonomies.AddRange(firstPage.Value.Taxonomies);

        var currentPage = firstPage.Value;
        while (currentPage.HasNextPage)
        {
            var next = await currentPage.FetchNextPageAsync();
            Assert.NotNull(next);
            Assert.True(next.IsSuccess);
            taxonomies.AddRange(next.Value.Taxonomies);
            currentPage = next.Value;
        }

        Assert.Equal(3, taxonomies.Count);
        Assert.Equal(allCodenames, taxonomies.Select(t => t.System.Codename).ToArray());
        mockHttp.VerifyNoOutstandingExpectation();
    }

    #endregion

    #region Languages Pagination Tests

    [Fact]
    public async Task LanguagesListing_FetchPageByPage_WorksCorrectly()
    {
        var env = Guid.NewGuid().ToString();
        var languagesUrl = $"https://deliver.kontent.ai/{env}/languages";
        var allCodenames = new[] { "en-US", "es-ES", "de-DE" };

        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(languagesUrl)
            .WithQueryString("limit", "1")
            .Respond("application/json", BuildLanguagesListingJson(skip: 0, limit: 1, totalCount: 3, codenames: [allCodenames[0]], hasNextPage: true));

        for (var i = 1; i < 3; i++)
        {
            mockHttp.Expect(languagesUrl)
                .WithQueryString("skip", i.ToString())
                .WithQueryString("limit", "1")
                .Respond("application/json", BuildLanguagesListingJson(skip: i, limit: 1, totalCount: 3, codenames: [allCodenames[i]], hasNextPage: i < 2));
        }

        var client = BuildClient(env, mockHttp);

        var languages = new List<ILanguage>();
        var firstPage = await client.GetLanguages().Limit(1).ExecuteAsync();
        Assert.True(firstPage.IsSuccess);
        languages.AddRange(firstPage.Value.Languages);

        var currentPage = firstPage.Value;
        while (currentPage.HasNextPage)
        {
            var next = await currentPage.FetchNextPageAsync();
            Assert.NotNull(next);
            Assert.True(next.IsSuccess);
            languages.AddRange(next.Value.Languages);
            currentPage = next.Value;
        }

        Assert.Equal(3, languages.Count);
        Assert.Equal(allCodenames, languages.Select(l => l.System.Codename).ToArray());
        mockHttp.VerifyNoOutstandingExpectation();
    }

    #endregion

    #region Helpers

    private static IDeliveryClient BuildClient(string env, MockHttpMessageHandler mockHttp)
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = env },
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    private static string BuildItemsListingJson(int skip, int limit, int totalCount, IReadOnlyList<string> codenames, bool hasNextPage = false)
    {
        var itemsJson = string.Join(",", codenames.Select(codename =>
            $"{{\"system\": {{\"id\": \"{Guid.NewGuid()}\", \"name\": \"{codename}\", \"codename\": \"{codename}\", \"language\": \"default\", \"type\": \"article\", \"collection\": \"default\", \"last_modified\": \"2024-01-01T00:00:00Z\"}}, \"elements\": {{\"title\": {{\"type\": \"text\", \"name\": \"Title\", \"value\": \"Title for {codename}\"}}}}}}"
        ));

        var nextPageUrl = hasNextPage ? $"https://deliver.kontent.ai/items?skip={skip + limit}&limit={limit}" : "";

        return $"{{\"items\": [{itemsJson}], \"pagination\": {{\"skip\": {skip}, \"limit\": {limit}, \"count\": {codenames.Count}, \"total_count\": {totalCount}, \"next_page\": \"{nextPageUrl}\"}}, \"modular_content\": {{}}}}";
    }

    private static HttpResponseMessage CreateFeedResponse(IReadOnlyList<string> codenames, string? continuationToken)
    {
        var itemsJson = string.Join(",", codenames.Select(codename =>
            $"{{\"system\": {{\"id\": \"{Guid.NewGuid()}\", \"name\": \"{codename}\", \"codename\": \"{codename}\", \"language\": \"default\", \"type\": \"article\", \"collection\": \"default\", \"last_modified\": \"2024-01-01T00:00:00Z\"}}, \"elements\": {{\"title\": {{\"type\": \"text\", \"name\": \"Title\", \"value\": \"Title for {codename}\"}}}}}}"
        ));

        var json = $"{{\"items\": [{itemsJson}], \"modular_content\": {{}}}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
        if (!string.IsNullOrEmpty(continuationToken))
            response.Headers.Add("X-Continuation", continuationToken);
        return response;
    }

    private static string BuildTypesListingJson(int skip, int limit, int totalCount, IReadOnlyList<string> codenames, bool hasNextPage = false)
    {
        var typesJson = string.Join(",", codenames.Select(codename =>
            $"{{\"system\": {{\"id\": \"{Guid.NewGuid()}\", \"name\": \"{codename}\", \"codename\": \"{codename}\", \"last_modified\": \"2024-01-01T00:00:00Z\"}}, \"elements\": {{}}}}"
        ));

        var nextPageUrl = hasNextPage ? $"https://deliver.kontent.ai/types?skip={skip + limit}&limit={limit}" : "";

        return $"{{\"types\": [{typesJson}], \"pagination\": {{\"skip\": {skip}, \"limit\": {limit}, \"count\": {codenames.Count}, \"total_count\": {totalCount}, \"next_page\": \"{nextPageUrl}\"}}}}";
    }

    private static string BuildTaxonomiesListingJson(int skip, int limit, int totalCount, IReadOnlyList<string> codenames, bool hasNextPage = false)
    {
        var taxonomiesJson = string.Join(",", codenames.Select(codename =>
            $"{{\"system\": {{\"id\": \"{Guid.NewGuid()}\", \"name\": \"{codename}\", \"codename\": \"{codename}\", \"last_modified\": \"2024-01-01T00:00:00Z\"}}, \"terms\": []}}"
        ));

        var nextPageUrl = hasNextPage ? $"https://deliver.kontent.ai/taxonomies?skip={skip + limit}&limit={limit}" : "";

        return $"{{\"taxonomies\": [{taxonomiesJson}], \"pagination\": {{\"skip\": {skip}, \"limit\": {limit}, \"count\": {codenames.Count}, \"total_count\": {totalCount}, \"next_page\": \"{nextPageUrl}\"}}}}";
    }

    private static string BuildLanguagesListingJson(int skip, int limit, int totalCount, IReadOnlyList<string> codenames, bool hasNextPage = false)
    {
        var languagesJson = string.Join(",", codenames.Select(codename =>
            $"{{\"system\": {{\"id\": \"{Guid.NewGuid()}\", \"name\": \"{codename}\", \"codename\": \"{codename}\"}}}}"
        ));

        var nextPageUrl = hasNextPage ? $"https://deliver.kontent.ai/languages?skip={skip + limit}&limit={limit}" : "";

        return $"{{\"languages\": [{languagesJson}], \"pagination\": {{\"skip\": {skip}, \"limit\": {limit}, \"count\": {codenames.Count}, \"total_count\": {totalCount}, \"next_page\": \"{nextPageUrl}\"}}}}";
    }

    private sealed record TestArticle
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }
    }

    #endregion
}
