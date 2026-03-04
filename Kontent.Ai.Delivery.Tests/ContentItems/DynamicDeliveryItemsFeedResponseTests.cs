using System.Net;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.ContentItems;

public class DynamicDeliveryItemsFeedResponseTests
{
    [Fact]
    public void HasNextPage_WithContinuationToken_ReturnsTrue()
    {
        var sut = CreateResponse(continuationToken: "next-token");

        Assert.True(sut.HasNextPage);
    }

    [Fact]
    public void HasNextPage_WithoutContinuationToken_ReturnsFalse()
    {
        var sut = CreateResponse(continuationToken: null);

        Assert.False(sut.HasNextPage);
    }

    [Fact]
    public async Task FetchNextPageAsync_NoNextPage_ReturnsNull()
    {
        var sut = CreateResponse(continuationToken: null);

        var result = await sut.FetchNextPageAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchNextPageAsync_HasNextPageButNullFetcher_ReturnsNull()
    {
        var sut = CreateResponse(continuationToken: "next-token", nextPageFetcher: null);

        var result = await sut.FetchNextPageAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchNextPageAsync_HasNextPageAndFetcher_ReturnsFetchedResult()
    {
        var nextPageResponse = CreateResponse(continuationToken: null);
        var expectedResult = DeliveryResult.Success<IDeliveryItemsFeedResponse>(
            nextPageResponse,
            "https://deliver.kontent.ai/test",
            HttpStatusCode.OK,
            false,
            null,
            null,
            ResponseSource.Origin);

        var sut = CreateResponse(
            continuationToken: "next-token",
            nextPageFetcher: _ => Task.FromResult(expectedResult));

        var result = await sut.FetchNextPageAsync();

        Assert.Same(expectedResult, result);
    }

    private static DynamicDeliveryItemsFeedResponse CreateResponse(
        string? continuationToken,
        Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemsFeedResponse>>>? nextPageFetcher = null)
        => new()
        {
            Items = [],
            ContinuationToken = continuationToken,
            NextPageFetcher = nextPageFetcher
        };
}
