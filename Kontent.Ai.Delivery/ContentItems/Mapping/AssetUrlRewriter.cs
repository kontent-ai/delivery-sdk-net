namespace Kontent.Ai.Delivery.ContentItems.Mapping;

internal static class AssetUrlRewriter
{
    internal static string RewriteUrl(string url, Uri? customDomain)
    {
        if (customDomain is null || string.IsNullOrEmpty(url))
            return url;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var originalUri))
            return url;

        var builder = new UriBuilder(originalUri)
        {
            Scheme = customDomain.Scheme,
            Host = customDomain.Host,
            Port = customDomain.IsDefaultPort ? -1 : customDomain.Port
        };

        return builder.Uri.AbsoluteUri;
    }
}
