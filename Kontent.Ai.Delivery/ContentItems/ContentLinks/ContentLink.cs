using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.ContentItems.ContentLinks;

/// <inheritdoc cref="IContentLink"/>
[DebuggerDisplay("Codename = {" + nameof(IContentLink.Codename) + "}")]
[method: JsonConstructor]
internal sealed class ContentLink() : IContentLink // TODO: improve, nullability etc.
{
    /// <inheritdoc/>
    Guid IContentLink.Id
    {
        get; set;
    }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public string Codename
    {
        get; internal set;
    }

    /// <inheritdoc/>
    [JsonPropertyName("url_slug")]
    public string UrlSlug
    {
        get; internal set;
    }

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public string ContentTypeCodename
    {
        get; internal set;
    }
}

