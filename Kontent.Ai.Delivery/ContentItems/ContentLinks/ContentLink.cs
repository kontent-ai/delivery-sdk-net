using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.ContentItems.ContentLinks;

/// <inheritdoc cref="IContentLink"/>
[DebuggerDisplay("Codename = {" + nameof(IContentLink.Codename) + "}")]
internal sealed record ContentLink() : IContentLink // TODO: improve, nullability etc.
{
    /// <inheritdoc/>
    Guid IContentLink.Id { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public required string Codename { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("url_slug")]
    public required string UrlSlug { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public required string ContentTypeCodename { get; init; }
}