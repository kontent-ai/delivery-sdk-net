using System.Net;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Kontent.Ai.Delivery.Tests.ErrorHandling;

/// <summary>
/// Tests for API error handling and error response parsing.
/// Verifies the SDK correctly handles various HTTP error codes and
/// parses structured Kontent.ai error responses.
/// </summary>
public sealed class ApiErrorTests
{
    private readonly Guid _guid = Guid.NewGuid();
    private string BaseUrl => $"https://deliver.kontent.ai/{_guid}";

    private IDeliveryClient CreateClient(MockHttpMessageHandler mockHttp)
    {
        var services = new ServiceCollection();
        var options = new DeliveryOptions { EnvironmentId = _guid.ToString() };
        services.AddDeliveryClient(options, configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    #region HTTP Status Code Tests

    [Fact]
    public async Task GetItem_NotFound_ReturnsErrorWithStatusCode404()
    {
        var mock = new MockHttpMessageHandler();
        var errorJson = BuildKontentErrorJson(
            message: "The requested content item 'non_existent_item' was not found.",
            requestId: "req-12345",
            errorCode: 100);

        mock.When($"{BaseUrl}/items/non_existent_item")
            .Respond(HttpStatusCode.NotFound, "application/json", errorJson);

        var client = CreateClient(mock);

        var result = await client.GetItem<IDynamicElements>("non_existent_item").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.NotNull(result.Error);
        Assert.Contains("not found", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetItem_Unauthorized_ReturnsErrorWithStatusCode401()
    {
        var mock = new MockHttpMessageHandler();
        var errorJson = BuildKontentErrorJson(
            message: "The provided API key is invalid or has been revoked.",
            requestId: "req-67890",
            errorCode: 2);

        mock.When($"{BaseUrl}/items/some_item")
            .Respond(HttpStatusCode.Unauthorized, "application/json", errorJson);

        var client = CreateClient(mock);

        var result = await client.GetItem<IDynamicElements>("some_item").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task GetItem_Forbidden_ReturnsErrorWithStatusCode403()
    {
        var mock = new MockHttpMessageHandler();
        var errorJson = BuildKontentErrorJson(
            message: "Access to this environment requires secure access API key.",
            requestId: "req-11111",
            errorCode: 3);

        mock.When($"{BaseUrl}/items/secure_item")
            .Respond(HttpStatusCode.Forbidden, "application/json", errorJson);

        var client = CreateClient(mock);

        var result = await client.GetItem<IDynamicElements>("secure_item").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task GetItems_BadRequest_ReturnsErrorWithStatusCode400()
    {
        var mock = new MockHttpMessageHandler();
        var errorJson = BuildKontentErrorJson(
            message: "The provided filter parameter is invalid.",
            requestId: "req-22222",
            errorCode: 1);

        mock.When($"{BaseUrl}/items")
            .Respond(HttpStatusCode.BadRequest, "application/json", errorJson);

        var client = CreateClient(mock);

        var result = await client.GetItems<IDynamicElements>().ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task GetItems_RateLimited_ReturnsErrorWithStatusCode429()
    {
        var mock = new MockHttpMessageHandler();
        var errorJson = BuildKontentErrorJson(
            message: "Rate limit exceeded. Please retry after some time.",
            requestId: "req-33333",
            errorCode: 10000);

        mock.When($"{BaseUrl}/items")
            .Respond((HttpStatusCode)429, "application/json", errorJson);

        var client = CreateClient(mock);

        var result = await client.GetItems<IDynamicElements>().ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal((HttpStatusCode)429, result.StatusCode);
        Assert.NotNull(result.Error);
        Assert.Contains("Rate limit", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Error Response Parsing Tests

    [Fact]
    public async Task ErrorResponse_ParsesStructuredKontentError()
    {
        var mock = new MockHttpMessageHandler();
        var errorJson = """
            {
                "message": "The requested content item 'my_item' was not found.",
                "request_id": "req-abc-123-xyz",
                "error_code": 100,
                "specific_code": 1
            }
            """;

        mock.When($"{BaseUrl}/items/my_item")
            .Respond(HttpStatusCode.NotFound, "application/json", errorJson);

        var client = CreateClient(mock);

        var result = await client.GetItem<IDynamicElements>("my_item").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("The requested content item 'my_item' was not found.", result.Error.Message);
        Assert.Equal("req-abc-123-xyz", result.Error.RequestId);
        Assert.Equal(100, result.Error.ErrorCode);
        Assert.Equal(1, result.Error.SpecificCode);
    }

    [Fact]
    public async Task ErrorResponse_HandlesNonJsonError_CombinesRefitMessageWithRawBody()
    {
        var mock = new MockHttpMessageHandler();
        const string rawErrorMessage = "Bad Gateway - upstream server unavailable";

        mock.When($"{BaseUrl}/items/some_item")
            .Respond(HttpStatusCode.BadGateway, "text/plain", rawErrorMessage);

        var client = CreateClient(mock);

        var result = await client.GetItem<IDynamicElements>("some_item").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.BadGateway, result.StatusCode);
        Assert.NotNull(result.Error);
        Assert.Contains("Raw response:", result.Error.Message);
        Assert.Contains(rawErrorMessage, result.Error.Message);
    }

    [Fact]
    public async Task ErrorResponse_HandlesHtmlErrorPage_CombinesRefitMessageWithRawBody()
    {
        var mock = new MockHttpMessageHandler();
        const string htmlError = "<html><body><h1>502 Bad Gateway</h1><p>nginx</p></body></html>";

        mock.When($"{BaseUrl}/items/some_item")
            .Respond(HttpStatusCode.BadGateway, "text/html", htmlError);

        var client = CreateClient(mock);

        var result = await client.GetItem<IDynamicElements>("some_item").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.BadGateway, result.StatusCode);
        Assert.NotNull(result.Error);
        Assert.Contains("Raw response:", result.Error.Message);
        Assert.Contains("502 Bad Gateway", result.Error.Message);
        Assert.Contains("nginx", result.Error.Message);
    }

    [Fact]
    public async Task ErrorResponse_LongHtmlPage_TruncatesRawBody()
    {
        var mock = new MockHttpMessageHandler();
        var longHtml = "<html><body>" + new string('x', 1000) + "</body></html>";

        mock.When($"{BaseUrl}/items/some_item")
            .Respond(HttpStatusCode.BadGateway, "text/html", longHtml);

        var client = CreateClient(mock);

        var result = await client.GetItem<IDynamicElements>("some_item").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Contains("(truncated)", result.Error.Message);
        Assert.True(result.Error.Message.Length < 700);
    }

    [Fact]
    public async Task ErrorResponse_WithoutMessageField_UsesDefaultMessage()
    {
        var mock = new MockHttpMessageHandler();
        var errorJson = """
            {
                "error_code": 100,
                "request_id": "req-no-message"
            }
            """;

        mock.When($"{BaseUrl}/items/some_item")
            .Respond(HttpStatusCode.NotFound, "application/json", errorJson);

        var client = CreateClient(mock);

        var result = await client.GetItem<IDynamicElements>("some_item").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.NotNull(result.Error);
        Assert.False(string.IsNullOrEmpty(result.Error.Message));
        Assert.Equal(100, result.Error.ErrorCode);
        Assert.Equal("req-no-message", result.Error.RequestId);
    }

    [Fact]
    public async Task ErrorResponse_HandlesEmptyBody()
    {
        var mock = new MockHttpMessageHandler();

        mock.When($"{BaseUrl}/items/some_item")
            .Respond(HttpStatusCode.InternalServerError, "application/json", string.Empty);

        var client = CreateClient(mock);

        var result = await client.GetItem<IDynamicElements>("some_item").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ErrorResponse_HandlesMalformedJson()
    {
        var mock = new MockHttpMessageHandler();

        mock.When($"{BaseUrl}/items/some_item")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{ invalid json }");

        var client = CreateClient(mock);

        var result = await client.GetItem<IDynamicElements>("some_item").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.NotNull(result.Error);
        Assert.False(string.IsNullOrEmpty(result.Error.Message));
    }

    [Fact]
    public async Task ErrorResponse_ExceptionProperty_ContainsUnderlyingException()
    {
        var mock = new MockHttpMessageHandler();
        var errorJson = BuildKontentErrorJson(
            message: "Test error for exception property",
            requestId: "req-exception-test",
            errorCode: 100);

        mock.When($"{BaseUrl}/items/some_item")
            .Respond(HttpStatusCode.NotFound, "application/json", errorJson);

        var client = CreateClient(mock);

        var result = await client.GetItem<IDynamicElements>("some_item").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.NotNull(result.Error.Exception);
        Assert.Contains("404", result.Error.Exception.Message);
    }

    #endregion

    #region Error on Different Query Types

    [Fact]
    public async Task GetType_NotFound_ReturnsError()
    {
        var mock = new MockHttpMessageHandler();
        var errorJson = BuildKontentErrorJson(
            message: "The requested content type 'non_existent_type' was not found.",
            requestId: "req-44444",
            errorCode: 101);

        mock.When($"{BaseUrl}/types/non_existent_type")
            .Respond(HttpStatusCode.NotFound, "application/json", errorJson);

        var client = CreateClient(mock);

        var result = await client.GetType("non_existent_type").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task GetTaxonomy_NotFound_ReturnsError()
    {
        var mock = new MockHttpMessageHandler();
        var errorJson = BuildKontentErrorJson(
            message: "The requested taxonomy group 'non_existent_taxonomy' was not found.",
            requestId: "req-55555",
            errorCode: 102);

        mock.When($"{BaseUrl}/taxonomies/non_existent_taxonomy")
            .Respond(HttpStatusCode.NotFound, "application/json", errorJson);

        var client = CreateClient(mock);

        var result = await client.GetTaxonomy("non_existent_taxonomy").ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.NotNull(result.Error);
    }

    #endregion

    #region Helper Methods

    private static string BuildKontentErrorJson(string message, string? requestId = null, int? errorCode = null, int? specificCode = null)
    {
        var parts = new List<string> { $"\"message\": \"{message}\"" };

        if (requestId is not null)
            parts.Add($"\"request_id\": \"{requestId}\"");

        if (errorCode.HasValue)
            parts.Add($"\"error_code\": {errorCode.Value}");

        if (specificCode.HasValue)
            parts.Add($"\"specific_code\": {specificCode.Value}");

        return $"{{ {string.Join(", ", parts)} }}";
    }

    #endregion
}
