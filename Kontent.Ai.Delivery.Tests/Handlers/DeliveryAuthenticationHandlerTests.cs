using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Handlers;

namespace Kontent.Ai.Delivery.Tests.Handlers;

public class DeliveryAuthenticationHandlerTests
{
    private const string TestEnvironmentId = "12345678-1234-1234-1234-123456789012";
    private const string TestPreviewApiKey = "preview.api.key";
    private const string TestSecureApiKey = "secure.api.key";

    [Fact]
    public async Task SendAsync_WithPreviewApiKey_AddsAuthorizationHeader()
    {
        // Arrange
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId,
            UsePreviewApi = true,
            PreviewApiKey = TestPreviewApiKey
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items");

        // Act
        var response = await InvokeSendAsync(handler, request);

        // Assert
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal(TestPreviewApiKey, request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithSecureAccessApiKey_AddsAuthorizationHeader()
    {
        // Arrange
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId,
            UseSecureAccess = true,
            SecureAccessApiKey = TestSecureApiKey
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items");

        // Act
        var response = await InvokeSendAsync(handler, request);

        // Assert
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal(TestSecureApiKey, request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithoutApiKey_DoesNotAddAuthorizationHeader()
    {
        // Arrange
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId,
            UsePreviewApi = false,
            UseSecureAccess = false
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items");

        // Act
        var response = await InvokeSendAsync(handler, request);

        // Assert
        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_WithEnvironmentId_InjectsIntoPath()
    {
        // Arrange
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items");

        // Act
        var response = await InvokeSendAsync(handler, request);

        // Assert
        Assert.NotNull(request.RequestUri);
        Assert.Equal($"/{TestEnvironmentId}/items", request.RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task SendAsync_WithEnvironmentIdAlreadyInPath_DoesNotDuplicate()
    {
        // Arrange
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://deliver.kontent.ai/{TestEnvironmentId}/items");

        // Act
        var response = await InvokeSendAsync(handler, request);

        // Assert
        Assert.NotNull(request.RequestUri);
        Assert.Equal($"/{TestEnvironmentId}/items", request.RequestUri.AbsolutePath);
        // Ensure the environment ID appears only once
        var pathSegments = request.RequestUri.AbsolutePath.Split('/');
        var envIdCount = Array.FindAll(pathSegments, s => s == TestEnvironmentId).Length;
        Assert.Equal(1, envIdCount);
    }

    [Fact]
    public async Task SendAsync_WithNamedOptions_UsesCorrectConfiguration()
    {
        // Arrange
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

        var handler = new DeliveryAuthenticationHandler(optionsMonitor, "named");
        handler.InnerHandler = new TestHandler();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items");

        // Act
        var response = await InvokeSendAsync(handler, request);

        // Assert
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal(TestPreviewApiKey, request.Headers.Authorization.Parameter);
        Assert.Contains(TestEnvironmentId, request.RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task SendAsync_PreservesQueryParameters()
    {
        // Arrange
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai/items?system.type=article&limit=5");

        // Act
        var response = await InvokeSendAsync(handler, request);

        // Assert
        Assert.NotNull(request.RequestUri);
        Assert.Equal("?system.type=article&limit=5", request.RequestUri.Query);
    }

    [Fact]
    public async Task SendAsync_HandlesPathWithoutLeadingSlash()
    {
        // Arrange
        var options = new DeliveryOptions
        {
            EnvironmentId = TestEnvironmentId
        };

        var handler = CreateHandler(options);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://deliver.kontent.ai");
        request.RequestUri = new Uri("https://deliver.kontent.ai/items", UriKind.Absolute);

        // Act
        var response = await InvokeSendAsync(handler, request);

        // Assert
        Assert.NotNull(request.RequestUri);
        Assert.Equal($"/{TestEnvironmentId}/items", request.RequestUri.AbsolutePath);
    }

    private static DeliveryAuthenticationHandler CreateHandler(DeliveryOptions options)
    {
        var optionsMonitor = new TestOptionsMonitor<DeliveryOptions>(options);
        var handler = new DeliveryAuthenticationHandler(optionsMonitor);
        handler.InnerHandler = new TestHandler();
        return handler;
    }

    private static async Task<HttpResponseMessage> InvokeSendAsync(
        DeliveryAuthenticationHandler handler,
        HttpRequestMessage request)
    {
        // Use reflection to invoke the protected SendAsync method
        var sendAsyncMethod = typeof(DeliveryAuthenticationHandler)
            .GetMethod("SendAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (sendAsyncMethod == null)
            throw new InvalidOperationException("SendAsync method not found");

        var task = (Task<HttpResponseMessage>)sendAsyncMethod.Invoke(
            handler,
            new object[] { request, CancellationToken.None });

        return await task;
    }

    // Simple handler that returns a success response
    private class TestHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    // Test implementation of IOptionsMonitor
    private class TestOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
        where TOptions : class
    {
        private readonly TOptions _currentValue;
        private readonly Dictionary<string, TOptions> _namedOptions = new();

        public TestOptionsMonitor(TOptions currentValue)
        {
            _currentValue = currentValue;
        }

        public TOptions CurrentValue => _currentValue;

        public TOptions Get(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return _currentValue;

            return _namedOptions.TryGetValue(name, out var options) ? options : _currentValue;
        }

        public IDisposable OnChange(Action<TOptions, string> listener)
        {
            // Not needed for tests
            return new EmptyDisposable();
        }

        public void AddNamedOptions(string name, TOptions options)
        {
            _namedOptions[name] = options;
        }

        private class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}