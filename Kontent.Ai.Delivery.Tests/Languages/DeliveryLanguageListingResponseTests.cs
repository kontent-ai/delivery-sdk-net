using System.Net;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Languages;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Languages;

public class DeliveryLanguageListingResponseTests
{
    [Fact]
    public void HasNextPage_WithNonEmptyNextPageUrl_ReturnsTrue()
    {
        var sut = CreateResponse(nextPageUrl: "https://deliver.kontent.ai/languages?skip=1&limit=1");

        Assert.True(sut.HasNextPage);
    }

    [Fact]
    public void HasNextPage_WithEmptyNextPageUrl_ReturnsFalse()
    {
        var sut = CreateResponse(nextPageUrl: "");

        Assert.False(sut.HasNextPage);
    }

    [Fact]
    public void Pagination_AccessedViaIPageable_ReturnsSameInstance()
    {
        var sut = CreateResponse();

        var pagination = ((IPageable)sut).Pagination;

        Assert.Same(sut.Pagination, pagination);
    }

    [Fact]
    public async Task FetchNextPageAsync_WhenHasNextPageIsFalse_ReturnsNull()
    {
        var sut = CreateResponse(
            nextPageUrl: "",
            nextPageFetcher: _ => throw new InvalidOperationException("Should not be called"));

        var result = await sut.FetchNextPageAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchNextPageAsync_WhenNextPageFetcherIsNull_ReturnsNull()
    {
        var sut = CreateResponse(
            nextPageUrl: "https://deliver.kontent.ai/languages?skip=1",
            nextPageFetcher: null);

        var result = await sut.FetchNextPageAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchNextPageAsync_WhenHasNextPageAndFetcherSet_ReturnsFetcherResult()
    {
        var expected = CreateSuccessResult(CreateResponse());

        var sut = CreateResponse(
            nextPageUrl: "https://deliver.kontent.ai/languages?skip=1",
            nextPageFetcher: _ => Task.FromResult(expected));

        var result = await sut.FetchNextPageAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task FetchNextPageAsync_PassesCancellationTokenToFetcher()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken captured = default;

        var sut = CreateResponse(
            nextPageUrl: "https://deliver.kontent.ai/languages?skip=1",
            nextPageFetcher: ct =>
            {
                captured = ct;
                return Task.FromResult(CreateSuccessResult(CreateResponse()));
            });

        await sut.FetchNextPageAsync(cts.Token);

        Assert.Equal(cts.Token, captured);
    }

    private static DeliveryLanguageListingResponse CreateResponse(
        string nextPageUrl = "",
        Func<CancellationToken, Task<IDeliveryResult<IDeliveryLanguageListingResponse>>>? nextPageFetcher = null)
        => new()
        {
            Languages = [],
            Pagination = new Pagination
            {
                Skip = 0,
                Limit = 10,
                Count = 0,
                NextPageUrl = nextPageUrl
            },
            NextPageFetcher = nextPageFetcher
        };

    private static IDeliveryResult<IDeliveryLanguageListingResponse> CreateSuccessResult(
        IDeliveryLanguageListingResponse value)
        => DeliveryResult.Success(value, "https://test", HttpStatusCode.OK, false, null, null, ResponseSource.Origin);
}
