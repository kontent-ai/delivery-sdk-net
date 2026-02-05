using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.ContentLinks;

/// <inheritdoc cref="IContentLink"/>
[DebuggerDisplay("Id = {Id}, Codename = {Codename}")]
internal sealed record ContentLink : IContentLink
{
    /// <summary>
    /// The unique identifier of the content item this link points to.
    /// Populated from the dictionary key when deserializing rich text links.
    /// </summary>
    [JsonIgnore]
    public Guid Id { get; set; }

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
