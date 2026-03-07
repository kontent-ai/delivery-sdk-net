using Kontent.Ai.Delivery.ContentItems.Mapping;

namespace Kontent.Ai.Delivery.Tests.ContentItems.Mapping;

public sealed class AssetUrlRewriterTests
{
    [Fact]
    public void RewriteUrl_NullCustomDomain_ReturnsOriginalUrl()
    {
        var original = "https://assets-eu-01.kc-usercontent.com/env-id/asset-id/file.jpg";
        var result = AssetUrlRewriter.RewriteUrl(original, null);
        Assert.Equal(original, result);
    }

    [Fact]
    public void RewriteUrl_EmptyUrl_ReturnsEmpty()
    {
        var customDomain = new Uri("https://assets.example.com");
        var result = AssetUrlRewriter.RewriteUrl(string.Empty, customDomain);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RewriteUrl_RewritesSchemeAndHost_PreservesPath()
    {
        var original = "https://assets-eu-01.kc-usercontent.com/env-id/asset-id/file.jpg";
        var customDomain = new Uri("https://assets.example.com");

        var result = AssetUrlRewriter.RewriteUrl(original, customDomain);

        Assert.Equal("https://assets.example.com/env-id/asset-id/file.jpg", result);
    }

    [Fact]
    public void RewriteUrl_PreservesQueryString()
    {
        var original = "https://assets-eu-01.kc-usercontent.com/env-id/asset-id/file.jpg?w=200&h=100";
        var customDomain = new Uri("https://assets.example.com");

        var result = AssetUrlRewriter.RewriteUrl(original, customDomain);

        Assert.Equal("https://assets.example.com/env-id/asset-id/file.jpg?w=200&h=100", result);
    }

    [Fact]
    public void RewriteUrl_HandlesNonDefaultPort()
    {
        var original = "https://assets-eu-01.kc-usercontent.com/env-id/asset-id/file.jpg";
        var customDomain = new Uri("https://assets.example.com:8443");

        var result = AssetUrlRewriter.RewriteUrl(original, customDomain);

        Assert.Equal("https://assets.example.com:8443/env-id/asset-id/file.jpg", result);
    }

    [Fact]
    public void RewriteUrl_InvalidUrl_ReturnsOriginal()
    {
        var original = "not-a-valid-url";
        var customDomain = new Uri("https://assets.example.com");

        var result = AssetUrlRewriter.RewriteUrl(original, customDomain);

        Assert.Equal(original, result);
    }

    [Fact]
    public void RewriteUrl_CustomDomainWithTrailingSlash_HandledCorrectly()
    {
        var original = "https://assets-eu-01.kc-usercontent.com/env-id/asset-id/file.jpg";
        var customDomain = new Uri("https://assets.example.com/");

        var result = AssetUrlRewriter.RewriteUrl(original, customDomain);

        Assert.Equal("https://assets.example.com/env-id/asset-id/file.jpg", result);
    }

    [Fact]
    public void RewriteUrl_ChangesScheme()
    {
        var original = "https://assets-eu-01.kc-usercontent.com/env-id/asset-id/file.jpg";
        var customDomain = new Uri("http://assets.example.com");

        var result = AssetUrlRewriter.RewriteUrl(original, customDomain);

        Assert.Equal("http://assets.example.com/env-id/asset-id/file.jpg", result);
    }

    [Fact]
    public void RewriteUrl_DefaultPort_OmitsPortInResult()
    {
        var original = "https://assets-eu-01.kc-usercontent.com/env-id/asset-id/file.jpg";
        var customDomain = new Uri("https://assets.example.com:443");

        var result = AssetUrlRewriter.RewriteUrl(original, customDomain);

        // Port 443 is default for HTTPS, should not appear in the URL
        Assert.DoesNotContain(":443", result);
        Assert.Equal("https://assets.example.com/env-id/asset-id/file.jpg", result);
    }
}
