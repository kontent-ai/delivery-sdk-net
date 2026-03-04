using System.Net;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;
using Refit;

namespace Kontent.Ai.Delivery.Tests.Extensions;

public class RefitApiResponseExtensionsTests
{
    [Fact]
    public async Task ToDeliveryResultAsync_SuccessfulResponse_ReturnsSuccess()
    {
        using var httpResponse = CreateHttpResponse(HttpStatusCode.OK, "test content");
        using var apiResponse = new ApiResponse<string>(httpResponse, "test content", new RefitSettings());

        var result = await apiResponse.ToDeliveryResultAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("test content", result.Value);
    }

    [Fact]
    public async Task ToDeliveryResultAsync_SuccessWithNullContent_FallsToFailurePath()
    {
        using var httpResponse = CreateHttpResponse(HttpStatusCode.OK);
        // Successful status but null content triggers the failure path with no ApiException
        using var apiResponse = new ApiResponse<string>(httpResponse, null, new RefitSettings());

        var result = await apiResponse.ToDeliveryResultAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ToDeliveryResultAsync_FailureWithApiException_ReturnsFailure()
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/test");
        using var httpResponse = CreateHttpResponse(HttpStatusCode.NotFound, """{"message":"not found","error_code":100}""");
        httpResponse.RequestMessage = requestMessage;
        var apiException = await ApiException.Create(requestMessage, HttpMethod.Get, httpResponse, new RefitSettings());
        using var failedResponse = new ApiResponse<string>(httpResponse, null, new RefitSettings(), apiException);

        var result = await failedResponse.ToDeliveryResultAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ToDeliveryResultAsync_WithStaleContentHeader_ExtractsStaleContent()
    {
        using var httpResponse = CreateHttpResponse(HttpStatusCode.OK, "content");
        httpResponse.Headers.TryAddWithoutValidation("X-Stale-Content", "1");
        using var apiResponse = new ApiResponse<string>(httpResponse, "content", new RefitSettings());

        var result = await apiResponse.ToDeliveryResultAsync();

        Assert.True(result.HasStaleContent);
    }

    [Fact]
    public async Task ToDeliveryResultAsync_WithContinuationHeader_ExtractsContinuationToken()
    {
        using var httpResponse = CreateHttpResponse(HttpStatusCode.OK, "content");
        httpResponse.Headers.TryAddWithoutValidation("X-Continuation", "token123");
        using var apiResponse = new ApiResponse<string>(httpResponse, "content", new RefitSettings());

        var result = await apiResponse.ToDeliveryResultAsync();

        Assert.Equal("token123", result.ContinuationToken);
    }

    [Fact]
    public async Task ToDeliveryResultAsync_WithXCacheHit_ReturnsResponseSourceCdn()
    {
        using var httpResponse = CreateHttpResponse(HttpStatusCode.OK, "content");
        httpResponse.Headers.TryAddWithoutValidation("X-Cache", "HIT");
        using var apiResponse = new ApiResponse<string>(httpResponse, "content", new RefitSettings());

        var result = await apiResponse.ToDeliveryResultAsync();

        Assert.Equal(ResponseSource.Cdn, result.ResponseSource);
    }

    [Fact]
    public async Task ToDeliveryResultAsync_WithoutXCacheHeader_ReturnsResponseSourceOrigin()
    {
        using var httpResponse = CreateHttpResponse(HttpStatusCode.OK, "content");
        using var apiResponse = new ApiResponse<string>(httpResponse, "content", new RefitSettings());

        var result = await apiResponse.ToDeliveryResultAsync();

        Assert.Equal(ResponseSource.Origin, result.ResponseSource);
    }

    [Fact]
    public async Task ToDeliveryResultAsync_ApiExceptionWithNonJsonBody_ReturnsRawBodyInMessage()
    {
        var htmlBody = "<html><body>Internal Server Error</body></html>";
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/test");
        using var httpResponse = CreateHttpResponse(HttpStatusCode.InternalServerError, htmlBody);
        httpResponse.RequestMessage = requestMessage;
        httpResponse.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
        var apiException = await ApiException.Create(requestMessage, HttpMethod.Get, httpResponse, new RefitSettings());
        using var failedResponse = new ApiResponse<string>(httpResponse, null, new RefitSettings(), apiException);

        var result = await failedResponse.ToDeliveryResultAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Contains("Raw response:", result.Error.Message);
    }

    private static HttpResponseMessage CreateHttpResponse(HttpStatusCode statusCode, string? content = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content is not null)
        {
            response.Content = new StringContent(content);
        }
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/test");
        return response;
    }
}
