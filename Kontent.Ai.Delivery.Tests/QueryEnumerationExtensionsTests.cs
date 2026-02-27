using System.Net;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;
using Xunit;

namespace Kontent.Ai.Delivery.Tests;

public class QueryEnumerationExtensionsTests
{
    #region ThrowUnsupportedStatusEnumeration

    [Fact]
    public void ItemUsedIn_EnumerateItemsWithStatusAsync_ThrowsForNonSdkQuery()
    {
        var stub = new StubItemUsedInQuery();

        Assert.Throws<NotSupportedException>(() => stub.EnumerateItemsWithStatusAsync());
    }

    [Fact]
    public void AssetUsedIn_EnumerateItemsWithStatusAsync_ThrowsForNonSdkQuery()
    {
        var stub = new StubAssetUsedInQuery();

        Assert.Throws<NotSupportedException>(() => stub.EnumerateItemsWithStatusAsync());
    }

    #endregion

    #region EnumerateFeedPagesWithStatusAsync — edge cases

    [Fact]
    public async Task TypedFeed_EnumerateWithStatus_FirstPageFails_YieldsSingleFailedResult()
    {
        var query = new StubEnumerateItemsQuery<object>(
            _ => Task.FromResult(CreateFailedFeedResult<IDeliveryItemsFeedResponse<object>>()));

        var pages = await CollectAsync(query.EnumerateItemsWithStatusAsync());

        Assert.Single(pages);
        Assert.False(pages[0].IsSuccess);
    }

    [Fact]
    public async Task DynamicFeed_EnumerateWithStatus_FirstPageFails_YieldsSingleFailedResult()
    {
        var query = new StubDynamicEnumerateItemsQuery(
            _ => Task.FromResult(CreateFailedFeedResult<IDeliveryItemsFeedResponse>()));

        var pages = await CollectAsync(query.EnumerateItemsWithStatusAsync());

        Assert.Single(pages);
        Assert.False(pages[0].IsSuccess);
    }

    [Fact]
    public async Task TypedFeed_EnumerateWithStatus_SinglePage_StopsAfterFirstPage()
    {
        var feedPage = new StubFeedResponse<object>(hasNextPage: false);
        var query = new StubEnumerateItemsQuery<object>(
            _ => Task.FromResult(CreateSuccessFeedResult<IDeliveryItemsFeedResponse<object>>(feedPage)));

        var pages = await CollectAsync(query.EnumerateItemsWithStatusAsync());

        Assert.Single(pages);
        Assert.True(pages[0].IsSuccess);
    }

    [Fact]
    public async Task TypedFeed_EnumerateWithStatus_FetchNextPageReturnsNull_StopsEnumeration()
    {
        var feedPage = new StubFeedResponse<object>(hasNextPage: true, fetchNextPage: _ => Task.FromResult<IDeliveryResult<IDeliveryItemsFeedResponse<object>>?>(null));
        var query = new StubEnumerateItemsQuery<object>(
            _ => Task.FromResult(CreateSuccessFeedResult<IDeliveryItemsFeedResponse<object>>(feedPage)));

        var pages = await CollectAsync(query.EnumerateItemsWithStatusAsync());

        Assert.Single(pages);
        Assert.True(pages[0].IsSuccess);
    }

    #endregion

    #region Helpers

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
            list.Add(item);
        return list;
    }

    private static IDeliveryResult<T> CreateFailedFeedResult<T>()
        => new StubDeliveryResult<T>(default!, isSuccess: false);

    private static IDeliveryResult<T> CreateSuccessFeedResult<T>(T value)
        => new StubDeliveryResult<T>(value, isSuccess: true);

    #endregion

    #region Stubs

    /// <summary>Stub that does NOT implement IUsedInQueryStatusProvider.</summary>
    private sealed class StubItemUsedInQuery : IItemUsedInQuery
    {
        public IItemUsedInQuery WaitForLoadingNewContent(bool enabled = true) => this;
        public IItemUsedInQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build) => this;
        public IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    /// <summary>Stub that does NOT implement IUsedInQueryStatusProvider.</summary>
    private sealed class StubAssetUsedInQuery : IAssetUsedInQuery
    {
        public IAssetUsedInQuery WaitForLoadingNewContent(bool enabled = true) => this;
        public IAssetUsedInQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build) => this;
        public IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class StubEnumerateItemsQuery<TModel>(
        Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>>> executeAsync)
        : IEnumerateItemsQuery<TModel>
    {
        public Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
            => executeAsync(cancellationToken);

        public IEnumerateItemsQuery<TModel> WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled) => this;
        public IEnumerateItemsQuery<TModel> WithElements(params string[] elementCodenames) => this;
        public IEnumerateItemsQuery<TModel> OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending) => this;
        public IEnumerateItemsQuery<TModel> WaitForLoadingNewContent(bool enabled = true) => this;
        public IEnumerateItemsQuery<TModel> Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build) => this;
        public IAsyncEnumerable<IContentItem<TModel>> EnumerateItemsAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class StubDynamicEnumerateItemsQuery(
        Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemsFeedResponse>>> executeAsync)
        : IDynamicEnumerateItemsQuery
    {
        public Task<IDeliveryResult<IDeliveryItemsFeedResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
            => executeAsync(cancellationToken);

        public IDynamicEnumerateItemsQuery WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled) => this;
        public IDynamicEnumerateItemsQuery WithElements(params string[] elementCodenames) => this;
        public IDynamicEnumerateItemsQuery OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending) => this;
        public IDynamicEnumerateItemsQuery WaitForLoadingNewContent(bool enabled = true) => this;
        public IDynamicEnumerateItemsQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build) => this;
        public IAsyncEnumerable<IContentItem> EnumerateItemsAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class StubFeedResponse<TModel>(
        bool hasNextPage,
        Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>?>>? fetchNextPage = null)
        : IDeliveryItemsFeedResponse<TModel>
    {
        public IReadOnlyList<IContentItem<TModel>> Items => [];
        public bool HasNextPage => hasNextPage;
        public IReadOnlyDictionary<string, System.Text.Json.JsonElement> ModularContent => new Dictionary<string, System.Text.Json.JsonElement>();
        public Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>?> FetchNextPageAsync(CancellationToken cancellationToken = default)
            => fetchNextPage?.Invoke(cancellationToken) ?? Task.FromResult<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>?>(null);
    }

    private sealed class StubDeliveryResult<T>(T value, bool isSuccess) : IDeliveryResult<T>
    {
        public T Value => value;
        public bool IsSuccess => isSuccess;
        public IError? Error => null;
        public HttpStatusCode StatusCode => isSuccess ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
        public bool HasStaleContent => false;
        public string? ContinuationToken => null;
        public string? RequestUrl => null;
        public System.Net.Http.Headers.HttpResponseHeaders? ResponseHeaders => null;
        public ResponseSource ResponseSource => ResponseSource.Origin;
        public bool IsCacheHit => false;
    }

    #endregion
}
