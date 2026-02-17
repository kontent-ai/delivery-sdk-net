using System.Net;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.SharedModels;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Caching;

public class QueryExecutionResultHelperTests
{
    [Fact]
    public void CreateMissingApiResultFailure_ReturnsInternalServerErrorFailure()
    {
        var result = QueryExecutionResultHelper.CreateMissingApiResultFailure<string>("Items", "list");

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Contains("API result was not captured", result.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureApiResult_WhenNull_ReturnsMissingApiResultFailure()
    {
        var result = QueryExecutionResultHelper.EnsureApiResult<string>(null, "Items", "list");

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [Fact]
    public void EnsureApiResult_WhenPresent_ReturnsOriginalResult()
    {
        var existing = DeliveryResult.Success(
            "value",
            requestUrl: "/items",
            statusCode: HttpStatusCode.OK,
            hasStaleContent: false,
            continuationToken: null,
            responseHeaders: null);

        var result = QueryExecutionResultHelper.EnsureApiResult<string>(existing, "Items", "list");

        Assert.Same(existing, result);
    }
}
