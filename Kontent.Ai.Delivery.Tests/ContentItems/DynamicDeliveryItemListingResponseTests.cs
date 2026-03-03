using System.Net;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ContentItems;

public class DynamicDeliveryItemListingResponseTests
{
    [Fact]
    public void IPageable_Pagination_ReturnsSameInstance()
    {
        var pagination = new Pagination { Skip = 0, Limit = 10, Count = 5, TotalCount = null, NextPageUrl = string.Empty };
        var sut = CreateResponse(pagination);

        var result = ((IPageable)sut).Pagination;

        Assert.Same(pagination, result);
    }

    [Fact]
    public void HasNextPage_WithNextPageUrl_ReturnsTrue()
    {
        var pagination = new Pagination { Skip = 0, Limit = 10, Count = 10, TotalCount = null, NextPageUrl = "https://next" };
        var sut = CreateResponse(pagination);

        Assert.True(sut.HasNextPage);
    }

    [Fact]
    public void HasNextPage_WithoutNextPageUrl_ReturnsFalse()
    {
        var pagination = new Pagination { Skip = 0, Limit = 10, Count = 5, TotalCount = null, NextPageUrl = string.Empty };
        var sut = CreateResponse(pagination);

        Assert.False(sut.HasNextPage);
    }

    [Fact]
    public async Task FetchNextPageAsync_NoNextPage_ReturnsNull()
    {
        var pagination = new Pagination { Skip = 0, Limit = 10, Count = 5, TotalCount = null, NextPageUrl = string.Empty };
        var sut = CreateResponse(pagination);

        var result = await sut.FetchNextPageAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchNextPageAsync_HasNextPageButNullFetcher_ReturnsNull()
    {
        var pagination = new Pagination { Skip = 0, Limit = 10, Count = 10, TotalCount = null, NextPageUrl = "https://next" };
        var sut = CreateResponse(pagination, nextPageFetcher: null);

        var result = await sut.FetchNextPageAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchNextPageAsync_HasNextPageAndFetcher_ReturnsFetchedResult()
    {
        var pagination = new Pagination { Skip = 0, Limit = 10, Count = 10, TotalCount = null, NextPageUrl = "https://next" };
        var nextPageResponse = CreateResponse(
            new Pagination { Skip = 10, Limit = 10, Count = 3, TotalCount = null, NextPageUrl = string.Empty });
        var expectedResult = CreateSuccessResult(nextPageResponse);

        var sut = CreateResponse(pagination, nextPageFetcher: _ => Task.FromResult(expectedResult));

        var result = await sut.FetchNextPageAsync();

        Assert.Same(expectedResult, result);
    }

    private static DynamicDeliveryItemListingResponse CreateResponse(
        Pagination pagination,
        Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemListingResponse>>>? nextPageFetcher = null)
        => new()
        {
            Items = [],
            Pagination = pagination,
            NextPageFetcher = nextPageFetcher
        };

    private static IDeliveryResult<IDeliveryItemListingResponse> CreateSuccessResult(
        IDeliveryItemListingResponse response)
        => DeliveryResult.Success(
            response,
            "https://deliver.kontent.ai/test",
            HttpStatusCode.OK,
            false,
            null,
            null,
            ResponseSource.Origin);
}
