using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes;

/// <inheritdoc/>
/// <summary>
/// Initializes a new instance of the <see cref="ContentTypeSystemAttributes"/> class.
/// </summary>
[DebuggerDisplay("Id = {" + nameof(Id) + "}")]
internal sealed record ContentTypeSystemAttributes() : IContentTypeSystemAttributes
{
    /// <inheritdoc/>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public required string Codename { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("last_modified")]
    public required DateTime LastModified { get; init; }
}