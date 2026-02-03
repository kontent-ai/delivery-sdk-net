using FluentAssertions;
using Kontent.Ai.Delivery.Abstractions;
using Xunit;

namespace Kontent.Ai.Delivery.Tests;

public class DeliveryOptionsExtensionsTests
{
    [Fact]
    public void GetBaseUrl_Production_ReturnsProductionEndpoint()
    {
        var options = new DeliveryOptions
        {
            UsePreviewApi = false,
            ProductionEndpoint = "https://deliver.kontent.ai/",
            PreviewEndpoint = "https://preview-deliver.kontent.ai/"
        };

        var result = options.GetBaseUrl();

        result.Should().Be(options.ProductionEndpoint);
    }

    [Fact]
    public void GetBaseUrl_Preview_ReturnsPreviewEndpoint()
    {
        var options = new DeliveryOptions
        {
            UsePreviewApi = true,
            ProductionEndpoint = "https://deliver.kontent.ai/",
            PreviewEndpoint = "https://preview-deliver.kontent.ai/"
        };

        var result = options.GetBaseUrl();

        result.Should().Be(options.PreviewEndpoint);
    }

    [Fact]
    public void GetApiKey_SecureAccessEnabled_ReturnsSecureAccessApiKey()
    {
        var options = new DeliveryOptions
        {
            UseSecureAccess = true,
            SecureAccessApiKey = "sec.sec.sec",
            UsePreviewApi = false,
            PreviewApiKey = "pre.pre.pre"
        };

        var result = options.GetApiKey();

        result.Should().Be(options.SecureAccessApiKey);
    }

    [Fact]
    public void GetApiKey_PreviewEnabled_ReturnsPreviewApiKey()
    {
        var options = new DeliveryOptions
        {
            UseSecureAccess = false,
            UsePreviewApi = true,
            PreviewApiKey = "pre.pre.pre"
        };

        var result = options.GetApiKey();

        result.Should().Be(options.PreviewApiKey);
    }

    [Fact]
    public void GetApiKey_NoneEnabled_ReturnsNull()
    {
        var options = new DeliveryOptions
        {
            UseSecureAccess = false,
            UsePreviewApi = false
        };

        var result = options.GetApiKey();

        result.Should().BeNull();
    }
}
