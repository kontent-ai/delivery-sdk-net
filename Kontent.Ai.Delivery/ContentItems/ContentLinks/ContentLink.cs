using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.ContentLinks;

/// <inheritdoc cref="IContentLink"/>
[DebuggerDisplay("Id = {Id}, Codename = {" + nameof(IContentLink.Codename) + "}")]
internal sealed record ContentLink() : IContentLink
{
    /// <summary>
    /// The unique identifier of the content item this link points to.
    /// This is populated from the dictionary key when deserializing rich text links.
    /// </summary>
    [JsonIgnore]
    public Guid Id { get; set; }

    /// <inheritdoc/>
    Guid IContentLink.Id
    {
        get => Id;
        set => Id = value;
    }

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
