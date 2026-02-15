using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Handlers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Handlers;

public class DeliveryAuthenticationHandlerTests
{
    private const string TestEnvironmentId = "12345678-1234-1234-1234-123456789012";
    private const string TestPreviewApiKey = "preview.api.key";
    private const string TestSecureApiKey = "secure.api.key";

    [Fact]
    public async Task SendAsync_WithPreviewApiKey_AddsAuthorizationHeader()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId,
            UsePreviewApi = true,
            PreviewApiKey = TestPreviewApiKey
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items");

        var response = await InvokeSendAsync(handler, request);

        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal(TestPreviewApiKey, request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithSecureAccessApiKey_AddsAuthorizationHeader()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId,
            UseSecureAccess = true,
            SecureAccessApiKey = TestSecureApiKey
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items");

        var response = await InvokeSendAsync(handler, request);

        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal(TestSecureApiKey, request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithoutApiKey_DoesNotAddAuthorizationHeader()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId,
            UsePreviewApi = false,
            UseSecureAccess = false
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items");

        var response = await InvokeSendAsync(handler, request);

        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_WhenKeyBecomesEmpty_ClearsAuthorizationHeader()
    {
        var optionsWithKey = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId,
            UsePreviewApi = true,
            PreviewApiKey = TestPreviewApiKey
        };

        var optionsWithoutKey = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId,
            UsePreviewApi = false
        };

        var optionsMonitor = new TestOptionsMonitor<DeliveryOptions>(optionsWithKey);
        var handler = new DeliveryAuthenticationHandler(optionsMonitor)
        {
            InnerHandler = new TestHandler()
        };

        var request1 = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items");
        await InvokeSendAsync(handler, request1);

        Assert.NotNull(request1.Headers.Authorization);
        Assert.Equal(TestPreviewApiKey, request1.Headers.Authorization.Parameter);

        optionsMonitor.ChangeCurrentValue(optionsWithoutKey);

        var request2 = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items");
        // Pre-populate with old auth header to simulate request reuse or stale state
        request2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "old-stale-key");
        await InvokeSendAsync(handler, request2);

        Assert.Null(request2.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_WithEnvironmentId_InjectsIntoPath()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items");

        var response = await InvokeSendAsync(handler, request);

        Assert.NotNull(request.RequestUri);
        Assert.Equal($"/{TestEnvironmentId}/items", request.RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task SendAsync_WithEnvironmentIdAlreadyInPath_DoesNotDuplicate()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://deliver.kontent.ai/{TestEnvironmentId}/items");

        var response = await InvokeSendAsync(handler, request);

        Assert.NotNull(request.RequestUri);
        Assert.Equal($"/{TestEnvironmentId}/items", request.RequestUri.AbsolutePath);
        var pathSegments = request.RequestUri.AbsolutePath.Split('/');
        var envIdCount = Array.FindAll(pathSegments, s => s == TestEnvironmentId).Length;
        Assert.Equal(1, envIdCount);
    }

    [Fact]
    public async Task SendAsync_WithNamedOptions_UsesCorrectConfiguration()
    {
        var defaultOptions = new DeliveryOptions
        {
            EnvironmentId = "default-env-id"
        };

        var namedOptions = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId,
            UsePreviewApi = true,
            PreviewApiKey = TestPreviewApiKey
        };

        var optionsMonitor = new TestOptionsMonitor<DeliveryOptions>(defaultOptions);
        optionsMonitor.AddNamedOptions("named", namedOptions);

        var handler = new DeliveryAuthenticationHandler(optionsMonitor, "named")
        {
            InnerHandler = new TestHandler()
        };
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items");

        _ = await InvokeSendAsync(handler, request);

        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal(TestPreviewApiKey, request.Headers.Authorization.Parameter);
        Assert.NotNull(request.RequestUri);
        Assert.Contains(TestEnvironmentId, request.RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task SendAsync_PreservesQueryParameters()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items?system.type=article&limit=5");

        var response = await InvokeSendAsync(handler, request);

        Assert.NotNull(request.RequestUri);
        Assert.Equal("?system.type=article&limit=5", request.RequestUri.Query);
    }

    [Fact]
    public async Task SendAsync_HandlesPathWithoutLeadingSlash()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai")
        {
            RequestUri = new Uri("https://deliver.kontent.ai/items", UriKind.Absolute)
        };

        var response = await InvokeSendAsync(handler, request);

        Assert.NotNull(request.RequestUri);
        Assert.Equal($"/{TestEnvironmentId}/items", request.RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task SendAsync_WithCustomBaseUrl_UsesCustomBase()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId,
            ProductionEndpoint = "https://custom-delivery.example.com"
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://custom-delivery.example.com/items");

        var response = await InvokeSendAsync(handler, request);

        Assert.NotNull(request.RequestUri);
        Assert.Equal("https", request.RequestUri.Scheme);
        Assert.Equal("custom-delivery.example.com", request.RequestUri.Host);
        Assert.Equal($"/{TestEnvironmentId}/items", request.RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task SendAsync_WithAssetCdnUrl_LeavesUntouched()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId
        };

        var handler = CreateHandler(options);
        var assetUrl = "https://assets-eu-01.kc-usercontent.com/abc123/def456/image.png";
        var request = new HttpRequestMessage(HttpMethod.Get, assetUrl);

        var response = await InvokeSendAsync(handler, request);

        Assert.NotNull(request.RequestUri);
        Assert.Equal("assets-eu-01.kc-usercontent.com", request.RequestUri.Host);
        Assert.Equal("/abc123/def456/image.png", request.RequestUri.AbsolutePath);
        Assert.DoesNotContain(TestEnvironmentId, request.RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task SendAsync_WithExternalWebhookUrl_LeavesUntouched()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId
        };

        var handler = CreateHandler(options);
        var webhookUrl = "https://external-service.com/webhook/callback?token=abc123";
        var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl);

        var response = await InvokeSendAsync(handler, request);

        Assert.NotNull(request.RequestUri);
        Assert.Equal("external-service.com", request.RequestUri.Host);
        Assert.Equal("/webhook/callback", request.RequestUri.AbsolutePath);
        Assert.Equal("?token=abc123", request.RequestUri.Query);
        Assert.DoesNotContain(TestEnvironmentId, request.RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task SendAsync_WithExternalUrl_DoesNotAttachSdkAuthorizationHeader()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId,
            UsePreviewApi = true,
            PreviewApiKey = TestPreviewApiKey
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://external-service.com/webhook/callback");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "stale-sdk-key");

        await InvokeSendAsync(handler, request);

        Assert.NotNull(request.RequestUri);
        Assert.Equal("external-service.com", request.RequestUri.Host);
        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_WithRelativeUri_InjectsEnvironmentId()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/items", UriKind.Relative));

        var response = await InvokeSendAsync(handler, request);

        Assert.NotNull(request.RequestUri);
        Assert.Equal($"/{TestEnvironmentId}/items", request.RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task SendAsync_WithManagementApiUrl_LeavesUntouched()
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId
        };

        var handler = CreateHandler(options);
        var managementUrl = $"https://manage.kontent.ai/{TestEnvironmentId}/content-items";
        var request = new HttpRequestMessage(HttpMethod.Get, managementUrl);

        var response = await InvokeSendAsync(handler, request);

        Assert.NotNull(request.RequestUri);
        Assert.Equal("manage.kontent.ai", request.RequestUri.Host);
        Assert.Equal($"/{TestEnvironmentId}/content-items", request.RequestUri.AbsolutePath);
    }

    private static DeliveryAuthenticationHandler CreateHandler(DeliveryOptions options)
    {
        var optionsMonitor = new TestOptionsMonitor<DeliveryOptions>(options);
        var handler = new DeliveryAuthenticationHandler(optionsMonitor)
        {
            InnerHandler = new TestHandler()
        };
        return handler;
    }

    private static async Task<HttpResponseMessage> InvokeSendAsync(
        DeliveryAuthenticationHandler handler,
        HttpRequestMessage request)
    {
        var sendAsyncMethod = typeof(DeliveryAuthenticationHandler)
            .GetMethod("SendAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) ?? throw new InvalidOperationException("SendAsync method not found");
        var task = sendAsyncMethod.Invoke(
            handler,
            [request, CancellationToken.None]) as Task<HttpResponseMessage>
            ?? throw new InvalidOperationException("SendAsync returned null");

        return await task;
    }

    private class TestHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    private class TestOptionsMonitor<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
        where TOptions : class
    {
        private TOptions _currentValue = currentValue;
        private readonly Dictionary<string, TOptions> _namedOptions = [];

        public TOptions CurrentValue => _currentValue;

        public TOptions Get(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return _currentValue;
            }

            return _namedOptions.TryGetValue(name, out var options)
                ? options
                : _currentValue;
        }

        public IDisposable OnChange(Action<TOptions, string> listener) => new EmptyDisposable();

        public void AddNamedOptions(string name, TOptions options) => _namedOptions[name] = options;

        public void ChangeCurrentValue(TOptions newValue) => _currentValue = newValue;

        private class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
